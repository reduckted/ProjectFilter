using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services {

    public class SolutionLoadObserver : IAsyncInitializable, IVsSolutionEvents {

#nullable disable
        private IFilterService _filterService;
        private DTE2 _dte;
#nullable restore


        public async Task InitializeAsync(IAsyncServiceProvider provider, CancellationToken cancellationToken) {
            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _filterService = await provider.GetServiceAsync<IFilterService, IFilterService>();
            _dte = await provider.GetServiceAsync<DTE, DTE2>();
        }


        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution) {
            HideUnloadedProjectsInSolutionExplorerAsync().FileAndForget(nameof(SolutionLoadObserver));
            return VSConstants.S_OK;
        }


        private async Task HideUnloadedProjectsInSolutionExplorerAsync() {
            // The solution has opened, but Solution Explorer may not have been populated
            // yet, so keep looping until there is something in Solution Explorer.
            while (true) {
                await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_dte.ToolWindows.SolutionExplorer.UIHierarchyItems.Count > 0) {
                    _filterService.ShowOnlyLoadedProjects();
                    return;
                }

                await Task.Delay(500);
            }
        }


        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) {
            return VSConstants.S_OK;
        }


        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) {
            return VSConstants.S_OK;
        }


        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) {
            return VSConstants.S_OK;
        }


        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) {
            return VSConstants.S_OK;
        }


        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) {
            return VSConstants.S_OK;
        }


        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) {
            return VSConstants.S_OK;
        }


        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) {
            return VSConstants.S_OK;
        }


        public int OnBeforeCloseSolution(object pUnkReserved) {
            return VSConstants.S_OK;
        }


        public int OnAfterCloseSolution(object pUnkReserved) {
            return VSConstants.S_OK;
        }

    }

}
