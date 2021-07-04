using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Community.VisualStudio.Toolkit;
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
    public sealed class ProjectFilterPackage : ToolkitPackage {

        private IVsSolution? _solution;
        private SolutionLoadObserver? _solutionLoadObserver;
        private uint _solutionEventsCookie;


        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
            await base.InitializeAsync(cancellationToken, progress);

            AddServices();
            await AddCommandsAsync();

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            _solution = await VS.Services.GetSolutionAsync();

            _solutionLoadObserver = new SolutionLoadObserver(this);
            await _solutionLoadObserver.InitializeAsync(cancellationToken);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            _solution.AdviseSolutionEvents(_solutionLoadObserver, out _solutionEventsCookie);

            // If a solution is already open, then we need to tell the solution
            // observer to do whatever it does after a solution is opened.
            if (_solution.IsOpen()) {
                _solutionLoadObserver.OnAfterOpenSolution(0, 0);
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
            await FilterProjectsCommand.InitializeAsync(this);
        }


        [SuppressMessage("Usage", "VSTHRD104:Offer async methods", Justification = "No other way to use async code in Dispose().")]
        protected override void Dispose(bool disposing) {
            ExtensionThreadHelper.JoinableTaskFactory.Run(DisposeAsync);
            base.Dispose(disposing);
        }


        private async Task DisposeAsync() {
            if (_solution is not null && _solutionEventsCookie != 0) {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                _solution.UnadviseSolutionEvents(_solutionEventsCookie);
                _solutionEventsCookie = 0;
                _solutionLoadObserver = null;
            }
        }

    }

}
