using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Moq;
using System;
using Xunit;


namespace ProjectFilter.Services {

    public static class HierarchyNodeTests {

        public class IdentifierProperty {

            [Fact]
            public void StoresTheValueFromTheConstructor() {
                Guid identifier;


                identifier = new Guid("{289a07bd-ea36-412a-9c5b-ec3fee63c319}");

                Assert.Equal(
                    identifier,
                    CreateNode(identifier: identifier).Identifier
                );
            }

        }


        public class NameProperty {

            [Fact]
            public void StoresTheValueFromTheConstructor() {
                HierarchyNode node;


                node = new HierarchyNode(
                    Guid.NewGuid(),
                    "foo",
                    KnownMonikers.Abbreviation,
                    KnownMonikers.Abbreviation
                );

                Assert.Equal("foo", node.Name);
            }

        }


        public class ExpandedIconProperty {

            [Fact]
            public void StoresTheValueFromTheConstructor() {
                Assert.Equal(
                    KnownMonikers.Reference,
                    CreateNode(
                        collapsedIcon: KnownMonikers.Accelerator,
                        expandedIcon: KnownMonikers.Reference
                    ).ExpandedIcon
                );
            }

        }


        public class CollapsedIconProperty {

            [Fact]
            public void StoresTheValueFromTheConstructor() {
                Assert.Equal(
                    KnownMonikers.Accelerator,
                    CreateNode(
                        collapsedIcon: KnownMonikers.Accelerator,
                        expandedIcon: KnownMonikers.Reference
                    ).CollapsedIcon
                );
            }

        }


        public class ChildrenProperty {

            [Fact]
            public void IsNotNull() {
                Assert.NotNull(CreateNode().Children);
            }

        }


        private static HierarchyNode CreateNode(
            Guid? identifier = null,
            ImageMoniker? collapsedIcon = null,
            ImageMoniker? expandedIcon = null
        ) {
            return new HierarchyNode(
              identifier ?? Guid.NewGuid(),
              "foo",
              collapsedIcon ?? KnownMonikers.Abbreviation,
              expandedIcon ?? KnownMonikers.Abbreviation
          );
        }

    }

}
