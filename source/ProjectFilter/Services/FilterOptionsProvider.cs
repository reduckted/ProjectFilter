using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using ProjectFilter.UI;
using ProjectFilter.UI.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;


namespace ProjectFilter.Services {

    public class FilterOptionsProvider : IFilterOptionsProvider {

        private readonly IAsyncServiceProvider _provider;


        public FilterOptionsProvider(IAsyncServiceProvider provider) {
            _provider = provider;
        }


        public async Task<FilterOptions?> GetOptionsAsync() {
            IHierarchyProvider hierarchyProvider;
            IExtensionSettings settings;
            Func<Task<IEnumerable<IHierarchyNode>>> hierarchyFactory;


            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            hierarchyProvider = await _provider.GetServiceAsync<IHierarchyProvider, IHierarchyProvider>();
            settings = await _provider.GetServiceAsync<IExtensionSettings, IExtensionSettings>();

            hierarchyFactory = async () => await hierarchyProvider.GetHierarchyAsync();

            using (var vm = new FilterDialogViewModel(hierarchyFactory, Debouncer.Create, SearchUtilities.CreateSearchQuery)) {
                FilterDialog dialog;


                await settings.LoadAsync();
                vm.LoadProjectDependencies = settings.LoadProjectDependencies;

                dialog = new FilterDialog {
                    DataContext = vm
                };

                if (dialog.ShowModal().GetValueOrDefault() && (vm.Result is not null)) {
                    settings.LoadProjectDependencies = vm.Result.LoadProjectDependencies;
                    await settings.SaveAsync();

                    return vm.Result;
                }
            }

            return null;
        }

    }

}
