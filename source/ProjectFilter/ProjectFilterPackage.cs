using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using ProjectFilter.Commands;
using ProjectFilter.Services;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter;


[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
[Guid(VSCommandTable.ProjectFilterPackage.GuidString)]
[ProvideService(typeof(IExtensionSettings), IsAsyncQueryable = true)]
[ProvideService(typeof(IFilterService), IsAsyncQueryable = true)]
[ProvideService(typeof(IHierarchyProvider), IsAsyncQueryable = true)]
[ProvideService(typeof(ILogger), IsAsyncQueryable = true)]
[ProvideService(typeof(ISolutionExplorer), IsAsyncQueryable = true)]
[ProvideService(typeof(ISolutionSettingsManager), IsAsyncQueryable = true)]
[ProvideService(typeof(IWaitDialogFactory), IsAsyncQueryable = true)]
public sealed class ProjectFilterPackage : ToolkitPackage {

    private ISolutionSettingsManager? _solutionSettingsManager;


    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
        await base.InitializeAsync(cancellationToken, progress);

        AddService<IExtensionSettings>(async () => await ExtensionSettings.CreateAsync());
        AddService<FilterService, IFilterService>(new FilterService());
        AddService<HierarchyProvider, IHierarchyProvider>(new HierarchyProvider());
        AddService<Logger, ILogger>(new Logger());
        AddService<SolutionExplorer, ISolutionExplorer>(new SolutionExplorer());
        AddService<ISolutionSettingsManager>(async () => new SolutionSettingsManager(this, await this.GetServiceAsync<ILogger, ILogger>()));
        AddService<WaitDialogFactory, IWaitDialogFactory>(new WaitDialogFactory());

        AddOptionKey(SolutionSettingsManager.OptionsKey);

        await FilterProjectsCommand.InitializeAsync(this);

        await SolutionLoadObserver.InitializeAsync();

        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        _solutionSettingsManager = this.GetService<ISolutionSettingsManager, ISolutionSettingsManager>();
    }


    private void AddService<TService, TInterface>(TService service) where TService : class, TInterface {
        AddService(
            typeof(TInterface),
            (container, cancellation, type) => Task.FromResult<object>(service),
            true
        );
    }


    private void AddService<TInterface>(Func<Task<TInterface>> serviceFactory) where TInterface : class {
        AddService(
            typeof(TInterface),
            async (container, cancellation, type) => await serviceFactory(),
            true
        );
    }


    protected override void OnLoadOptions(string key, Stream stream) {
        if (key == SolutionSettingsManager.OptionsKey) {
            _solutionSettingsManager?.Load(stream);
        }
    }


    protected override void OnSaveOptions(string key, Stream stream) {
        if (key == SolutionSettingsManager.OptionsKey) {
            _solutionSettingsManager?.Save(stream);
        }
    }
}
