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
        TextFilterFactory textFilterFactory;
        Func<Task<IEnumerable<IHierarchyNode>>> hierarchyFactory;
        IExtensionSettings globalSettings;
        ISolutionSettingsManager solutionSettingsManager;
        SolutionSettings? solutionSettings;


        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        hierarchyProvider = await VS.GetRequiredServiceAsync<IHierarchyProvider, IHierarchyProvider>();
        textFilterFactory = await CreateTextFilterFactoryAsync();
        hierarchyFactory = async () => await hierarchyProvider.GetHierarchyAsync();

        globalSettings = await VS.GetRequiredServiceAsync<IExtensionSettings, IExtensionSettings>();
        await globalSettings.LoadAsync();

        solutionSettingsManager = await VS.GetRequiredServiceAsync<ISolutionSettingsManager, ISolutionSettingsManager>();
        solutionSettings = await solutionSettingsManager.GetSettingsAsync();

        using (var vm = new FilterDialogViewModel(hierarchyFactory, Debouncer.Create, textFilterFactory, solutionSettings?.Nodes, Package.JoinableTaskFactory)) {
            FilterDialog dialog;
            bool result;


            // Initialize the settings with the settings that were last
            // used for this solution. If this solution hasn't been
            // filtered before, then we'll use the global settings.
            vm.LoadProjectDependencies = solutionSettings?.LoadProjectDependencies ?? globalSettings.LoadProjectDependencies;
            vm.UseRegularExpressions = solutionSettings?.UseRegularExpressions ?? globalSettings.UseRegularExpressions;
            vm.ExpandLoadedProjects = solutionSettings?.ExpandLoadedProjects ?? globalSettings.ExpandLoadedProjects;

            dialog = new FilterDialog {
                DataContext = vm
            };

            result = dialog.ShowModal().GetValueOrDefault();

            // Save the settings for this solution, even if filtering
            // was cancelled. This saves the user from having to re-apply
            // the settings the next time they open the dialog.
            solutionSettings = new SolutionSettings {
                LoadProjectDependencies = vm.LoadProjectDependencies,
                UseRegularExpressions = vm.UseRegularExpressions,
                ExpandLoadedProjects = vm.ExpandLoadedProjects
            };
            PopulateNodeSettings(vm.Items, solutionSettings.Nodes);

            solutionSettingsManager.SetSettings(solutionSettings);

            // Also save the settings to the global settings so that the same settings
            // can be used for solutions that don't have their own settings yet.
            globalSettings.LoadProjectDependencies = vm.LoadProjectDependencies;
            globalSettings.UseRegularExpressions = vm.UseRegularExpressions;
            globalSettings.ExpandLoadedProjects = vm.ExpandLoadedProjects;
            await globalSettings.SaveAsync();

            if (result) {
                return vm.Result;
            }
        }

        return null;
    }


    private static void PopulateNodeSettings(HierarchyTreeViewItemCollection items, Dictionary<string, SolutionNodeSettings> settings) {
        foreach (var item in items) {
            SolutionNodeSettings nodeSettings;


            nodeSettings = new SolutionNodeSettings { IsExpanded = item.IsExpanded };
            PopulateNodeSettings(item.Children, nodeSettings.Children);

            settings[item.Name] = nodeSettings;
        }
    }


    private static async Task<TextFilterFactory> CreateTextFilterFactoryAsync() {
        IPatternMatcherFactory patternMatcherFactory;


        patternMatcherFactory = await VS.GetMefServiceAsync<IPatternMatcherFactory>();

        return (pattern, isRegularExpression) => isRegularExpression
            ? new RegexTextFilter(pattern)
            : new PatternTextFilter(pattern, patternMatcherFactory);
    }

}
