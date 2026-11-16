using System;
using System.Collections.Generic;


namespace ProjectFilter.UI;


public class FilterDialogResult {

    public FilterDialogResult(
        IEnumerable<Guid> projectsToLoad,
        IEnumerable<Guid> projectsToUnload
    ) {
        ProjectsToLoad = [.. projectsToLoad];
        ProjectsToUnload = [.. projectsToUnload];
    }


    public IReadOnlyCollection<Guid> ProjectsToLoad { get; }


    public IReadOnlyCollection<Guid> ProjectsToUnload { get; }

}
