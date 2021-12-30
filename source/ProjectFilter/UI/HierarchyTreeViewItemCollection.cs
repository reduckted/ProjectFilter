using ProjectFilter.Services;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;


namespace ProjectFilter.UI;


public class HierarchyTreeViewItemCollection : IReadOnlyCollection<HierarchyTreeViewItem>, INotifyCollectionChanged {

    private readonly List<HierarchyTreeViewItem> _originalItems;
    private readonly List<HierarchyTreeViewItem> _items;


    public HierarchyTreeViewItemCollection(IEnumerable<HierarchyTreeViewItem> items) {
        // Store the original items so that we always 
        // have a complete list to use when filtering.
        _originalItems = new List<HierarchyTreeViewItem>(items);

        // Copy the original items into another list that is the collection 
        // we will expose from this class. Note that we use the original 
        // items as the source rather than the given collection, because 
        // the given collection can be lazy-evaluated, and enumerating 
        // it twice would result in different objects being created.
        _items = new List<HierarchyTreeViewItem>(_originalItems);
    }


    public int Count {
        get { return _items.Count; }
    }


    public bool? CalculateCheckedState() {
        bool? allChecked;


        if (Count == 0) {
            return false;
        }

        allChecked = null;

        foreach (var child in this) {
            if (!child.IsChecked.HasValue) {
                // The child is neither checked nor unchecked, so the 
                // checked state of the collection is indeterminate.
                return null;

            } else if (allChecked.HasValue) {
                // The children are a mix of checked and 
                // unchecked, so the state is indeterminate.
                if (allChecked != child.IsChecked) {
                    return null;
                }

            } else {
                // This must be the first item we've 
                // seen, so start with its checked state.
                allChecked = child.IsChecked;
            }
        }

        return allChecked;
    }


    public void Filter(ITextFilter filter) {
        // If there's no original items, then there's nothing 
        // to filter and the collection won't ever change.
        if (_originalItems.Count > 0) {
            // Replace the items with the 
            // original items that meet the filter.
            _items.Clear();
            _items.AddRange(_originalItems.Where((x) => x.Filter(filter)));

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }


    public void ClearFilter() {
        // If there's no original items, then nothing 
        // was filtered and there's nothing to reset.
        if (_originalItems.Count > 0) {
            // Restore the original items and 
            // clear the filter in each item.
            _items.Clear();
            _items.AddRange(_originalItems);
            _items.ForEach((x) => x.ClearFilter());

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }


    public IEnumerable<HierarchyTreeViewItem> GetFullHierarchy() {
        foreach (var node in _originalItems) {
            yield return node;

            foreach (var descendant in node.Children.GetFullHierarchy()) {
                yield return descendant;
            }
        }
    }


    public IEnumerator<HierarchyTreeViewItem> GetEnumerator() {
        return _items.GetEnumerator();
    }


    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }


    protected void OnCollectionChanged(NotifyCollectionChangedEventArgs args) {
        CollectionChanged?.Invoke(this, args);
    }


    public event NotifyCollectionChangedEventHandler? CollectionChanged;

}
