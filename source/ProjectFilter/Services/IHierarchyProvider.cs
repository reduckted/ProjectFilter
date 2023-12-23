using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;


namespace ProjectFilter.Services;


[Guid("7eaa8e03-e6fe-35c7-a27d-5415b28d58df")]
public interface IHierarchyProvider {

    Task<IEnumerable<IHierarchyNode>> GetHierarchyAsync();

}
