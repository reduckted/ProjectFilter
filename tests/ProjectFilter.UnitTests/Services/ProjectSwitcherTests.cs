using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using ProjectFilter.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Xunit;
using Task = System.Threading.Tasks.Task;




namespace ProjectFilter.Services {

    public static class ProjectFilterTests {

        private static readonly Guid ProjectAlpha = new("1959e9e4-da3c-4532-86c1-035615646892");
        private static readonly Guid ProjectBeta = new("20bb948d-36ad-4814-9171-62c4163e750b");
        private static readonly Guid ProjectGamma = new("33049b07-b3f7-4cc3-aa03-040307074b97");
        private static readonly Guid ProjectDelta = new("4826a53b-4ea3-4fc6-ad1d-465614ed5137");


        public class ApplyMethod : ServiceTest<FilterService> {

            private readonly Dictionary<Guid, List<string>> _dependencies = new();
            private TestHierarchyItem? _root;


            [Fact]
            public async Task LoadsSpecifiedProjects() {
                Setup(
                    $@"
                    <solution name='root' expanded='true'>
                        <unloaded name='alpha' guid='{ProjectAlpha}'/>
                        <unloaded name='beta' guid='{ProjectBeta}'/>
                        <unloaded name='gamma' guid='{ProjectGamma}'/>
                        <unloaded name='delta' guid='{ProjectDelta}'/>
                    </solution>
                    "
                );

                await ApplyAsync(
                    new FilterOptions(
                        new Guid[] { ProjectAlpha, ProjectBeta },
                        Enumerable.Empty<Guid>(),
                        false
                    )
                );

                Verify(
                    $@"
                    <solution name='root' expanded='true'>
                        <project name='alpha'/>
                        <project name='beta'/>
                        <unloaded name='gamma'/>
                        <unloaded name='delta'/>
                    </solution>
                    "
                );
            }


            [Fact]
            public async Task UnloadsSpecifiedProjects() {
                Setup(
                    $@"
                    <solution name='root' expanded='true'>
                        <project name='alpha' guid='{ProjectAlpha}'/>
                        <project name='beta' guid='{ProjectBeta}'/>
                        <project name='gamma' guid='{ProjectGamma}'/>
                        <project name='delta' guid='{ProjectDelta}'/>
                    </solution>
                    "
                );

                await ApplyAsync(
                    new FilterOptions(
                        Enumerable.Empty<Guid>(),
                        new Guid[] { ProjectAlpha, ProjectBeta },
                        false
                    )
                );

                Verify(
                    $@"
                    <solution name='root' expanded='true'>
                        <unloaded name='alpha'/>
                        <unloaded name='beta'/>
                        <project name='gamma'/>
                        <project name='delta'/>
                    </solution>
                    "
                );
            }


            [Fact]
            public async Task LoadsAndUnloadsSpecifiedProjects() {
                Setup(
                    $@"
                    <solution name='root' expanded='true'>
                        <project name='alpha' guid='{ProjectAlpha}'/>
                        <unloaded name='beta' guid='{ProjectBeta}'/>
                        <project name='gamma' guid='{ProjectGamma}'/>
                        <unloaded name='delta' guid='{ProjectDelta}'/>
                    </solution>
                    "
                );

                await ApplyAsync(
                    new FilterOptions(
                        new Guid[] { ProjectBeta },
                        new Guid[] { ProjectAlpha },
                        false
                    )
                );

                Verify(
                    $@"
                    <solution name='root' expanded='true'>
                        <unloaded name='alpha'/>
                        <project name='beta'/>
                        <project name='gamma'/>
                        <unloaded name='delta'/>
                    </solution>
                    "
                );
            }


            [Fact]
            public async Task LoadsDependenciesOfProjectsThatAreLoadedWhenRequested() {
                Setup(
                    $@"
                    <solution name='root' expanded='true'>
                        <unloaded name='alpha' guid='{ProjectAlpha}' dependsOn='beta,gamma' />
                        <unloaded name='beta' guid='{ProjectBeta}'/>
                        <unloaded name='gamma' guid='{ProjectGamma}'/>
                        <unloaded name='delta' guid='{ProjectDelta}'/>
                    </solution>
                    "
                );

                await ApplyAsync(
                    new FilterOptions(
                        new Guid[] { ProjectAlpha },
                        Enumerable.Empty<Guid>(),
                        true
                    )
                );

                Verify(
                    $@"
                    <solution name='root' expanded='true'>
                        <project name='alpha'/>
                        <project name='beta'/>
                        <project name='gamma'/>
                        <unloaded name='delta'/>
                    </solution>
                    "
                );
            }


            [Fact]
            public async Task DoesNotLoadDependenciesOfProjectsThatAreLoadedWhenNotRequested() {
                Setup(
                    $@"
                    <solution name='root' expanded='true'>
                        <unloaded name='alpha' guid='{ProjectAlpha}' dependsOn='beta' />
                        <unloaded name='beta' guid='{ProjectBeta}'/>
                        <unloaded name='gamma' guid='{ProjectGamma}'/>
                        <unloaded name='delta' guid='{ProjectDelta}'/>
                    </solution>
                    "
                );

                await ApplyAsync(
                    new FilterOptions(
                        new Guid[] { ProjectAlpha },
                        Enumerable.Empty<Guid>(),
                        false
                    )
                );

                Verify(
                    $@"
                    <solution name='root' expanded='true'>
                        <project name='alpha'/>
                        <unloaded name='beta'/>
                        <unloaded name='gamma'/>
                        <unloaded name='delta'/>
                    </solution>
                    "
                );
            }


            [Fact]
            public async Task LoadsDependencyOfProjectThatIsLoadedWhenThatProjectShouldBeUnloaded() {
                Setup(
                    $@"
                    <solution name='root' expanded='true'>
                        <unloaded name='alpha' guid='{ProjectAlpha}' dependsOn='beta' />
                        <project name='beta' guid='{ProjectBeta}'/>
                    </solution>
                    "
                );

                await ApplyAsync(
                    new FilterOptions(
                        new Guid[] { ProjectAlpha },
                        new Guid[] { ProjectBeta },
                        true
                    )
                );

                Verify(
                    $@"
                    <solution name='root' expanded='true'>
                        <project name='alpha'/>
                        <project name='beta'/>
                    </solution>
                    "
                );
            }


            [Fact]
            public async Task LoadsDependenciesOfDependenciesOfProjectsThatAreLoadedWhenRequested() {
                Setup(
                    $@"
                    <solution name='root' expanded='true'>
                        <unloaded name='alpha' guid='{ProjectAlpha}' dependsOn='beta' />
                        <unloaded name='beta' guid='{ProjectBeta}' dependsOn='delta'/>
                        <unloaded name='gamma' guid='{ProjectGamma}'/>
                        <unloaded name='delta' guid='{ProjectDelta}'/>
                    </solution>
                    "
                );

                await ApplyAsync(
                    new FilterOptions(
                        new Guid[] { ProjectAlpha },
                        Enumerable.Empty<Guid>(),
                        true
                    )
                );

                Verify(
                    $@"
                    <solution name='root' expanded='true'>
                        <project name='alpha'/>
                        <project name='beta'/>
                        <unloaded name='gamma'/>
                        <project name='delta'/>
                    </solution>
                    "
                );
            }


            [Fact]
            public async Task KeepsSelectedProjectSelected() {
                Setup(
                    $@"
                    <solution name='root' expanded='true'>
                        <unloaded name='alpha' guid='{ProjectAlpha}'/>
                        <project name='beta' guid='{ProjectBeta}'/>
                        <project name='gamma' guid='{ProjectGamma}' selected='true'/>
                    </solution>
                    "
                );

                await ApplyAsync(
                    new FilterOptions(
                        new Guid[] { ProjectAlpha },
                        new Guid[] { ProjectBeta },
                        false
                    )
                );

                Verify(
                    $@"
                    <solution name='root' expanded='true'>
                        <project name='alpha'/>
                        <unloaded name='beta'/>
                        <project name='gamma' selected='true'/>
                    </solution>
                    "
                );
            }


            [Fact]
            public async Task ExpandsFolderOfProjectsThatWereLoaded() {
                Setup(
                    $@"
                    <solution name='root'>
                        <folder name='core'>
                            <folder name='test'>
                                <unloaded name='alpha' guid='{ProjectAlpha}'/>
                            </folder>
                        </folder>
                    </solution>
                    "
                );

                await ApplyAsync(
                    new FilterOptions(
                        new Guid[] { ProjectAlpha },
                        Enumerable.Empty<Guid>(),
                        false
                    )
                );

                Verify(
                    $@"
                    <solution name='root' expanded='true'>
                        <folder name='core' expanded='true'>
                            <folder name='test' expanded='true'>
                                <project name='alpha'/>
                            </folder>
                        </folder>
                    </solution>
                    "
                );
            }


            [Fact]
            public async Task DoesNotExpandFolderOfProjectsThatWereUnloaded() {
                Setup(
                    $@"
                    <solution name='root'>
                        <folder name='core'>
                            <project name='alpha' guid='{ProjectAlpha}'/>
                        </folder>
                    </solution>
                    "
                );

                await ApplyAsync(
                    new FilterOptions(
                        Enumerable.Empty<Guid>(),
                        new Guid[] { ProjectAlpha },
                        false
                    )
                );

                Verify(
                    $@"
                    <solution name='root'>
                        <folder name='core'>
                            <unloaded name='alpha'/>
                        </folder>
                    </solution>
                    "
                );
            }


            private void Setup(string data) {
                IVsSolution solution;
                IVsSolutionBuildManager2 buildManager;


                _root = Factory.ParseHierarchies<TestHierarchyItem>(data, CreateNode);
                solution = Factory.CreateSolution(_root);

                buildManager = Factory.CreateBuildManager(_root, _dependencies, solution);

                AddService<SVsSolution, IVsSolution>(solution);
                AddService<DTE, DTE2>(MockDTE(_root));
                AddService<SVsSolutionBuildManager, IVsSolutionBuildManager2>(buildManager);
                AddService<IWaitDialogFactory, IWaitDialogFactory>(MockWaitDialogFactory());
            }


            private TestHierarchyItem CreateNode(XElement element, TestHierarchyItem? parent) {
                HierarchyData data;
                TestUIItem ui;
                string? dependencies;


                data = Factory.CreateHierarchyData(element, parent?.Data);

                ui = CreateUIHierarchyItem(element, data.Name, parent?.UI);
                parent?.UI.Add(ui);

                dependencies = element.Attribute("dependsOn")?.Value;

                if (dependencies is not null) {
                    _dependencies[data.Identifier] = dependencies.Split(',').ToList();
                }

                return new TestHierarchyItem(data, ui);
            }


            private static TestUIItem CreateUIHierarchyItem(XElement element, string name, TestUIItem? parent) {
                TestUIItem item;


                item = new TestUIItem(name, parent?.UIHierarchyItems);

                if (element.Attribute("selected")?.Value == "true") {
                    item.IsSelected = true;
                }

                if (element.Attribute("expanded")?.Value == "true") {
                    item.UIHierarchyItems.Expanded = true;
                }

                return item;
            }


            private static IWaitDialogFactory MockWaitDialogFactory() {
                Mock<IWaitDialogFactory> factory;
                Mock<IWaitDialog> dialog;


                dialog = new Mock<IWaitDialog>();
                dialog.SetupGet((x) => x.CancellationToken).Returns(CancellationToken.None);

                factory = new Mock<IWaitDialogFactory>();
                factory.Setup((x) => x.CreateAsync(It.IsAny<string>(), It.IsAny<ThreadedWaitDialogProgressData>())).ReturnsAsync(dialog.Object);

                return factory.Object;
            }


            private DTE2 MockDTE(TestHierarchyItem root) {
                Mock<DTE> dte;
                Mock<DTE2> dte2;
                Mock<ToolWindows> toolWindows;
                Mock<UIHierarchy> solutionExplorer;
                Mock<Window> solutionExplorerWindow;
                Mock<Solution> solution;
                Mock<EnvDTE.Commands> commands;
                Mock<Command> command;


                solutionExplorerWindow = new Mock<Window>();
                solutionExplorerWindow.SetupProperty((x) => x.Visible);

                solutionExplorer = new Mock<UIHierarchy>();
                solutionExplorer.SetupGet((x) => x.Parent).Returns(solutionExplorerWindow.Object);
                solutionExplorer.Setup((x) => x.GetItem(It.IsAny<string>())).Returns((string path) => GetItem(root, path));
                solutionExplorer
                    .SetupGet((x) => x.SelectedItems)
                    .Returns(() => root.DescendantsAndSelf().Where((x) => x.UI.IsSelected).Select((x) => x.UI).ToArray());

                toolWindows = new Mock<ToolWindows>();
                toolWindows.SetupGet((x) => x.SolutionExplorer).Returns(solutionExplorer.Object);

                solution = new Mock<Solution>();
                solution.Setup((x) => x.Properties);

                command = new Mock<Command>();
                command.SetupGet((x) => x.IsAvailable).Returns(true);

                commands = new Mock<EnvDTE.Commands>();
                commands.Setup((x) => x.Item(It.IsAny<object>(), It.IsAny<int>())).Returns(command.Object);

                dte = new Mock<DTE>();
                dte2 = dte.As<DTE2>();
                dte2.SetupGet((x) => x.ToolWindows).Returns(toolWindows.Object);
                dte2.SetupGet((x) => x.Commands).Returns(commands.Object);

                dte.SetupGet((x) => x.Solution).Returns(solution.Object);

                return dte2.Object;
            }


            private UIHierarchyItem GetItem(TestHierarchyItem item, string path) {
                string[] parts;


                parts = path.Split(new[] { '\\' }, 2);

                if (item.Data.Name == parts[0]) {
                    if (parts.Length == 1) {
                        return item.UI;
                    }

                    return item
                        .Children
                        .Cast<TestHierarchyItem>()
                        .Select((x) => GetItem(x, parts[0]))
                        .FirstOrDefault((x) => x is not null);
                }

                return null!;
            }


            private async Task ApplyAsync(FilterOptions options) {
                await (await CreateAsync()).ApplyAsync(options);
            }


            private void Verify(string expected) {
                XElement solution;


                if (_root is null) {
                    throw new InvalidOperationException("You must setup the test before verifying.");
                }

                solution = ConvertToElement(_root);

                Assert.Equal(
                    XDocument.Parse(expected).Root.ToString(),
                    solution.ToString()
                );
            }


            private XElement ConvertToElement(TestHierarchyItem node) {
                XElement element;


                element = new XElement(XName.Get(Factory.TypeToElementName(node.Data.Type)));
                element.SetAttributeValue("name", node.Data.Name);

                element.Add(node.Children.Cast<TestHierarchyItem>().Select(ConvertToElement).ToArray());

                if (node.UI.IsSelected) {
                    element.SetAttributeValue("selected", "true");
                }
                if (node.UI.UIHierarchyItems.Expanded) {
                    element.SetAttributeValue("expanded", "true");
                }

                return element;
            }


            private class TestHierarchyItem : TreeItem {

                public TestHierarchyItem(HierarchyData data, TestUIItem ui) : base(data) {
                    UI = ui;
                }


                public TestUIItem UI { get; set; }

            }


            private class TestUIItem : UIHierarchyItem {

                private readonly TestUIItems _children;


                public TestUIItem(string name, UIHierarchyItems? owner) {
                    Name = name;
                    _children = new TestUIItems(this);
                    Collection = owner;
                }


                public void Select(vsUISelectionType How) {
                    IsSelected = true;
                }


                public DTE DTE => throw new NotSupportedException();


                public UIHierarchyItems? Collection { get; }


                public string Name { get; }


                public UIHierarchyItems UIHierarchyItems => _children;


                public object Object => throw new NotSupportedException();


                public bool IsSelected { get; set; }


                public void Add(TestUIItem child) {
                    _children.Add(child);
                }
            }


            private class TestUIItems : UIHierarchyItems {

                private readonly List<UIHierarchyItem> _items = new();

                public TestUIItems(TestUIItem? parent) {
                    Parent = parent;
                }


                public UIHierarchyItem Item(object index) {
                    if (index is int i) {
                        return _items[i];
                    }

                    if (index is string name) {
                        return _items.FirstOrDefault((x) => x.Name == name) ?? throw new ArgumentException("Not found.", nameof(index));
                    }

                    throw new NotSupportedException();
                }


                public IEnumerator GetEnumerator() {
                    throw new NotSupportedException();
                }


                public DTE DTE => throw new NotSupportedException();


                public object? Parent { get; }


                public int Count => _items.Count;


                public bool Expanded { get; set; }


                public void Add(TestUIItem item) {
                    _items.Add(item);
                }

            }

        }

    }

}
