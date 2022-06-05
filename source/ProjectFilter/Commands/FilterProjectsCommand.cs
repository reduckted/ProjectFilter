using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.PatternMatching;
using ProjectFilter.Services;
using ProjectFilter.UI;
using ProjectFilter.UI.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Commands;


[Command(VSCommandTable.ProjectFilterPackage.FilterProjectsCommand)]
public sealed class FilterProjectsCommand : BaseCommand<FilterProjectsCommand> {

    protected async override Task ExecuteAsync(OleMenuCmdEventArgs e) {
        IFilterService filterService;
        FilterOptions? options;


        filterService = await VS.GetRequiredServiceAsync<IFilterService, IFilterService>();

        options = await GetOptionsAsync();

        if (options is not null) {
            await filterService.ApplyAsync(options);
        }
    }


    private async Task<FilterOptions?> GetOptionsAsync() {
        IHierarchyProvider hierarchyProvider;
        IExtensionSettings settings;
        TextFilterFactory textFilterFactory;
        Func<Task<IEnumerable<IHierarchyNode>>> hierarchyFactory;


        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        hierarchyProvider = await VS.GetRequiredServiceAsync<IHierarchyProvider, IHierarchyProvider>();
        settings = await VS.GetRequiredServiceAsync<IExtensionSettings, IExtensionSettings>();

        textFilterFactory = await CreateTextFilterFactoryAsync();
        hierarchyFactory = async () => await hierarchyProvider.GetHierarchyAsync();

        using (var vm = new FilterDialogViewModel(hierarchyFactory, Debouncer.Create, textFilterFactory, Package.JoinableTaskFactory)) {
            FilterDialog dialog;
            bool result;


            await settings.LoadAsync();
            vm.LoadProjectDependencies = settings.LoadProjectDependencies;
            vm.UseRegularExpressions = settings.UseRegularExpressions;

            dialog = new FilterDialog {
                DataContext = vm
            };

            result = dialog.ShowModal().GetValueOrDefault();

            settings.LoadProjectDependencies = vm.LoadProjectDependencies;
            settings.UseRegularExpressions = vm.UseRegularExpressions;
            await settings.SaveAsync();

            if (result) {
                return vm.Result;
            }
        }

        return null;
    }


    private static async Task<TextFilterFactory> CreateTextFilterFactoryAsync() {
        IPatternMatcherFactory patternMatcherFactory;


        patternMatcherFactory = await VS.GetMefServiceAsync<IPatternMatcherFactory>();

        return (pattern, isRegularExpression) => isRegularExpression
            ? new RegexTextFilter(pattern)
            : new PatternTextFilter(pattern, patternMatcherFactory);
    }

}
