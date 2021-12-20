using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using ProjectFilter.Services;
using System;
using System.Collections.Generic;
using System.Linq;


namespace ProjectFilter.UI;


public class HierarchyTreeViewItem : ObservableObject {

    private readonly IHierarchyNode _node;
    private bool? _isChecked;
    private bool _isExpanded;
    private string _highlightText;


    public HierarchyTreeViewItem(
        IHierarchyNode node,
        IEnumerable<HierarchyTreeViewItem> children
    ) {
        _node = node;
        Children = new HierarchyTreeViewItemCollection(children);

        _isChecked = false;
        _isExpanded = true;
        _highlightText = "";

        // Connect the children to this parent item.
        foreach (var child in Children) {
            child.Parent = this;
        }

        // If there are children, then that collection could
        // change as the items are filtered. We will need to
        // update our checked state when that collection changes.
        if (Children.Count > 0) {
            Children.CollectionChanged += (s, e) => UpdateCheckedStateFromChildren(true);
        }
    }


    public Guid Identifier {
        get { return _node.Identifier; }
    }


    public string Name {
        get { return _node.Name; }
    }


    public bool IsFolder {
        get { return _node.IsFolder; }
    }


    public HierarchyTreeViewItem? Parent { get; private set; }


    public HierarchyTreeViewItemCollection Children { get; }


    public bool? IsChecked {
        get { return _isChecked; }
        set { SetIsChecked(value, updateChildren: true, updateParent: true); }
    }


    public bool IsExpanded {
        get { return _isExpanded; }
        set {
            SetProperty(ref _isExpanded, value);
            NotifyPropertyChanged(nameof(Icon));
        }
    }


    public ImageMoniker Icon {
        get {
            return IsExpanded ? _node.ExpandedIcon : _node.CollapsedIcon;
        }
    }


    public string HighlightText {
        get { return _highlightText; }
        set { SetProperty(ref _highlightText, value); }
    }


    public bool Filter(HierarchySearchMatchEvaluator evaluator) {
        bool isMatch;


        if (evaluator is null) {
            throw new ArgumentNullException(nameof(evaluator));
        }

        Children.Filter(evaluator);
        isMatch = evaluator.IsSearchMatch(Name);

        if (isMatch && evaluator.SearchTerms.Count == 1) {
            HighlightText = evaluator.SearchTerms[0];
        } else {
            HighlightText = "";
        }

        return isMatch || Children.Count > 0;
    }


    public void ClearFilter() {
        Children.ClearFilter();
        HighlightText = "";
    }


    public IEnumerable<HierarchyTreeViewItem> DescendantsAndSelf() {
        yield return this;

        foreach (var descendant in Children.SelectMany((x) => x.DescendantsAndSelf())) {
            yield return descendant;
        }
    }


    internal void SetIsChecked(bool? value, bool updateChildren, bool updateParent) {
        if (_isChecked != value) {
            SetProperty(ref _isChecked, value, propertyName: nameof(IsChecked));

            if (updateChildren) {
                // If our new value is not indeterminate, then make that value 
                // flow down into all descendants, but don't update the parent of 
                // the descendants, because that would just update our checked state
                // again and possibly change it from non-indeterminate to indeterminate.
                if (_isChecked.HasValue) {
                    foreach (var child in Children) {
                        child.SetIsChecked(value, updateChildren: true, updateParent: false);
                    }
                }
            }

            if (updateParent) {
                Parent?.UpdateCheckedStateFromChildren(true);
            }
        }
    }


    private void UpdateCheckedStateFromChildren(bool updateParent) {
        SetIsChecked(Children.CalculateCheckedState(), false, updateParent);
    }


    public override string ToString() {
        return Name + (_node.IsFolder ? "/" : "");
    }

}
