using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text;
using ProjectFilter.Services;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;


namespace ProjectFilter.UI;


public class HierarchyTreeViewItem : ObservableObject {

    private readonly IHierarchyNode _node;
    private bool? _isChecked;
    private bool _isExpanded;
    private IReadOnlyCollection<Span>? _highlightSpans;
    private string? _path;


    public HierarchyTreeViewItem(
        IHierarchyNode node,
        bool isExpanded,
        IEnumerable<HierarchyTreeViewItem> children
    ) {
        _node = node;
        Children = new HierarchyTreeViewItemCollection(children);

        _isChecked = false;
        _isExpanded = isExpanded;

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


    public IReadOnlyCollection<Span>? HighlightSpans {
        get { return _highlightSpans; }
        set { SetProperty(ref _highlightSpans, value); }
    }


    public string Path => _path ??= Parent is not null ? $"{Parent.Path}/{Name}" : Name;


    public bool Filter(ITextFilter filter) {
        ImmutableArray<Span> matches;


        if (filter is null) {
            throw new ArgumentNullException(nameof(filter));
        }

        Children.Filter(filter);
        matches = filter.TryMatch(Path);

        // The matches specify the spans in the `Path` that match, but we highlight the matching
        // spans in the `Name`. Translate the spans so that they apply to the `Name` property.
        if (matches.Length > 0) {
            HighlightSpans = TranslateSpansFromPathToName(matches);
        } else {
            HighlightSpans = null;
        }

        return matches.Length > 0 || Children.Count > 0;
    }


    private IReadOnlyCollection<Span>? TranslateSpansFromPathToName(ImmutableArray<Span> matches) {
        int nameStartOffset;


        // The name is always at the end of the path, so we can determine where 
        // it starts by subtracting its length from the length of the path.
        nameStartOffset = Path.Length - Name.Length;

        if (nameStartOffset == 0) {
            return matches;
        }

        return matches
            // Exclude any spans that end before the name starts.
            .Where((x) => x.End > nameStartOffset)
            // Create new spans starting from zero, and truncate any ranges start
            // before the start of the name but end after the start of the name.
            .Select((x) => Span.FromBounds(Math.Max(x.Start - nameStartOffset, 0), x.End - nameStartOffset))
            .ToList();
    }


    public void ClearFilter() {
        Children.ClearFilter();
        HighlightSpans = null;
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
