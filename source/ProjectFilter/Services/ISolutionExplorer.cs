using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;


namespace ProjectFilter.Services;


[Guid("45198243-f9cd-3867-b7a7-9d0e44530035")]
public interface ISolutionExplorer {

    Task<bool?> IsEmptyAsync();


    Task HideUnloadedProjectsAsync();


    Task ExpandAsync(IEnumerable<Guid> projects);


    Task CollapseAsync(IEnumerable<Guid> projects);


    Task<IEnumerable<Guid>> GetExpandedFoldersAsync();

}
