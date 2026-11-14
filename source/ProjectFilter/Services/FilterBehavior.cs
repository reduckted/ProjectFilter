namespace ProjectFilter.Services;


public class FilterBehavior {

    public FilterBehavior(
        SolutionSettings? solutionSettings,
        IExtensionSettings globalSettings
    ) {
        // Initialize the settings with the settings that were last
        // used for this solution. If this solution hasn't been
        // filtered before, then we'll use the global settings.

        LoadProjectDependencies = solutionSettings?.LoadProjectDependencies ?? globalSettings.LoadProjectDependencies;

        ExpandLoadedProjects = solutionSettings?.ExpandLoadedProjects ?? globalSettings.ExpandLoadedProjects;

        HideSolutionFoldersWithoutLoadedProjects =
            solutionSettings?.HideSolutionFoldersWithoutLoadedProjects ??
            globalSettings.HideSolutionFoldersWithoutLoadedProjects;

        HideSolutionFoldersWithOnlyFiles =
            solutionSettings?.HideSolutionFoldersWithOnlyFiles ??
            globalSettings.HideSolutionFoldersWithOnlyFiles;
    }


    public bool LoadProjectDependencies { get; }


    public bool ExpandLoadedProjects { get; }


    public bool HideSolutionFoldersWithoutLoadedProjects { get; }


    public bool HideSolutionFoldersWithOnlyFiles { get; }

}
