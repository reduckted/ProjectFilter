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

    public class GetSolutionFoldersWithoutProjectsMethod {

        [Fact]
        public async Task FindsNoFoldersWhenSolutionIsEmpty() {
            Node solution;


            solution = await CreateSolutionAsync(
                """
                <solution/>
                """
            );

            Assert.Empty(GetSolutionFoldersWithoutProjects(solution));
        }


        [Fact]
        public async Task IncludesFolderWhenItHasOnlyUnloadedProjects() {
            Node solution;


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

            Assert.Equal(["a"], GetSolutionFoldersWithoutProjects(solution));
        }


        [Fact]
        public async Task DoesNotIncludeFolderWhenItHasLoadedProjects() {
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

            Assert.Empty(GetSolutionFoldersWithoutProjects(solution));
        }


        [Fact]
        public async Task DoesNotIncludeFolderWhenItHasLoadedAndUnloadedProjects() {
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

            Assert.Empty(GetSolutionFoldersWithoutProjects(solution));
        }


        [Fact]
        public async Task IncludesParentAndChildFoldersWhenChildFoldersCanBeHidden() {
            Node solution;


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

            Assert.Equal(["a", "b", "d"], GetSolutionFoldersWithoutProjects(solution));
        }


        [Fact]
        public async Task IncludesChildFoldersWhenParentFolderCannotBeHidden() {
            Node solution;


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

            Assert.Equal(["b", "d"], GetSolutionFoldersWithoutProjects(solution));
        }


        [Fact]
        public async Task CanFindMultipleFolders() {
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
                            </folder>
                        </folder>
                    </folder>
                </solution>
                """
            );

            Assert.Equal(["e", "k", "n", "o", "p", "q", "r"], GetSolutionFoldersWithoutProjects(solution));
        }


        private static async Task<Node> CreateSolutionAsync(string data) {
            return Factory.ParseHierarchies(data, CreateNode);
        }


        private static Node CreateNode(XElement element, HierarchyData data) {
            return new Node(data);
        }


        private static IEnumerable<string> GetSolutionFoldersWithoutProjects(IHierarchyNode root) {
            return SolutionExplorer
                .GetSolutionFoldersWithoutLoadedProjects(root.Children)
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
