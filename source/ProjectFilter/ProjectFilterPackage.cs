using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ProjectFilter.Commands;
using ProjectFilter.Services;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter {

    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PackageGuids.ProjectFilterPackageString)]
    [ProvideService(typeof(IExtensionSettings), IsAsyncQueryable = true)]
    [ProvideService(typeof(IFilterOptionsProvider), IsAsyncQueryable = true)]
    [ProvideService(typeof(IFilterService), IsAsyncQueryable = true)]
    [ProvideService(typeof(IHierarchyProvider), IsAsyncQueryable = true)]
    [ProvideService(typeof(ILogger), IsAsyncQueryable = true)]
    [ProvideService(typeof(IWaitDialogFactory), IsAsyncQueryable = true)]
    public sealed class ProjectFilterPackage : AsyncPackage {

        private readonly List<MenuCommandBase> _commands = new List<MenuCommandBase>();
        private IVsSolution? _solution;
        private SolutionLoadObserver? _solutionLoadObserver;
        private uint _solutionEventsCookie;


        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
            await base.InitializeAsync(cancellationToken, progress);

            AddServices();

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await AddCommandsAsync();

            _solution = await this.GetServiceAsync<SVsSolution, IVsSolution>();

            if (_solution != null) {
                _solutionLoadObserver = new SolutionLoadObserver(this);
                await _solutionLoadObserver.InitializeAsync(cancellationToken);

                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                _solution.AdviseSolutionEvents(_solutionLoadObserver, out _solutionEventsCookie);

                // If a solution is already open, then we need to tell the
                // solution observer to do whatever it does after a solution is opened.
                if (ErrorHandler.Succeeded(_solution.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object value))) {
                    if (value is bool isOpen && isOpen) {
                        _solutionLoadObserver.OnAfterOpenSolution(0, 0);
                    }
                }
            }
        }


        private void AddServices() {
            AddService<ExtensionSettings, IExtensionSettings>(new ExtensionSettings(this));
            AddService<FilterOptionsProvider, IFilterOptionsProvider>(new FilterOptionsProvider(this));
            AddService<FilterService, IFilterService>(new FilterService(this));
            AddService<HierarchyProvider, IHierarchyProvider>(new HierarchyProvider(this));
            AddService<Logger, ILogger>(new Logger(this));
            AddService<WaitDialogFactory, IWaitDialogFactory>(new WaitDialogFactory(this));
        }


        private void AddService<TService, TInterface>(TService service) where TService : TInterface {
            AddService(
                typeof(TInterface),
                async (container, cancellation, type) => {
                    if (service is IAsyncInitializable initializable) {
                        await initializable.InitializeAsync(cancellation);
                    }

                    return service;
                }
            );
        }


        private async Task AddCommandsAsync() {
            await AddCommandAsync(new FilterProjectsCommand(this));
        }


        private async Task AddCommandAsync<T>(T command) where T : MenuCommandBase {
            await command.InitializeAsync(DisposalToken);
            _commands.Add(command);
        }


        [SuppressMessage("Usage", "VSTHRD104:Offer async methods", Justification = "No other way to use async code in Dispose().")]
        protected override void Dispose(bool disposing) {
            ExtensionThreadHelper.JoinableTaskFactory.Run(DisposeAsync);
            base.Dispose(disposing);
        }


        private async Task DisposeAsync() {
            if (_solution != null && _solutionEventsCookie != 0) {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                _solution.UnadviseSolutionEvents(_solutionEventsCookie);
                _solutionEventsCookie = 0;
                _solutionLoadObserver = null;
            }
        }

    }

}
