using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;


namespace ProjectFilter.Helpers {

    public abstract class TreeItem {

        protected TreeItem(HierarchyData data) {
            Data = data;
            Children = new List<TreeItem>();
        }


        public HierarchyData Data { get; }


        public IVsHierarchy? Hierarchy { get; set; }


        public TreeItem? Parent { get; set; }


        public List<TreeItem> Children { get; }

    }

}
