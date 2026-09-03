using ProjectFilter.Helpers;
using ProjectFilter.Services;
using System.Collections.Specialized;
using System.Linq;
using Xunit;


namespace ProjectFilter.UI;


public static class HierarchyTreeViewItemCollectionTests {

    public class CountProperty {

        [Fact]
        public void ReturnsCountOfFilteredItems() {
            HierarchyTreeViewItemCollection collection;


            collection = new HierarchyTreeViewItemCollection([
                    Factory.CreateTreeViewItem(name:"foo"),
                    Factory.CreateTreeViewItem(name:"bar")
                ]);

            Assert.Equal(2, collection.Count);

            collection.Filter(new RegexTextFilter("f"));

            Assert.Single(collection);

            collection.ClearFilter();

            Assert.Equal(2, collection.Count);
        }

    }


    public class CalculateCheckedStateMethod {

        [Fact]
        public void ReturnsFalseWhenCollectionIsEmpty() {
            HierarchyTreeViewItemCollection collection;


            collection = new HierarchyTreeViewItemCollection([]);

            Assert.False(collection.CalculateCheckedState());
        }


        [Fact]
        public void ReturnsFalseWhenNoItemsAreChecked() {
            HierarchyTreeViewItemCollection collection;


            collection = new HierarchyTreeViewItemCollection([
                    Factory.CreateTreeViewItem(isChecked: false),
                    Factory.CreateTreeViewItem(isChecked: false),
                    Factory.CreateTreeViewItem(isChecked: false)
                ]);

            Assert.False(collection.CalculateCheckedState());
        }


        [Fact]
        public void ReturnsTrueWhenAllItemsAreChecked() {
            HierarchyTreeViewItemCollection collection;


            collection = new HierarchyTreeViewItemCollection([
                    Factory.CreateTreeViewItem(isChecked: true),
                    Factory.CreateTreeViewItem(isChecked: true),
                    Factory.CreateTreeViewItem(isChecked: true),
                ]);

            Assert.True(collection.CalculateCheckedState());
        }


        [Fact]
        public void ReturnsNullWhenSomeItemsAreChecked() {
            HierarchyTreeViewItemCollection collection;


            collection = new HierarchyTreeViewItemCollection([
                    Factory.CreateTreeViewItem(isChecked: true),
                    Factory.CreateTreeViewItem(isChecked: false),
                    Factory.CreateTreeViewItem(isChecked: true),
                ]);

            Assert.Null(collection.CalculateCheckedState());
        }


        [Fact]
        public void ReturnsNullWhenAllItemsAreIndeterminate() {
            HierarchyTreeViewItemCollection collection;


            collection = new HierarchyTreeViewItemCollection([
                    Factory.CreateTreeViewItem(isChecked: null),
                    Factory.CreateTreeViewItem(isChecked: null),
                    Factory.CreateTreeViewItem(isChecked: null),
                ]);

            Assert.Null(collection.CalculateCheckedState());
        }

    }


    public class FilterMethod {

        [Fact]
        public void FiltersCollectionToOnlyContainItemsThatMatchFilterWhenItemsHaveNoChildren() {
            HierarchyTreeViewItemCollection collection;
            HierarchyTreeViewItem foo;
            HierarchyTreeViewItem bar;
            HierarchyTreeViewItem meep;


            foo = Factory.CreateTreeViewItem(name: "foo");
            bar = Factory.CreateTreeViewItem(name: "bar");
            meep = Factory.CreateTreeViewItem(name: "meep");

            collection = new HierarchyTreeViewItemCollection([foo, bar, meep]);
            collection.Filter(new RegexTextFilter("foo"));

            Assert.Equal([foo], collection);
        }


        [Fact]
        public void FiltersCollectionToOnlyContainItemsThatMatchFilterOrHaveChildrenThatMatchFilter() {
            HierarchyTreeViewItemCollection collection;
            HierarchyTreeViewItem foo;
            HierarchyTreeViewItem bar;
            HierarchyTreeViewItem meep;


            foo = Factory.CreateTreeViewItem(name: "foo", children: [Factory.CreateTreeViewItem(name: "x")]);
            bar = Factory.CreateTreeViewItem(name: "bar", children: [Factory.CreateTreeViewItem(name: "y")]);
            meep = Factory.CreateTreeViewItem(name: "meep", children: [Factory.CreateTreeViewItem(name: "fff")]);

            collection = new HierarchyTreeViewItemCollection([foo, bar, meep]);
            collection.Filter(new RegexTextFilter("f"));

            Assert.Equal([foo, meep], collection);
        }


        [Fact]
        public void UsesOriginalItemsWhenFilteringIfCollectionHasAlreadyBeenFiltered() {
            HierarchyTreeViewItemCollection collection;
            HierarchyTreeViewItem foo;
            HierarchyTreeViewItem bar;
            HierarchyTreeViewItem meep;


            foo = Factory.CreateTreeViewItem(name: "foo");
            bar = Factory.CreateTreeViewItem(name: "bar");
            meep = Factory.CreateTreeViewItem(name: "meep");

            collection = new HierarchyTreeViewItemCollection([foo, bar, meep]);
            collection.Filter(new RegexTextFilter("foo"));

            Assert.Equal([foo], collection);

            collection.Filter(new RegexTextFilter("bar"));

            Assert.Equal([bar], collection);
        }


        [Fact]
        public void DoesNotRaiseCollectionChangedEventWhenCollectionIsEmpty() {
            HierarchyTreeViewItemCollection collection;
            bool raised;


            raised = false;

            collection = new HierarchyTreeViewItemCollection([]);
            collection.CollectionChanged += (s, e) => raised = true;

            collection.Filter(new RegexTextFilter("foo"));

            Assert.False(raised);
        }


        [Fact]
        public void RaisesCollectionChangedEventWhenCollectionIsNotEmpty() {
            HierarchyTreeViewItemCollection collection;
            Assert.RaisedEvent<NotifyCollectionChangedEventArgs> e;


            collection = new HierarchyTreeViewItemCollection([
                    Factory.CreateTreeViewItem(name: "foo"),
                    Factory.CreateTreeViewItem(name: "bar")
                ]);

            e = Assert.Raises<NotifyCollectionChangedEventArgs>(
                (x) => collection.CollectionChanged += new NotifyCollectionChangedEventHandler(x),
                (x) => collection.CollectionChanged -= new NotifyCollectionChangedEventHandler(x),
                () => collection.Filter(new RegexTextFilter("foo"))
            );

            Assert.Equal(NotifyCollectionChangedAction.Reset, e.Arguments.Action);
        }

    }


    public class ClearFilterMethod {

        [Fact]
        public void RestoresOriginalItems() {
            HierarchyTreeViewItemCollection collection;
            HierarchyTreeViewItem foo;
            HierarchyTreeViewItem bar;
            HierarchyTreeViewItem meep;


            foo = Factory.CreateTreeViewItem(name: "foo");
            bar = Factory.CreateTreeViewItem(name: "bar");
            meep = Factory.CreateTreeViewItem(name: "meep");

            collection = new HierarchyTreeViewItemCollection([foo, bar, meep]);
            collection.Filter(new RegexTextFilter("foo"));

            Assert.Equal([foo], collection);

            collection.ClearFilter();

            Assert.Equal([foo, bar, meep], collection);
        }


        [Fact]
        public void ClearsFilterOnEachItem() {
            HierarchyTreeViewItemCollection collection;
            HierarchyTreeViewItem item;
            HierarchyTreeViewItem childA;
            HierarchyTreeViewItem childB;
            HierarchyTreeViewItem childC;


            childA = Factory.CreateTreeViewItem(name: "A");
            childB = Factory.CreateTreeViewItem(name: "B");
            childC = Factory.CreateTreeViewItem(name: "C");
            item = Factory.CreateTreeViewItem(name: "Root", children: [childA, childB, childC]);

            collection = new HierarchyTreeViewItemCollection([item]);
            collection.Filter(new RegexTextFilter("B"));

            Assert.Equal([item], collection);
            Assert.Equal([childB], item.Children);

            collection.ClearFilter();

            Assert.Equal([item], collection);
            Assert.Equal([childA, childB, childC], item.Children);
        }


        [Fact]
        public void DoesNotRaiseCollectionChangedEventWhenCollectionIsEmpty() {
            HierarchyTreeViewItemCollection collection;
            bool raised;


            raised = false;

            collection = new HierarchyTreeViewItemCollection([]);
            collection.Filter(new RegexTextFilter("A"));

            collection.CollectionChanged += (s, e) => raised = true;
            collection.ClearFilter();

            Assert.False(raised);
        }


        [Fact]
        public void RaisesCollectionChangedEventWhenCollectionIsNotEmpty() {
            HierarchyTreeViewItemCollection collection;
            Assert.RaisedEvent<NotifyCollectionChangedEventArgs> e;


            collection = new HierarchyTreeViewItemCollection([
                    Factory.CreateTreeViewItem(name: "Foo"),
                    Factory.CreateTreeViewItem(name: "Bar"),
                ]);

            collection.Filter(new RegexTextFilter("F"));

            e = Assert.Raises<NotifyCollectionChangedEventArgs>(
                (x) => collection.CollectionChanged += new NotifyCollectionChangedEventHandler(x),
                (x) => collection.CollectionChanged -= new NotifyCollectionChangedEventHandler(x),
                () => collection.ClearFilter()
            );

            Assert.Equal(NotifyCollectionChangedAction.Reset, e.Arguments.Action);
        }

    }


    public class GetFullHierarchyMethod {

        [Fact]
        public void ReturnsAllItemsAndAllDescendants() {
            HierarchyTreeViewItemCollection collection;
            HierarchyTreeViewItem root1;
            HierarchyTreeViewItem root2;
            HierarchyTreeViewItem child1;
            HierarchyTreeViewItem child2;
            HierarchyTreeViewItem grandchild1;
            HierarchyTreeViewItem grandchild2;


            grandchild1 = Factory.CreateTreeViewItem();
            grandchild2 = Factory.CreateTreeViewItem();
            child1 = Factory.CreateTreeViewItem(children: [grandchild1]);
            child2 = Factory.CreateTreeViewItem(children: [grandchild2]);
            root1 = Factory.CreateTreeViewItem(children: [child1]);
            root2 = Factory.CreateTreeViewItem(children: [child2]);

            collection = new HierarchyTreeViewItemCollection([root1, root2]);

            Assert.Equal(
                [
                        root1,
                        child1,
                        grandchild1,
                        root2,
                        child2,
                        grandchild2
                ],
                collection.GetFullHierarchy()
            );
        }


        [Fact]
        public void IncludesFilteredItems() {
            HierarchyTreeViewItemCollection collection;
            HierarchyTreeViewItem root1;
            HierarchyTreeViewItem root2;
            HierarchyTreeViewItem child1;
            HierarchyTreeViewItem child2;
            HierarchyTreeViewItem grandchild1;
            HierarchyTreeViewItem grandchild2;


            grandchild1 = Factory.CreateTreeViewItem(name: "x");
            grandchild2 = Factory.CreateTreeViewItem(name: "a");
            child1 = Factory.CreateTreeViewItem(name: "x", children: [grandchild1]);
            child2 = Factory.CreateTreeViewItem(name: "x", children: [grandchild2]);
            root1 = Factory.CreateTreeViewItem(name: "x", children: [child1]);
            root2 = Factory.CreateTreeViewItem(name: "a", children: [child2]);

            collection = new HierarchyTreeViewItemCollection([root1, root2]);

            collection.Filter(new RegexTextFilter("x"));

            Assert.Equal(
                [
                        root1,
                        child1,
                        grandchild1,
                        root2,
                        child2,
                        grandchild2
                ],
                collection.GetFullHierarchy()
            );
        }

    }


    public class GetEnumeratorMethod {

        [Fact]
        public void DoesNotIncludeFilteredItems() {
            HierarchyTreeViewItemCollection collection;
            HierarchyTreeViewItem root1;
            HierarchyTreeViewItem root2;
            HierarchyTreeViewItem root3;


            root1 = Factory.CreateTreeViewItem(name: "x");
            root2 = Factory.CreateTreeViewItem(name: "a");
            root3 = Factory.CreateTreeViewItem(name: "x");

            collection = new HierarchyTreeViewItemCollection([root1, root2, root3]);

            collection.Filter(new RegexTextFilter("x"));

            Assert.Equal(
                [root1, root3],
                collection
            );
        }

    }

}
