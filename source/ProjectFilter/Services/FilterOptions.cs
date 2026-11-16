using System;
using System.Collections.Generic;


namespace ProjectFilter.Services;


public class FilterOptions {

    public FilterOptions(
        IEnumerable<Guid> projectsToLoad,
        IEnumerable<Guid> projectsToUnload,
        FilterBehavior behavior
    ) {
        ProjectsToLoad = [.. projectsToLoad];
        ProjectsToUnload = [.. projectsToUnload];
        Behavior = behavior;
    }


    public IReadOnlyCollection<Guid> ProjectsToLoad { get; }


    public IReadOnlyCollection<Guid> ProjectsToUnload { get; }


    public FilterBehavior Behavior { get; }

}
