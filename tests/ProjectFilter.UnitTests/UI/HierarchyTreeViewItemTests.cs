using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Text;
using NSubstitute;
using ProjectFilter.Helpers;
using ProjectFilter.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;


namespace ProjectFilter.UI;


public static class HierarchyTreeViewItemTests {

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
            Assert.True(new HierarchyTreeViewItem(Substitute.For<IHierarchyNode>(), Enumerable.Empty<HierarchyTreeViewItem>()).IsExpanded);
        }


        [Fact]
        public void RaisesPropertyChangedEventForIcon() {
            HierarchyTreeViewItem item;


            item = new HierarchyTreeViewItem(
                Substitute.For<IHierarchyNode>(),
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
            IHierarchyNode node;


            node = Substitute.For<IHierarchyNode>();
            node.CollapsedIcon.Returns(KnownMonikers.FolderClosed);
            node.ExpandedIcon.Returns(KnownMonikers.FolderOpened);

            item = new HierarchyTreeViewItem(node, Enumerable.Empty<HierarchyTreeViewItem>()) {
                IsExpanded = true
            };

            Assert.Equal(KnownMonikers.FolderOpened, item.Icon);

            item.IsExpanded = false;
            Assert.Equal(KnownMonikers.FolderClosed, item.Icon);
        }

    }


    public class PathProperty {

        [Fact]
        public void ReturnsNameForItemWithoutParent() {
            HierarchyTreeViewItem item;
            IHierarchyNode node;


            node = Substitute.For<IHierarchyNode>();
            node.Name.Returns("Root");

            item = new HierarchyTreeViewItem(node, Enumerable.Empty<HierarchyTreeViewItem>());

            Assert.Equal("Root", item.Path);
        }


        [Fact]
        public void JoinsNameToParentPathForItemWithParent() {
            HierarchyTreeViewItem childItem;
            IHierarchyNode parentNode;
            IHierarchyNode childNode;


            childNode = Substitute.For<IHierarchyNode>();
            childNode.Name.Returns("Child");

            parentNode = Substitute.For<IHierarchyNode>();
            parentNode.Name.Returns("Root");

            childItem = new HierarchyTreeViewItem(childNode, Enumerable.Empty<HierarchyTreeViewItem>());
            _ = new HierarchyTreeViewItem(parentNode, new[] { childItem });

            Assert.Equal("Root/Child", childItem.Path);
        }

    }


    public class FilterMethod {

        [Fact]
        public void ReturnsTrueWhenTheNameMatches() {
            HierarchyTreeViewItem item;


            item = Factory.CreateTreeViewItem(name: "Foo");

            Assert.True(item.Filter(new RegexTextFilter("F")));
        }


        [Fact]
        public void ReturnsFalseWhenTheNameDoesNoMatch() {
            HierarchyTreeViewItem item;


            item = Factory.CreateTreeViewItem(name: "Foo");

            Assert.False(item.Filter(new RegexTextFilter("X")));
        }


        [Fact]
        public void ReturnsTrueWhenTheNameDoesNoMatchButSomeChildrenDoMatch() {
            HierarchyTreeViewItem item;



            item = Factory.CreateTreeViewItem(name: "Foo", children: new[] {
                    Factory.CreateTreeViewItem(name:"Bar"),
                    Factory.CreateTreeViewItem(name:"Meep")
                });

            Assert.True(item.Filter(new RegexTextFilter("B")));
        }


        [Fact]
        public void SetsHighlightSpansWhenTheFilterMatches() {
            HierarchyTreeViewItem item;


            item = Factory.CreateTreeViewItem(name: "Foo");

            Assert.True(item.Filter(new RegexTextFilter("F")));
            Assert.Equal(new[] { Span.FromBounds(0, 1) }, item.HighlightSpans);
        }


        [Fact]
        public void DoesNotSetHighlightSpansWhenThereAreNoMatches() {
            HierarchyTreeViewItem item;


            item = Factory.CreateTreeViewItem(name: "Foo");

            Assert.False(item.Filter(new RegexTextFilter("X")));
            Assert.Null(item.HighlightSpans);
        }


        [Fact]
        public void SetsHighlightSpansWhenThereAreMatchesAndMultipleSearchTerms() {
            HierarchyTreeViewItem item;


            item = Factory.CreateTreeViewItem(name: "Foo Bar");

            Assert.True(
                item.Filter(
                    new PatternTextFilter(
                        ".",
                        Factory.CreatePatternMatcherFactory(new[] { Span.FromBounds(0, 1), Span.FromBounds(4, 5) })
                    )
                )
            );

            Assert.Equal(new[] { Span.FromBounds(0, 1), Span.FromBounds(4, 5) }, item.HighlightSpans);
        }


        [Theory]
        [InlineData("[0,1]", "")]
        [InlineData("[0,1] [3,4]", "")]
        [InlineData("[0,1] [4,5]", "[0,1]")]
        [InlineData("[4,5]", "[0,1]")]
        [InlineData("[4,5] [6,7]", "[0,1] [2,3]")]
        [InlineData("[4,7]", "[0,3]")]
        [InlineData("[0,7]", "[0,3]")]
        public void ConvertsMatchingSpansFromPathToName(string matchingSpans, string expectedSpans) {
            HierarchyTreeViewItem parent;
            HierarchyTreeViewItem child;


            child = Factory.CreateTreeViewItem(name: "Bar");
            parent = Factory.CreateTreeViewItem(name: "Foo", children: new[] { child });

            child.Filter(new PatternTextFilter(".", Factory.CreatePatternMatcherFactory(ParseSpans(matchingSpans))));

            Assert.NotNull(child.HighlightSpans);
            Assert.Equal(ParseSpans(expectedSpans), child.HighlightSpans);

            static IEnumerable<Span> ParseSpans(string spans) {
                return spans
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select((x) => Regex.Match(x, "\\[(?<start>\\d+),(?<end>\\d+)\\]").Groups)
                    .Select((x) => Span.FromBounds(
                        int.Parse(x["start"].Value, CultureInfo.InvariantCulture),
                        int.Parse(x["end"].Value, CultureInfo.InvariantCulture)
                    ))
                    .ToList();
            }
        }

    }


    public class ClearFilterMethod {

        [Fact]
        public void ClearsTheHighlightSpans() {
            HierarchyTreeViewItem item;


            item = Factory.CreateTreeViewItem(name: "Foo");

            item.Filter(new RegexTextFilter("F"));
            Assert.NotNull(item.HighlightSpans);

            item.ClearFilter();
            Assert.Null(item.HighlightSpans);
        }


        [Fact]
        public void ClearsFilterInChildrenCollection() {
            HierarchyTreeViewItem item;


            item = Factory.CreateTreeViewItem(name: "Foo", children: new[] { Factory.CreateTreeViewItem(name: "Bar") });

            item.Filter(new RegexTextFilter("X"));
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
