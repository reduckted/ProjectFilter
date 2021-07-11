using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services {

    public class SolutionLoadObserver : IAsyncInitializable {

        private readonly IAsyncServiceProvider _provider;


        public SolutionLoadObserver(IAsyncServiceProvider provider) {
            _provider = provider;
        }


        public async Task InitializeAsync(CancellationToken cancellationToken) {
            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            VS.Events.SolutionEvents.OnAfterOpenSolution += OnAfterOpenSolution;

            // If a solution is already open, then we need to tell the solution
            // observer to do whatever it does after a solution is opened.
            if ((await VS.Services.GetSolutionAsync()).IsOpen()) {
                await HideUnloadedProjectsInSolutionExplorerAsync();
            }
        }


        private void OnAfterOpenSolution(SolutionItem? item) {
            HideUnloadedProjectsInSolutionExplorerAsync().FireAndForget();
        }


        private async Task HideUnloadedProjectsInSolutionExplorerAsync() {
            DTE2? dte;
            IFilterService filterService;


            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            dte = await VS.GetServiceAsync<DTE, DTE2>();

            if (dte is null) {
                return;
            }

            filterService = await _provider.GetServiceAsync<IFilterService, IFilterService>();

            // The solution has opened, but Solution Explorer may not have been populated
            // yet, so keep looping until there is something in Solution Explorer.
            while (true) {
                if (dte.ToolWindows.SolutionExplorer.UIHierarchyItems.Count > 0) {
                    await filterService.ShowOnlyLoadedProjectsAsync();
                    return;
                }

                await Task.Delay(500);
            }
        }

    }

}
