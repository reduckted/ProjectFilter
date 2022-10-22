using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace ProjectFilter.Services;


public interface ISolutionExplorer {

    Task<bool?> IsEmptyAsync();


    Task HideUnloadedProjectsAsync();


    Task ExpandAsync(IEnumerable<Guid> projects);


    Task CollapseAsync(IEnumerable<Guid> projects);


    Task<IEnumerable<Guid>> GetExpandedFoldersAsync();

}
