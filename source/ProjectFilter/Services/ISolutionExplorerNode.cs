using System.Collections.Generic;


namespace ProjectFilter.Services;


public interface ISolutionExplorerNode {

    bool IsFolder { get; }


    bool IsProject { get; }


    bool IsLoaded { get; }


    IReadOnlyList<ISolutionExplorerNode> Children { get; }

}
