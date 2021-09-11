using System;
using System.Runtime.InteropServices;
using System.Threading;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
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

        private SolutionLoadObserver? _solutionLoadObserver;


        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
            await base.InitializeAsync(cancellationToken, progress);

            AddServices();
            await AddCommandsAsync();

            _solutionLoadObserver = new SolutionLoadObserver(this);
            await _solutionLoadObserver.InitializeAsync(cancellationToken);
        }


        private void AddServices() {
            AddService(typeof(IExtensionSettings), async (container, cancellation, type) => await ExtensionSettings.CreateAsync());
            AddService<FilterOptionsProvider, IFilterOptionsProvider>(new FilterOptionsProvider(this));
            AddService<FilterService, IFilterService>(new FilterService(this));
            AddService<HierarchyProvider, IHierarchyProvider>(new HierarchyProvider());
            AddService<Logger, ILogger>(new Logger());
            AddService<WaitDialogFactory, IWaitDialogFactory>(new WaitDialogFactory());
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

    }

}
