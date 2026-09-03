using System;
using System.Collections.Generic;
using System.Linq;


namespace ProjectFilter.Services;


public class FilterOptions {

    public FilterOptions(
        IEnumerable<Guid> projectsToLoad,
        IEnumerable<Guid> projectsToUnload,
        bool loadProjectDependencies,
        bool expandLoadedProjects,
        bool hideSolutionFoldersWithoutLoadedProjects
    ) {
        ProjectsToLoad = [.. projectsToLoad];
        ProjectsToUnload = [.. projectsToUnload];
        LoadProjectDependencies = loadProjectDependencies;
        ExpandLoadedProjects = expandLoadedProjects;
        HideSolutionFoldersWithoutLoadedProjects = hideSolutionFoldersWithoutLoadedProjects;
    }


    public IReadOnlyCollection<Guid> ProjectsToLoad { get; }


    public IReadOnlyCollection<Guid> ProjectsToUnload { get; }


    public bool LoadProjectDependencies { get; }


    public bool ExpandLoadedProjects{ get; }


    public bool HideSolutionFoldersWithoutLoadedProjects { get; }

}
