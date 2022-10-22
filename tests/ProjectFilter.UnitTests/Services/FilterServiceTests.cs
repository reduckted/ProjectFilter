using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using ProjectFilter.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Xunit;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services;


public static class FilterServiceTests {

    private static readonly Guid ProjectAlpha = new("1959e9e4-da3c-4532-86c1-035615646892");
    private static readonly Guid ProjectBeta = new("20bb948d-36ad-4814-9171-62c4163e750b");
    private static readonly Guid ProjectGamma = new("33049b07-b3f7-4cc3-aa03-040307074b97");
    private static readonly Guid ProjectDelta = new("4826a53b-4ea3-4fc6-ad1d-465614ed5137");


    public class ApplyMethod : ServiceTest<FilterService> {

        private readonly Dictionary<Guid, List<string>> _dependencies = new();
        private TestHierarchyItem? _root;


        public ApplyMethod(GlobalServiceProvider serviceProvider) : base(serviceProvider) { }


        [Fact]
        public async Task LoadsSpecifiedProjects() {
            Setup(
                $@"
                    <solution name='root'>
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
                    false,
                    false
                )
            );

            Verify(
                $@"
                    <solution name='root'>
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
                    <solution name='root'>
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
                    false,
                    false
                )
            );

            Verify(
                $@"
                    <solution name='root'>
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
                    <solution name='root'>
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
                    false,
                    false
                )
            );

            Verify(
                $@"
                    <solution name='root'>
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
                    <solution name='root'>
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
                    true,
                    false
                )
            );

            Verify(
                $@"
                    <solution name='root'>
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
                    <solution name='root'>
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
                    false,
                    false
                )
            );

            Verify(
                $@"
                    <solution name='root'>
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
                    <solution name='root'>
                        <unloaded name='alpha' guid='{ProjectAlpha}' dependsOn='beta' />
                        <project name='beta' guid='{ProjectBeta}'/>
                    </solution>
                    "
            );

            await ApplyAsync(
                new FilterOptions(
                    new Guid[] { ProjectAlpha },
                    new Guid[] { ProjectBeta },
                    true,
                    false
                )
            );

            Verify(
                $@"
                    <solution name='root'>
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
                    <solution name='root'>
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
                    true,
                    false
                )
            );

            Verify(
                $@"
                    <solution name='root'>
                        <project name='alpha'/>
                        <project name='beta'/>
                        <unloaded name='gamma'/>
                        <project name='delta'/>
                    </solution>
                    "
            );
        }


        [Fact]
        public async Task LoadsSharedProjectsReferencedByProjectsThatAreLoadedWhenRequested() {
            Setup(
                $@"
                    <solution name='root'>
                        <unloaded name='alpha' guid='{ProjectAlpha}' dependsOn='beta' />
                        <unloaded name='beta' guid='{ProjectBeta}' shared='true' />
                        <unloaded name='gamma' guid='{ProjectGamma}'/>
                    </solution>
                    "
            );

            await ApplyAsync(
                new FilterOptions(
                    new Guid[] { ProjectAlpha },
                    Enumerable.Empty<Guid>(),
                    true,
                    false
                )
            );

            Verify(
                $@"
                    <solution name='root'>
                        <project name='alpha'/>
                        <project name='beta'/>
                        <unloaded name='gamma'/>
                    </solution>
                    "
            );
        }


        [Fact]
        public async Task ExpandsFolderOfProjectsThatWereLoadedWhenOptionIsEnabled() {
            Mock<ISolutionExplorer> solutionExplorer;


            solutionExplorer = new Mock<ISolutionExplorer>();

            Setup(
                $@"
                    <solution name='root'>
                        <folder name='core'>
                            <folder name='test'>
                                <unloaded name='alpha' guid='{ProjectAlpha}'/>
                                <project name='beta' guid='{ProjectBeta}'/>
                            </folder>
                        </folder>
                    </solution>
                    ",
                solutionExplorer: solutionExplorer.Object
            );

            await ApplyAsync(
                new FilterOptions(
                    new Guid[] { ProjectAlpha },
                    Enumerable.Empty<Guid>(),
                    false,
                    true
                )
            );

            solutionExplorer.Verify((x) => x.ExpandAsync(new[] { ProjectAlpha }), Times.Once);
        }


        [Fact]
        public async Task CollapsesProjectsThatWereLoadedWhenOptionIsDisabled() {
            Mock<ISolutionExplorer> solutionExplorer;
            Guid testFolder;
            Guid otherFolder;


            testFolder = new Guid("{1413358E-AD48-4DB9-92E3-238CFF65743D}");
            otherFolder = new Guid("{11C39F56-668C-48B4-B3E4-91F9BA7DB09F}");

            solutionExplorer = new Mock<ISolutionExplorer>();

            solutionExplorer
                .SetupSequence((x) => x.GetExpandedFoldersAsync())
                .ReturnsAsync(new[] { testFolder })
                .ReturnsAsync(new[] { testFolder, otherFolder });

            Setup(
                $@"
                    <solution name='root'>
                        <folder name='core'>
                            <folder name='test'>
                                <unloaded name='alpha' guid='{ProjectAlpha}'/>
                                <project name='beta' guid='{ProjectBeta}'/>
                            </folder>
                        </folder>
                        <folder name='other'>
                            <unloaded name='alpha' guid='{ProjectGamma}'/>
                        </folder>
                    </solution>
                    ",
                solutionExplorer: solutionExplorer.Object
            );

            await ApplyAsync(
                new FilterOptions(
                    new Guid[] { ProjectAlpha, ProjectGamma },
                    Enumerable.Empty<Guid>(),
                    false,
                    false
                )
            );

            solutionExplorer.Verify((x) => x.CollapseAsync(new[] { ProjectAlpha, ProjectGamma, otherFolder }), Times.Once);
        }


        [Fact]
        public async Task DoesNotExpandFolderOfProjectsThatWereUnloaded() {
            Mock<ISolutionExplorer> solutionExplorer;


            solutionExplorer = new Mock<ISolutionExplorer>();

            Setup(
                $@"
                    <solution name='root'>
                        <folder name='core'>
                            <project name='alpha' guid='{ProjectAlpha}'/>
                        </folder>
                    </solution>
                    ",
                solutionExplorer: solutionExplorer.Object
            );

            await ApplyAsync(
                new FilterOptions(
                    Enumerable.Empty<Guid>(),
                    new Guid[] { ProjectAlpha },
                    false,
                    true
                )
            );

            solutionExplorer.Verify((x) => x.ExpandAsync(It.IsAny<IEnumerable<Guid>>()), Times.Once);
            solutionExplorer.Verify((x) => x.ExpandAsync(It.Is<IEnumerable<Guid>>((items) => !items.Any())), Times.Once);
        }


        private void Setup(string data, ISolutionExplorer? solutionExplorer = null) {
            IVsSolution solution;
            IVsSolutionBuildManager2 buildManager;


            _root = Factory.ParseHierarchies(data, CreateNode);
            solution = Factory.CreateSolution(_root);

            buildManager = Factory.CreateBuildManager(_root, _dependencies, solution);

            AddService<SVsSolution>(solution);
            AddService<SVsSolutionBuildManager>(buildManager);
            AddService<IWaitDialogFactory>(MockWaitDialogFactory());
            AddService<ISolutionExplorer>(solutionExplorer ?? Mock.Of<ISolutionExplorer>());
        }


        private TestHierarchyItem CreateNode(XElement _, HierarchyData data) {
            _dependencies[data.Identifier] = data.DependencyNames.ToList();
            return new TestHierarchyItem(data);
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


        private async Task ApplyAsync(FilterOptions options) {
            await CreateService().ApplyAsync(options);
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

            return element;
        }


        private class TestHierarchyItem : TreeItem {

            public TestHierarchyItem(HierarchyData data) : base(data) { }

        }

    }

}
