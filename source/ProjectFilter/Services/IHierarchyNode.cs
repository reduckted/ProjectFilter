using Microsoft.VisualStudio.Imaging.Interop;
using System;
using System.Collections.Generic;


namespace ProjectFilter.Services {

    public interface IHierarchyNode {

        Guid Identifier { get; }


        string Name { get; }


        bool IsFolder { get; }


        bool IsLoaded { get; }


        ImageMoniker CollapsedIcon { get; }


        ImageMoniker ExpandedIcon { get; }


        IHierarchyNode? Parent { get; }


        IReadOnlyList<IHierarchyNode> Children { get; }

    }

}
