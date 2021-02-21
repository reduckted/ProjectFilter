using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using ProjectFilter.UI;
using ProjectFilter.UI.Utilities;
using System.Threading;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services {

    public class FilterOptionsProvider : IAsyncInitializable, IFilterOptionsProvider {

#nullable disable
        private IHierarchyProvider _hierarchyProvider;
        private IExtensionSettings _settings;
#nullable restore


        public async Task InitializeAsync(IAsyncServiceProvider provider, CancellationToken cancellationToken) {
            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _hierarchyProvider = await provider.GetServiceAsync<IHierarchyProvider, IHierarchyProvider>();
            _settings = await provider.GetServiceAsync<IExtensionSettings, IExtensionSettings>();
        }


        public FilterOptions? GetOptions() {
            ThreadHelper.ThrowIfNotOnUIThread();

            using (var vm = new FilterDialogViewModel(_hierarchyProvider.GetHierarchy(), Debouncer.Create, SearchUtilities.CreateSearchQuery)) {
                FilterDialog dialog;


                vm.LoadProjectDependencies = _settings.LoadProjectDependencies;

                dialog = new FilterDialog {
                    DataContext = vm
                };

                if (dialog.ShowModal().GetValueOrDefault() && (vm.Result != null)) {
                    _settings.LoadProjectDependencies = vm.Result.LoadProjectDependencies;

                    return vm.Result;
                }
            }

            return null;
        }

    }

}
