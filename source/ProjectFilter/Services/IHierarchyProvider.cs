using System.Collections.Generic;
using System.Threading.Tasks;


namespace ProjectFilter.Services;


public interface IHierarchyProvider {

    Task<IEnumerable<IHierarchyNode>> GetHierarchyAsync();

}
