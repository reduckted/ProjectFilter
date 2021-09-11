using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.PlatformUI;
using ProjectFilter.UI;
using ProjectFilter.UI.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;



namespace ProjectFilter.Services {

    public class FilterOptionsProvider : IFilterOptionsProvider {

        public async Task<FilterOptions?> GetOptionsAsync() {
            IHierarchyProvider hierarchyProvider;
            IExtensionSettings settings;
            Func<Task<IEnumerable<IHierarchyNode>>> hierarchyFactory;


            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            hierarchyProvider = await VS.GetRequiredServiceAsync<IHierarchyProvider, IHierarchyProvider>();
            settings = await VS.GetRequiredServiceAsync<IExtensionSettings, IExtensionSettings>();

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
