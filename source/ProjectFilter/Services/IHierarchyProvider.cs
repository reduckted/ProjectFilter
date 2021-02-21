using System.Collections.Generic;


namespace ProjectFilter.Services {

    public interface IHierarchyProvider {

        IEnumerable<IHierarchyNode> GetHierarchy();

    }

}
