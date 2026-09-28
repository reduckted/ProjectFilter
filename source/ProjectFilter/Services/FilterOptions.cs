using System;
using System.Collections.Generic;


namespace ProjectFilter.Services;


public class FilterOptions {

    public FilterOptions(
        IEnumerable<Guid> projectsToLoad,
        IEnumerable<Guid> projectsToUnload,
        bool loadProjectDependencies,
        bool expandLoadedProjects,
        bool hideSolutionFoldersWithoutLoadedProjects,
        bool hideSolutionFoldersWithOnlyFiles
    ) {
        ProjectsToLoad = [.. projectsToLoad];
        ProjectsToUnload = [.. projectsToUnload];
        LoadProjectDependencies = loadProjectDependencies;
        ExpandLoadedProjects = expandLoadedProjects;
        HideSolutionFoldersWithoutLoadedProjects = hideSolutionFoldersWithoutLoadedProjects;
        HideSolutionFoldersWithOnlyFiles = hideSolutionFoldersWithOnlyFiles;
    }


    public IReadOnlyCollection<Guid> ProjectsToLoad { get; }


    public IReadOnlyCollection<Guid> ProjectsToUnload { get; }


    public bool LoadProjectDependencies { get; }


    public bool ExpandLoadedProjects { get; }


    public bool HideSolutionFoldersWithoutLoadedProjects { get; }


    public bool HideSolutionFoldersWithOnlyFiles { get; }

}
