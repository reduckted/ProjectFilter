using System;
using System.Collections.Generic;
using System.Linq;


namespace ProjectFilter.Services;


public class FilterOptions {

    public FilterOptions(
        IEnumerable<Guid> projectsToLoad,
        IEnumerable<Guid> projectsToUnload,
        bool loadProjectDependencies,
        bool expandLoadedProjects
    ) {
        ProjectsToLoad = projectsToLoad.ToList();
        ProjectsToUnload = projectsToUnload.ToList();
        LoadProjectDependencies = loadProjectDependencies;
        ExpandLoadedProjects = expandLoadedProjects;
    }


    public IReadOnlyCollection<Guid> ProjectsToLoad { get; }


    public IReadOnlyCollection<Guid> ProjectsToUnload { get; }


    public bool LoadProjectDependencies { get; }


    public bool ExpandLoadedProjects{ get; }

}
