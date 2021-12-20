using Microsoft.VisualStudio.Imaging.Interop;
using System;
using System.Collections.Generic;


namespace ProjectFilter.Services;


public class HierarchyNode : IHierarchyNode {

    private readonly List<HierarchyNode> _children;


    public HierarchyNode(
        Guid identifier,
        string name,
        ImageMoniker collapsedIcon,
        ImageMoniker expandedIcon
    ) {
        Identifier = identifier;
        Name = name;
        CollapsedIcon = collapsedIcon;
        ExpandedIcon = expandedIcon;
        _children = new List<HierarchyNode>();

        IsFolder = false;
        IsLoaded = false;
    }


    public Guid Identifier { get; }


    public string Name { get; }


    public bool IsFolder { get; set; }


    public bool IsLoaded { get; set; }


    public ImageMoniker ExpandedIcon { get; }


    public ImageMoniker CollapsedIcon { get; }


    public IHierarchyNode? Parent { get; set; }


    public IReadOnlyList<IHierarchyNode> Children {
        get {
            return _children;
        }
    }


    public IList<HierarchyNode> ChildrenList {
        get {
            return _children;
        }
    }


    public override string ToString() {
        return $"{Name}{(IsFolder ? "/" : "")}{(!IsLoaded ? " (unloaded)" : "")}";
    }

}
