using Microsoft.VisualStudio.Imaging;
using Moq;
using ProjectFilter.Helpers;
using ProjectFilter.Services;
using System.Linq;
using Xunit;


namespace ProjectFilter.UI {

    public class HierarchyTreeViewItemTests {

        public class IsCheckedProperty {

            [Fact]
            public void ChecksAndUnchecksAllDescendantsWhenChanged() {
                HierarchyTreeViewItem root;
                HierarchyTreeViewItem childA;
                HierarchyTreeViewItem childB;
                HierarchyTreeViewItem grandchildA;
                HierarchyTreeViewItem grandchildB;
                HierarchyTreeViewItem grandchildC;


                grandchildC = Factory.CreateTreeViewItem();
                grandchildB = Factory.CreateTreeViewItem();
                grandchildA = Factory.CreateTreeViewItem();
                childB = Factory.CreateTreeViewItem(children: new[] { grandchildC });
                childA = Factory.CreateTreeViewItem(children: new[] { grandchildA, grandchildB });
                root = Factory.CreateTreeViewItem(children: new[] { childA, childB });

                root.IsChecked = true;

                Assert.True(root.IsChecked);
                Assert.Equal(new bool?[] { true, true }, new bool?[] { childA.IsChecked, childB.IsChecked });
                Assert.Equal(new bool?[] { true, true, true }, new bool?[] { grandchildA.IsChecked, grandchildB.IsChecked, grandchildC.IsChecked });

                root.IsChecked = false;

                Assert.False(root.IsChecked);
                Assert.Equal(new bool?[] { false, false }, new bool?[] { childA.IsChecked, childB.IsChecked });
                Assert.Equal(new bool?[] { false, false, false }, new bool?[] { grandchildA.IsChecked, grandchildB.IsChecked, grandchildC.IsChecked });
            }


            [Fact]
            public void SetsParentToIndeterminateWhenCheckedAndSiblingsAreUnchecked() {
                HierarchyTreeViewItem root;
                HierarchyTreeViewItem childA;
                HierarchyTreeViewItem childB;


                childB = Factory.CreateTreeViewItem();
                childA = Factory.CreateTreeViewItem();
                root = Factory.CreateTreeViewItem(children: new[] { childA, childB });

                childA.IsChecked = true;
                Assert.Null(root.IsChecked);
            }


            [Fact]
            public void SetsParentToIndeterminateWhenUncheckedAndSiblingsAreChecked() {
                HierarchyTreeViewItem root;
                HierarchyTreeViewItem childA;
                HierarchyTreeViewItem childB;


                childB = Factory.CreateTreeViewItem();
                childA = Factory.CreateTreeViewItem();
                root = Factory.CreateTreeViewItem(children: new[] { childA, childB });

                childA.IsChecked = true;
                childB.IsChecked = true;

                childA.IsChecked = false;
                Assert.Null(root.IsChecked);
            }


            [Fact]
            public void SetsParentToCheckedWhenCheckedAndSiblingsAreChecked() {
                HierarchyTreeViewItem root;
                HierarchyTreeViewItem childA;
                HierarchyTreeViewItem childB;


                childB = Factory.CreateTreeViewItem();
                childA = Factory.CreateTreeViewItem();
                root = Factory.CreateTreeViewItem(children: new[] { childA, childB });

                childA.IsChecked = true;
                childB.IsChecked = true;
                Assert.True(root.IsChecked);
            }


            [Fact]
            public void SetsParentToUncheckedWhenUncheckedAndSiblingsAreUnchecked() {
                HierarchyTreeViewItem root;
                HierarchyTreeViewItem childA;
                HierarchyTreeViewItem childB;


                childB = Factory.CreateTreeViewItem();
                childA = Factory.CreateTreeViewItem();
                root = Factory.CreateTreeViewItem(children: new[] { childA, childB });

                childA.IsChecked = true;
                childB.IsChecked = true;

                childA.IsChecked = false;
                childB.IsChecked = false;
                Assert.False(root.IsChecked);
            }


            [Fact]
            public void UpdatesAllAncestorsWhenChanged() {
                HierarchyTreeViewItem root;
                HierarchyTreeViewItem childA;
                HierarchyTreeViewItem childB;
                HierarchyTreeViewItem grandchildA;
                HierarchyTreeViewItem grandchildB;
                HierarchyTreeViewItem grandchildC;


                grandchildC = Factory.CreateTreeViewItem();
                grandchildB = Factory.CreateTreeViewItem();
                grandchildA = Factory.CreateTreeViewItem();
                childB = Factory.CreateTreeViewItem(children: new[] { grandchildC });
                childA = Factory.CreateTreeViewItem(children: new[] { grandchildA, grandchildB });
                root = Factory.CreateTreeViewItem(children: new[] { childA, childB });

                grandchildC.IsChecked = true;
                Assert.True(childB.IsChecked);
                Assert.False(childA.IsChecked);
                Assert.Null(root.IsChecked);

                grandchildB.IsChecked = true;
                Assert.True(childB.IsChecked);
                Assert.Null(childA.IsChecked);
                Assert.Null(root.IsChecked);

                grandchildA.IsChecked = true;
                Assert.True(childB.IsChecked);
                Assert.True(childA.IsChecked);
                Assert.True(root.IsChecked);
            }

        }


        public class IsExpandedProperty {

            [Fact]
            public void IsInitiallyTrue() {
                Assert.True(new HierarchyTreeViewItem(Mock.Of<IHierarchyNode>(), Enumerable.Empty<HierarchyTreeViewItem>()).IsExpanded);
            }


            [Fact]
            public void RaisesPropertyChangedEventForIcon() {
                HierarchyTreeViewItem item;


                item = new HierarchyTreeViewItem(
                    Mock.Of<IHierarchyNode>(),
                    Enumerable.Empty<HierarchyTreeViewItem>()
                );

                Assert.PropertyChanged(
                    item,
                    nameof(HierarchyTreeViewItem.Icon),
                    () => item.IsExpanded = false
                );

                Assert.PropertyChanged(
                    item,
                    nameof(HierarchyTreeViewItem.Icon),
                    () => item.IsExpanded = true
                );
            }

        }


        public class IconProperty {

            [Fact]
            public void ReturnsIconBasedOnExpandedState() {
                HierarchyTreeViewItem item;
                Mock<IHierarchyNode> node;


                node = new Mock<IHierarchyNode>();
                node.SetupGet((x) => x.CollapsedIcon).Returns(KnownMonikers.FolderClosed);
                node.SetupGet((x) => x.ExpandedIcon).Returns(KnownMonikers.FolderOpened);

                item = new HierarchyTreeViewItem(node.Object, Enumerable.Empty<HierarchyTreeViewItem>()) {
                    IsExpanded = true
                };

                Assert.Equal(KnownMonikers.FolderOpened, item.Icon);

                item.IsExpanded = false;
                Assert.Equal(KnownMonikers.FolderClosed, item.Icon);
            }

        }


        public class FilterMethod {

            [Fact]
            public void ReturnsTrueWhenTheNameMatches() {
                HierarchyTreeViewItem item;


                item = Factory.CreateTreeViewItem(name: "Foo");

                Assert.True(item.Filter(Factory.CreateSearchEvaluator("F")));
            }


            [Fact]
            public void ReturnsFalseWhenTheNameDoesNoMatch() {
                HierarchyTreeViewItem item;


                item = Factory.CreateTreeViewItem(name: "Foo");

                Assert.False(item.Filter(Factory.CreateSearchEvaluator("X")));
            }


            [Fact]
            public void ReturnsTrueWhenTheNameDoesNoMatchButSomeChildrenDoMatch() {
                HierarchyTreeViewItem item;



                item = Factory.CreateTreeViewItem(name: "Foo", children: new[] {
                    Factory.CreateTreeViewItem(name:"Bar"),
                    Factory.CreateTreeViewItem(name:"Meep")
                });

                Assert.True(item.Filter(Factory.CreateSearchEvaluator("B")));
            }


            [Fact]
            public void SetsHighlightTextWhenThereIsOneSearchTerm() {
                HierarchyTreeViewItem item;


                item = Factory.CreateTreeViewItem(name: "Foo");

                Assert.True(item.Filter(Factory.CreateSearchEvaluator("F")));
                Assert.Equal("F", item.HighlightText);
            }


            [Fact]
            public void DoesNotSetHighlightTextWhenThereAreNoMatches() {
                HierarchyTreeViewItem item;


                item = Factory.CreateTreeViewItem(name: "Foo");

                Assert.False(item.Filter(Factory.CreateSearchEvaluator("X")));
                Assert.Equal("", item.HighlightText);
            }


            [Fact]
            public void DoesNotSetHighlightTextWhenThereAreMatchesButMultipleSearchTerms() {
                HierarchyTreeViewItem item;


                item = Factory.CreateTreeViewItem(name: "Foo Bar");

                Assert.True(item.Filter(Factory.CreateSearchEvaluator("Foo Bar")));
                Assert.Equal("", item.HighlightText);
            }

        }


        public class ClearFilterMethod {

            [Fact]
            public void ClearsTheHighlightText() {
                HierarchyTreeViewItem item;


                item = Factory.CreateTreeViewItem(name: "Foo");

                item.Filter(Factory.CreateSearchEvaluator("F"));
                Assert.NotEqual("", item.HighlightText);

                item.ClearFilter();
                Assert.Equal("", item.HighlightText);
            }


            [Fact]
            public void ClearsFilterInChildrenCollection() {
                HierarchyTreeViewItem item;


                item = Factory.CreateTreeViewItem(name: "Foo", children: new[] { Factory.CreateTreeViewItem(name: "Bar") });

                item.Filter(Factory.CreateSearchEvaluator("F"));
                Assert.Empty(item.Children);

                item.ClearFilter();
                Assert.NotEmpty(item.Children);
            }

        }


        public class DescendantsAndSelfMethod {

            [Fact]
            public void ReturnsSelfAndAllDescendants() {
                HierarchyTreeViewItem root;
                HierarchyTreeViewItem childA;
                HierarchyTreeViewItem childB;
                HierarchyTreeViewItem grandchildA;
                HierarchyTreeViewItem grandchildB;
                HierarchyTreeViewItem grandchildC;


                grandchildC = Factory.CreateTreeViewItem();
                grandchildB = Factory.CreateTreeViewItem();
                grandchildA = Factory.CreateTreeViewItem();
                childB = Factory.CreateTreeViewItem(children: new[] { grandchildC });
                childA = Factory.CreateTreeViewItem(children: new[] { grandchildA, grandchildB });
                root = Factory.CreateTreeViewItem(children: new[] { childA, childB });

                Assert.Equal(
                    new[] { root, childA, grandchildA, grandchildB, childB, grandchildC },
                    root.DescendantsAndSelf()
                );

                Assert.Equal(
                    new[] { childA, grandchildA, grandchildB },
                    childA.DescendantsAndSelf()
                );

                Assert.Equal(
                    new[] { childB, grandchildC },
                    childB.DescendantsAndSelf()
                );

                Assert.Equal(
                    new[] { grandchildA },
                    grandchildA.DescendantsAndSelf()
                );

                Assert.Equal(
                    new[] { grandchildB },
                    grandchildB.DescendantsAndSelf()
                );
            }

        }

    }

}
