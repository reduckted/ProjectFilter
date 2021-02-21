using ProjectFilter.Helpers;
using System.Collections.Specialized;
using System.Linq;
using Xunit;


namespace ProjectFilter.UI {

    public class HierarchyTreeViewItemCollectionTests {

        public class CountProperty {

            [Fact]
            public void ReturnsCountOfFilteredItems() {
                HierarchyTreeViewItemCollection collection;


                collection = new HierarchyTreeViewItemCollection(new[] {
                    Factory.CreateTreeViewItem(name:"foo"),
                    Factory.CreateTreeViewItem(name:"bar")
                });

                Assert.Equal(2, collection.Count);

                collection.Filter(Factory.CreateSearchEvaluator("f"));

                Assert.Single(collection);

                collection.ClearFilter();

                Assert.Equal(2, collection.Count);
            }

        }


        public class GetCheckedStateMethod {

            [Fact]
            public void ReturnsFalseWhenCollectionIsEmpty() {
                HierarchyTreeViewItemCollection collection;


                collection = new HierarchyTreeViewItemCollection(Enumerable.Empty<HierarchyTreeViewItem>());

                Assert.False(collection.GetCheckedState());
            }


            [Fact]
            public void ReturnsFalseWhenNoItemsAreChecked() {
                HierarchyTreeViewItemCollection collection;


                collection = new HierarchyTreeViewItemCollection(new[] {
                    Factory.CreateTreeViewItem(isChecked: false),
                    Factory.CreateTreeViewItem(isChecked: false),
                    Factory.CreateTreeViewItem(isChecked: false)
                });

                Assert.False(collection.GetCheckedState());
            }


            [Fact]
            public void ReturnsTrueWhenAllItemsAreChecked() {
                HierarchyTreeViewItemCollection collection;


                collection = new HierarchyTreeViewItemCollection(new[] {
                    Factory.CreateTreeViewItem(isChecked: true),
                    Factory.CreateTreeViewItem(isChecked: true),
                    Factory.CreateTreeViewItem(isChecked: true),
                });

                Assert.True(collection.GetCheckedState());
            }


            [Fact]
            public void ReturnsNullWhenSomeItemsAreChecked() {
                HierarchyTreeViewItemCollection collection;


                collection = new HierarchyTreeViewItemCollection(new[] {
                    Factory.CreateTreeViewItem(isChecked: true),
                    Factory.CreateTreeViewItem(isChecked: false),
                    Factory.CreateTreeViewItem(isChecked: true),
                });

                Assert.Null(collection.GetCheckedState());
            }


            [Fact]
            public void ReturnsNullWhenAllItemsAreIndeterminate() {
                HierarchyTreeViewItemCollection collection;


                collection = new HierarchyTreeViewItemCollection(new[] {
                    Factory.CreateTreeViewItem(isChecked: null),
                    Factory.CreateTreeViewItem(isChecked: null),
                    Factory.CreateTreeViewItem(isChecked: null),
                });

                Assert.Null(collection.GetCheckedState());
            }

        }


        public class FilterMethod {

            [Fact]
            public void FiltersCollectionToOnlyContainItemsThatMatchFilterWhenItemshaveNoChildren() {
                HierarchyTreeViewItemCollection collection;
                HierarchyTreeViewItem foo;
                HierarchyTreeViewItem bar;
                HierarchyTreeViewItem meep;


                foo = Factory.CreateTreeViewItem(name: "foo");
                bar = Factory.CreateTreeViewItem(name: "bar");
                meep = Factory.CreateTreeViewItem(name: "meep");

                collection = new HierarchyTreeViewItemCollection(new[] { foo, bar, meep });
                collection.Filter(Factory.CreateSearchEvaluator("foo"));

                Assert.Equal(new[] { foo }, collection);
            }


            [Fact]
            public void FiltersCollectionToOnlyContainItemsThatMatchFilterOrHaveChildrenThatMatchFilter() {
                HierarchyTreeViewItemCollection collection;
                HierarchyTreeViewItem foo;
                HierarchyTreeViewItem bar;
                HierarchyTreeViewItem meep;


                foo = Factory.CreateTreeViewItem(name: "foo", children: new[] { Factory.CreateTreeViewItem(name: "x") });
                bar = Factory.CreateTreeViewItem(name: "bar", children: new[] { Factory.CreateTreeViewItem(name: "y") });
                meep = Factory.CreateTreeViewItem(name: "meep", children: new[] { Factory.CreateTreeViewItem(name: "fff") });

                collection = new HierarchyTreeViewItemCollection(new[] { foo, bar, meep });
                collection.Filter(Factory.CreateSearchEvaluator("f"));

                Assert.Equal(new[] { foo, meep }, collection);
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

                collection = new HierarchyTreeViewItemCollection(new[] { foo, bar, meep });
                collection.Filter(Factory.CreateSearchEvaluator("foo"));

                Assert.Equal(new[] { foo }, collection);

                collection.Filter(Factory.CreateSearchEvaluator("bar"));

                Assert.Equal(new[] { bar }, collection);
            }


            [Fact]
            public void DoesNotRaiseCollectionChangedEventWhenCollectionIsEmpty() {
                HierarchyTreeViewItemCollection collection;
                bool raised;


                raised = false;

                collection = new HierarchyTreeViewItemCollection(Enumerable.Empty<HierarchyTreeViewItem>());
                collection.CollectionChanged += (s, e) => raised = true;

                collection.Filter(Factory.CreateSearchEvaluator("foo"));

                Assert.False(raised);
            }


            [Fact]
            public void RaisesCollectionChangedEventWhenCollectionIsNotEmpty() {
                HierarchyTreeViewItemCollection collection;
                Assert.RaisedEvent<NotifyCollectionChangedEventArgs> e;


                collection = new HierarchyTreeViewItemCollection(new[]{
                    Factory.CreateTreeViewItem(name: "foo"),
                    Factory.CreateTreeViewItem(name: "bar")
                });

                e = Assert.Raises<NotifyCollectionChangedEventArgs>(
                    (x) => collection.CollectionChanged += new NotifyCollectionChangedEventHandler(x),
                    (x) => collection.CollectionChanged -= new NotifyCollectionChangedEventHandler(x),
                    () => collection.Filter(Factory.CreateSearchEvaluator("foo"))
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

                collection = new HierarchyTreeViewItemCollection(new[] { foo, bar, meep });
                collection.Filter(Factory.CreateSearchEvaluator("foo"));

                Assert.Equal(new[] { foo }, collection);

                collection.ClearFilter();

                Assert.Equal(new[] { foo, bar, meep }, collection);
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
                item = Factory.CreateTreeViewItem(name: "Bar", children: new[] { childA, childB, childC });

                collection = new HierarchyTreeViewItemCollection(new[] { item });
                collection.Filter(Factory.CreateSearchEvaluator("B"));

                Assert.Equal(new[] { item }, collection);
                Assert.Equal(new[] { childB }, item.Children);

                collection.ClearFilter();

                Assert.Equal(new[] { item }, collection);
                Assert.Equal(new[] { childA, childB, childC }, item.Children);
            }


            [Fact]
            public void DoesNotRaiseCollectionChangedEventWhenCollectionIsEmpty() {
                HierarchyTreeViewItemCollection collection;
                bool raised;


                raised = false;

                collection = new HierarchyTreeViewItemCollection(Enumerable.Empty<HierarchyTreeViewItem>());
                collection.Filter(Factory.CreateSearchEvaluator("A"));

                collection.CollectionChanged += (s, e) => raised = true;
                collection.ClearFilter();

                Assert.False(raised);
            }


            [Fact]
            public void RaisesCollectionChangedEventWhenCollectionIsNotEmpty() {
                HierarchyTreeViewItemCollection collection;
                Assert.RaisedEvent<NotifyCollectionChangedEventArgs> e;


                collection = new HierarchyTreeViewItemCollection(new[] {
                    Factory.CreateTreeViewItem(name: "Foo"),
                    Factory.CreateTreeViewItem(name: "Bar"),
                });

                collection.Filter(Factory.CreateSearchEvaluator("F"));

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
                child1 = Factory.CreateTreeViewItem(children: new[] { grandchild1 });
                child2 = Factory.CreateTreeViewItem(children: new[] { grandchild2 });
                root1 = Factory.CreateTreeViewItem(children: new[] { child1 });
                root2 = Factory.CreateTreeViewItem(children: new[] { child2 });

                collection = new HierarchyTreeViewItemCollection(new[] { root1, root2 });

                Assert.Equal(
                    new[] {
                        root1,
                        child1,
                        grandchild1,
                        root2,
                        child2,
                        grandchild2
                    },
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
                child1 = Factory.CreateTreeViewItem(name: "x", children: new[] { grandchild1 });
                child2 = Factory.CreateTreeViewItem(name: "x", children: new[] { grandchild2 });
                root1 = Factory.CreateTreeViewItem(name: "x", children: new[] { child1 });
                root2 = Factory.CreateTreeViewItem(name: "a", children: new[] { child2 });

                collection = new HierarchyTreeViewItemCollection(new[] { root1, root2 });

                collection.Filter(Factory.CreateSearchEvaluator("x"));

                Assert.Equal(
                    new[] {
                        root1,
                        child1,
                        grandchild1,
                        root2,
                        child2,
                        grandchild2
                    },
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

                collection = new HierarchyTreeViewItemCollection(new[] { root1, root2, root3 });

                collection.Filter(Factory.CreateSearchEvaluator("x"));

                Assert.Equal(
                    new[] { root1, root3 },
                    collection
                );
            }

        }

    }

}
