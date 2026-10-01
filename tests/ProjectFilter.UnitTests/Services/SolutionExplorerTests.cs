using Microsoft.VisualStudio.Imaging.Interop;
using ProjectFilter.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services;


public static class SolutionExplorerTests {

    public class GetSolutionFoldersToHideMethod {

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task SolutionIsEmpty(
            bool hideSolutionFoldersWithoutLoadedProjects,
            bool hideSolutionFoldersWithOnlyFiles
        ) {
            Node solution;


            solution = await CreateSolutionAsync(
                """
                <solution/>
                """
            );

            Assert.Empty(
                GetSolutionFoldersToHide(
                    solution,
                    hideSolutionFoldersWithoutLoadedProjects,
                    hideSolutionFoldersWithOnlyFiles
                )
            );
        }


        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        public async Task FolderHasOnlyUnloadedProjects(
            bool hideSolutionFoldersWithoutLoadedProjects,
            bool hideSolutionFoldersWithOnlyFiles,
            bool expectedToHide
        ) {
            Node solution;
            IEnumerable<string> result;


            solution = await CreateSolutionAsync(
                """
                <solution>
                    <folder name="a">
                        <unloaded name="b"/>
                        <unloaded name="c"/>
                    </folder>
                </solution>
                """
            );

            result = GetSolutionFoldersToHide(
                solution,
                hideSolutionFoldersWithoutLoadedProjects,
                hideSolutionFoldersWithOnlyFiles
            );

            if (expectedToHide) {
                Assert.Equal(["a"], result);
            } else {
                Assert.Empty(result);
            }
        }


        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task FolderHasLoadedProjects(
            bool hideSolutionFoldersWithoutLoadedProjects,
            bool hideSolutionFoldersWithOnlyFiles
        ) {
            Node solution;


            solution = await CreateSolutionAsync(
                """
                <solution>
                    <folder name="a">
                        <project name="b"/>
                        <project name="c"/>
                    </folder>
                </solution>
                """
            );

            Assert.Empty(
                GetSolutionFoldersToHide(
                    solution,
                    hideSolutionFoldersWithoutLoadedProjects,
                    hideSolutionFoldersWithOnlyFiles
                )
            );
        }


        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task FolderHasLoadedAndUnloadedProjects(
            bool hideSolutionFoldersWithoutLoadedProjects,
            bool hideSolutionFoldersWithOnlyFiles
        ) {
            Node solution;


            solution = await CreateSolutionAsync(
                """
                <solution>
                    <folder name="a">
                        <unloaded name="b"/>
                        <project name="c"/>
                    </folder>
                </solution>
                """
            );

            Assert.Empty(
                GetSolutionFoldersToHide(
                    solution,
                    hideSolutionFoldersWithoutLoadedProjects,
                    hideSolutionFoldersWithOnlyFiles
                )
            );
        }


        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        public async Task ParentAndChildFoldersCanBeHidden(
            bool hideSolutionFoldersWithoutLoadedProjects,
            bool hideSolutionFoldersWithOnlyFiles,
            bool expectedToHide
        ) {
            Node solution;
            IEnumerable<string> result;


            solution = await CreateSolutionAsync(
                """
                <solution>
                    <folder name="a">
                        <folder name="b">
                            <unloaded name="c"/>
                        </folder>
                        <folder name="d">
                            <unloaded name="e"/>
                        </folder>
                    </folder>
                </solution>
                """
            );

            result = GetSolutionFoldersToHide(
                solution,
                hideSolutionFoldersWithoutLoadedProjects,
                hideSolutionFoldersWithOnlyFiles
            );

            if (expectedToHide) {
                Assert.Equal(["a", "b", "d"], result);
            } else {
                Assert.Empty(result);
            }
        }


        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        public async Task OnlyChildFoldersCanBeHidden(
            bool hideSolutionFoldersWithoutLoadedProjects,
            bool hideSolutionFoldersWithOnlyFiles,
            bool expectedToHide
        ) {
            Node solution;
            IEnumerable<string> result;


            solution = await CreateSolutionAsync(
                """
                <solution>
                    <folder name="a">
                        <folder name="b">
                            <unloaded name="c"/>
                        </folder>
                        <folder name="d">
                            <unloaded name="e"/>
                        </folder>
                        <project name="f"/>
                    </folder>
                </solution>
                """
            );

            result = GetSolutionFoldersToHide(
                solution,
                hideSolutionFoldersWithoutLoadedProjects,
                hideSolutionFoldersWithOnlyFiles
            );

            if (expectedToHide) {
                Assert.Equal(["b", "d"], result);
            } else {
                Assert.Empty(result);
            }
        }


        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, true)]
        [InlineData(false, false, false)]
        public async Task FolderContainsNoProjects(
            bool hideSolutionFoldersWithoutLoadedProjects,
            bool hideSolutionFoldersWithOnlyFiles,
            bool expectedToHide
        ) {
            Node solution;
            IEnumerable<string> result;


            solution = await CreateSolutionAsync(
                """
                <solution>
                    <folder name="a"/>
                </solution>
                """
            );

            result = GetSolutionFoldersToHide(
                solution,
                hideSolutionFoldersWithoutLoadedProjects,
                hideSolutionFoldersWithOnlyFiles
            );

            if (expectedToHide) {
                Assert.Equal(["a"], result);
            } else {
                Assert.Empty(result);
            }
        }


        [Theory]
        [InlineData(true, true, new[] { "e", "k", "n", "o", "p", "q", "r", "u", "v" })]
        [InlineData(true, false, new[] { "e", "k", "p", "q", "r" })]
        [InlineData(false, true, new[] { "v" })]
        [InlineData(false, false, new string[] { })]
        public async Task CanFindMultipleFolders(
            bool hideSolutionFoldersWithoutLoadedProjects,
            bool hideSolutionFoldersWithOnlyFiles,
            string[] expected
        ) {
            Node solution;


            solution = await CreateSolutionAsync(
                """
                <solution>
                    <folder name="a">
                        <folder name="b">
                            <project name="c"/>
                            <project name="d"/>
                            <folder name="e">
                                <unloaded name="f"/>
                                <unloaded name="g"/>
                            </folder>
                            <folder name="h">
                                <project name="i"/>
                                <unloaded name="j"/>
                            </folder>
                            <folder name="k">
                                <unloaded name="l"/>
                                <unloaded name="m"/>
                            </folder>
                        </folder>
                        <folder name="n">
                            <folder name="o">
                                <folder name="p">
                                    <folder name="q">
                                        <folder name="r">
                                            <unloaded name="s"/>
                                        </folder>
                                    </folder>
                                    <unloaded name="t"/>
                                </folder>
                                <folder name="u">
                                    <folder name="v"/>
                                    <unloaded name="w"/>
                                </folder>
                            </folder>
                        </folder>
                    </folder>
                </solution>
                """
            );

            Assert.Equal(
                expected,
                GetSolutionFoldersToHide(
                    solution,
                    hideSolutionFoldersWithoutLoadedProjects,
                    hideSolutionFoldersWithOnlyFiles
                )
            );
        }


        private static async Task<Node> CreateSolutionAsync(string data) {
            return Factory.ParseHierarchies(data, CreateNode);
        }


        private static Node CreateNode(XElement element, HierarchyData data) {
            return new Node(data);
        }


        private static IEnumerable<string> GetSolutionFoldersToHide(
            IHierarchyNode root,
            bool hideSolutionFoldersWithoutLoadedProjects,
            bool hideSolutionFoldersWithOnlyFiles
        ) {
            return SolutionExplorer
                .GetSolutionFoldersToHide(
                    root.Children,
                    hideSolutionFoldersWithoutLoadedProjects,
                    hideSolutionFoldersWithOnlyFiles
                )
                .Cast<Node>()
                .Select(node => node.Data.Name)
                .OrderBy((x) => x);
        }


        [DebuggerDisplay("{Data.Name,nq}")]
        private class Node : TreeItem, IHierarchyNode {

            private List<IHierarchyNode>? _children;


            public Node(HierarchyData data) : base(data) { }


            public bool IsFolder => Data.Type == HierarchyType.Folder;


            public bool IsLoaded => Data.Type == HierarchyType.Project;


            public string Name => Data.Name;


            IReadOnlyList<IHierarchyNode> IHierarchyNode.Children {
                get {
                    return _children ??= [.. Children.Cast<IHierarchyNode>()];
                }
            }


            public Guid Identifier => throw new NotImplementedException();


            public ImageMoniker CollapsedIcon => throw new NotSupportedException();


            public ImageMoniker ExpandedIcon => throw new NotSupportedException();


            IHierarchyNode? IHierarchyNode.Parent => throw new NotSupportedException();
        }
    }

}
