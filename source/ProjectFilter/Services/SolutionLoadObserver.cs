using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services {

    public static class SolutionLoadObserver {

        public static async Task InitializeAsync() {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            VS.Events.SolutionEvents.OnAfterOpenSolution += OnAfterOpenSolution;

            // If a solution is already open, then we need to tell the solution
            // observer to do whatever it does after a solution is opened.
            if ((await VS.Services.GetSolutionAsync()).IsOpen()) {
                await HideUnloadedProjectsInSolutionExplorerAsync();
            }
        }


        private static void OnAfterOpenSolution(SolutionItem? item) {
            HideUnloadedProjectsInSolutionExplorerAsync().FireAndForget();
        }


        private static async Task HideUnloadedProjectsInSolutionExplorerAsync() {
            ISolutionExplorer solutionExplorer;


            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            solutionExplorer = await VS.GetRequiredServiceAsync<ISolutionExplorer, ISolutionExplorer>();

            // The solution has opened, but Solution Explorer may not have been populated
            // yet, so keep looping until there is something in Solution Explorer.
            while (true) {
                bool? empty;


                empty = await solutionExplorer.IsEmptyAsync();

                // If we know for sure that Solution Explorer is empty, then wait
                // a bit and try again. If we know it's not empty then we can hide
                // the unloaded projects. If we don't know if it's empty or not, then
                // we'll still try to hide the unloaded projects because if we don't now,
                // then we may never know, and then we'd end up looping here forever.
                if (empty.GetValueOrDefault()) {
                    await Task.Delay(1000);
                } else {
                    break;
                }
            }

            await solutionExplorer.HideUnloadedProjectsAsync();
        }

    }

}
