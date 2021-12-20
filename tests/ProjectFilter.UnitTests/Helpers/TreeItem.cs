using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;


namespace ProjectFilter.Helpers;


internal class TreeItem {

    public TreeItem(HierarchyData data) {
        Data = data;
        Children = new List<TreeItem>();
    }


    public HierarchyData Data { get; }


    public IVsHierarchy? Hierarchy { get; set; }


    public TreeItem? Parent { get; set; }


    public IList<TreeItem> Children { get; }

}
