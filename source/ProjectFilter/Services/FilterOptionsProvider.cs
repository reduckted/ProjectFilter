using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using ProjectFilter.UI;
using ProjectFilter.UI.Utilities;
using System.Threading.Tasks;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;


namespace ProjectFilter.Services {

    public class FilterOptionsProvider : IFilterOptionsProvider {

        private readonly IAsyncServiceProvider _provider;


        public FilterOptionsProvider (IAsyncServiceProvider provider) {
            _provider = provider;
        }


        public async Task<FilterOptions?> GetOptionsAsync() {
            IHierarchyProvider hierarchyProvider;
            IExtensionSettings settings;


            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            hierarchyProvider = await _provider.GetServiceAsync<IHierarchyProvider, IHierarchyProvider>();
            settings = await _provider.GetServiceAsync<IExtensionSettings, IExtensionSettings>();

            using (var vm = new FilterDialogViewModel(await hierarchyProvider.GetHierarchyAsync(), Debouncer.Create, SearchUtilities.CreateSearchQuery)) {
                FilterDialog dialog;


                vm.LoadProjectDependencies = settings.LoadProjectDependencies;

                dialog = new FilterDialog {
                    DataContext = vm
                };

                if (dialog.ShowModal().GetValueOrDefault() && (vm.Result != null)) {
                    settings.LoadProjectDependencies = vm.Result.LoadProjectDependencies;

                    return vm.Result;
                }
            }

            return null;
        }

    }

}
