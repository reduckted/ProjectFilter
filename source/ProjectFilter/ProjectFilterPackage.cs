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
    [ProvideService(typeof(ISolutionExplorer), IsAsyncQueryable = true)]
    public sealed class ProjectFilterPackage : ToolkitPackage {

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
            await base.InitializeAsync(cancellationToken, progress);

            AddService(typeof(IExtensionSettings), async (container, cancellation, type) => await ExtensionSettings.CreateAsync(), true);
            AddService<FilterOptionsProvider, IFilterOptionsProvider>(new FilterOptionsProvider(this));
            AddService<FilterService, IFilterService>(new FilterService());
            AddService<HierarchyProvider, IHierarchyProvider>(new HierarchyProvider());
            AddService<Logger, ILogger>(new Logger());
            AddService<SolutionExplorer, ISolutionExplorer>(new SolutionExplorer());
            AddService<WaitDialogFactory, IWaitDialogFactory>(new WaitDialogFactory());

            await FilterProjectsCommand.InitializeAsync(this);

            await SolutionLoadObserver.InitializeAsync();
        }


        private void AddService<TService, TInterface>(TService service) where TService : class, TInterface {
            AddService(
                typeof(TInterface),
                (container, cancellation, type) => Task.FromResult<object>(service),
                true
            );
        }

    }

}
