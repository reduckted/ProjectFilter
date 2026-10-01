using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using NSubstitute;
using ProjectFilter.Helpers;
using ProjectFilter.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Xunit;


namespace ProjectFilter.UI;


[SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "Not using VS services.")]
public static class FilterDialogViewModelTests {

    public class LoadingVisibilityProperty : TestBase {

        public LoadingVisibilityProperty(GlobalServiceProvider serviceProvider) : base(serviceProvider) { }


        [Fact]
        public async Task IsVisibleUntilHierarchyIsRetrieved() {
            TaskCompletionSource<IEnumerable<IHierarchyNode>> hierarchy;
            FilterDialogViewModel vm;
            Task loaded;


            hierarchy = new TaskCompletionSource<IEnumerable<IHierarchyNode>>();

            vm = CreateViewModel(() => hierarchy.Task);

            Assert.Equal(Visibility.Visible, vm.LoadingVisibility);

            loaded = vm.OnLoadedAsync(SearchHost);

            Assert.Equal(Visibility.Visible, vm.LoadingVisibility);

            hierarchy.SetResult(
                [
                        CreateNode("a", isLoaded: true),
                            CreateNode("b", isLoaded: false)
                ]
            );

            await loaded;

            Assert.Equal(Visibility.Collapsed, vm.LoadingVisibility);
        }

    }


    public class LoadedVisibilityProperty : TestBase {

        public LoadedVisibilityProperty(GlobalServiceProvider serviceProvider) : base(serviceProvider) { }


        [Fact]
        public async Task IsCollapsedUntilHierarchyIsRetrieved() {
            TaskCompletionSource<IEnumerable<IHierarchyNode>> hierarchy;
            FilterDialogViewModel vm;
            Task loaded;


            hierarchy = new TaskCompletionSource<IEnumerable<IHierarchyNode>>();

            vm = CreateViewModel(() => hierarchy.Task);


            Assert.Equal(Visibility.Collapsed, vm.LoadedVisibility);

            loaded = vm.OnLoadedAsync(SearchHost);

            Assert.Equal(Visibility.Collapsed, vm.LoadedVisibility);

            hierarchy.SetResult(
                [
                        CreateNode("a", isLoaded: true),
                            CreateNode("b", isLoaded: false)
                ]
            );

            await loaded;

            Assert.Equal(Visibility.Visible, vm.LoadedVisibility);
        }

    }


    public class ItemsProperty : TestBase {

        public ItemsProperty(GlobalServiceProvider serviceProvider) : base(serviceProvider) { }


        [Fact]
        public async Task IsEmptyUntilHierarchyIsRetrieved() {
            TaskCompletionSource<IEnumerable<IHierarchyNode>> hierarchy;
            FilterDialogViewModel vm;
            Task loaded;


            hierarchy = new TaskCompletionSource<IEnumerable<IHierarchyNode>>();

            vm = CreateViewModel(() => hierarchy.Task);

            Assert.Empty(vm.Items);

            loaded = vm.OnLoadedAsync(SearchHost);

            Assert.Empty(vm.Items);

            hierarchy.SetResult(
                [
                    CreateNode("a", isLoaded: true),
                    CreateNode("b", isLoaded: false)
                ]
            );

            await loaded;

            Assert.Equal(
                [
                    ("a", (bool?)true),
                        ("b", (bool?)false)
                ],
                vm.Items.Select((x) => (x.Name, x.IsChecked))
            );
        }


        [Fact]
        public async Task InitiallyChecksItemsBasedOnWhetherTheyAreLoadedOrTheirChildrenAreChecked() {
            IEnumerable<IHierarchyNode> hierarchy;
            FilterDialogViewModel vm;


            hierarchy = [
                CreateNode("a", isLoaded: true),
                CreateNode("b", isLoaded: false),
                CreateNode("c", children: [CreateNode("d", isLoaded: true)]),
                CreateNode("e", children: [CreateNode("f", isLoaded: true), CreateNode("g", isLoaded: false)]),
            ];

            vm = CreateViewModel(hierarchy);
            await vm.OnLoadedAsync(SearchHost);

            Assert.Equal(
                [
                    ("a", true),
                    ("b", false),
                    ("c", true),
                    ("e", null)
                ],
                vm.Items.Select((x) => (x.Name, x.IsChecked))
            );
        }


        [Fact]
        public async Task InitiallyExpandsAllItemsWhenThereAreNoNodeSettings() {
            IEnumerable<IHierarchyNode> hierarchy;
            FilterDialogViewModel vm;


            hierarchy = [
                CreateNode("a"),
                CreateNode("b"),
                CreateNode("c", children: [CreateNode("d")]),
                CreateNode("e", children: [CreateNode("f", children: [CreateNode("g")])]),
            ];

            vm = CreateViewModel(hierarchy, nodeSettings: null);
            await vm.OnLoadedAsync(SearchHost);

            Assert.Equal(
                [
                    ("a", true),
                    ("b", true),
                    ("c", true),
                    ("d", true),
                    ("e", true),
                    ("f", true),
                    ("g", true)
                ],
                Flatten(vm.Items).Select((x) => (x.Name, x.IsExpanded))
            );
        }


        [Fact]
        public async Task InitiallyExpandsItemsBasedOnTheGivenNodeSettings() {
            IEnumerable<IHierarchyNode> hierarchy;
            Dictionary<string, SolutionNodeSettings> nodeSettings;
            FilterDialogViewModel vm;


            hierarchy = [
                CreateNode("a"),
                CreateNode("b"),
                CreateNode("c", children: [CreateNode("d")]),
                CreateNode("e", children: [CreateNode("f", children: [CreateNode("g")])]),
            ];

            nodeSettings = [];
            AddNodeSetting(nodeSettings, "a", true);
            AddNodeSetting(nodeSettings, "b", false);
            AddNodeSetting(nodeSettings, "c", true);
            AddNodeSetting(nodeSettings, "c/d", false);
            AddNodeSetting(nodeSettings, "e", true);
            AddNodeSetting(nodeSettings, "e/f", false);
            // Omit "g" to prove that the default is to be expanded.

            vm = CreateViewModel(hierarchy, nodeSettings: nodeSettings);
            await vm.OnLoadedAsync(SearchHost);

            Assert.Equal(
                [
                    ("a", true),
                    ("b", false),
                    ("c", true),
                    ("d", false),
                    ("e", true),
                    ("f", false),
                    ("g", true)
                ],
                Flatten(vm.Items).Select((x) => (x.Name, x.IsExpanded))
            );

            static void AddNodeSetting(Dictionary<string, SolutionNodeSettings> nodeSettings, string path, bool isExpanded) {
                Dictionary<string, SolutionNodeSettings> collection;
                SolutionNodeSettings? node;


                collection = nodeSettings;
                node = null;

                foreach (var segment in path.Split('/')) {
                    if (!collection.TryGetValue(segment, out node)) {
                        node = new SolutionNodeSettings();
                        collection[segment] = node;
                    }

                    collection = node.Children;
                }

                node?.IsExpanded = isExpanded;
            }
        }


        private static IEnumerable<HierarchyTreeViewItem> Flatten(IEnumerable<HierarchyTreeViewItem> items) {
            foreach (var item in items) {
                yield return item;

                foreach (var descendant in Flatten(item.Children)) {
                    yield return descendant;
                }
            }
        }

    }


    public class ToggleLoadProjectDependenciesCommandProperty : TestBase {

        public ToggleLoadProjectDependenciesCommandProperty(GlobalServiceProvider serviceProvider) : base(serviceProvider) { }


        [Fact]
        public async Task TogglesTheLoadProjectDependenciesProperty() {
            FilterDialogViewModel vm;


            vm = CreateViewModel([]);
            await vm.OnLoadedAsync(SearchHost);

            Assert.False(vm.LoadProjectDependencies);

            vm.ToggleLoadProjectDependenciesCommand.Execute(null);

            Assert.True(vm.LoadProjectDependencies);

            vm.ToggleLoadProjectDependenciesCommand.Execute(null);

            Assert.False(vm.LoadProjectDependencies);
        }

    }


    public class ToggleHideSolutionFoldersWithoutLoadedProjectsCommandProperty : TestBase {

        public ToggleHideSolutionFoldersWithoutLoadedProjectsCommandProperty(GlobalServiceProvider serviceProvider) : base(serviceProvider) { }


        [Fact]
        public async Task TogglesTheHideSolutionFoldersWithoutLoadedProjectsProperty() {
            FilterDialogViewModel vm;


            vm = CreateViewModel([]);
            await vm.OnLoadedAsync(SearchHost);

            Assert.False(vm.HideSolutionFoldersWithoutLoadedProjects);

            vm.ToggleHideSolutionFoldersWithoutLoadedProjectsCommand.Execute(null);

            Assert.True(vm.HideSolutionFoldersWithoutLoadedProjects);

            vm.ToggleHideSolutionFoldersWithoutLoadedProjectsCommand.Execute(null);

            Assert.False(vm.HideSolutionFoldersWithoutLoadedProjects);
        }

    }


    public class ToggleHideSolutionFoldersWithOnlyFilesCommandProperty : TestBase {

        public ToggleHideSolutionFoldersWithOnlyFilesCommandProperty(GlobalServiceProvider serviceProvider) : base(serviceProvider) { }


        [Fact]
        public async Task TogglesTheHideSolutionFoldersWithOnlyFilesProperty() {
            FilterDialogViewModel vm;


            vm = CreateViewModel([]);
            await vm.OnLoadedAsync(SearchHost);

            Assert.False(vm.HideSolutionFoldersWithOnlyFiles);

            vm.ToggleHideSolutionFoldersWithOnlyFilesCommand.Execute(null);

            Assert.True(vm.HideSolutionFoldersWithOnlyFiles);

            vm.ToggleHideSolutionFoldersWithOnlyFilesCommand.Execute(null);

            Assert.False(vm.HideSolutionFoldersWithOnlyFiles);
        }

    }


    public class CollapseAllCommandProperty : TestBase {

        public CollapseAllCommandProperty(GlobalServiceProvider serviceProvider) : base(serviceProvider) { }


        [Fact]
        public async Task CollapsesAllItemsWhenNoParameterIsSpecified() {
            IEnumerable<IHierarchyNode> hierarchy;
            FilterDialogViewModel vm;


            hierarchy = [
                    CreateNode(name: "a", children: [
                        CreateNode("b", children: [
                            CreateNode("c")
                        ])
                    ]),
                    CreateNode(name: "d", children: [
                        CreateNode("e", children: [
                            CreateNode("f")
                        ])
                    ])
                ];

            vm = CreateViewModel(hierarchy);
            await vm.OnLoadedAsync(SearchHost);

            vm.CollapseAllCommand.Execute(null);
            Assert.All(vm.Items.GetFullHierarchy(), (x) => Assert.False(x.IsExpanded));
        }


        [Fact]
        public async Task CollapsesSpecifiedItemAndAllOfItsDescendantsWhenItHasChildren() {
            IEnumerable<IHierarchyNode> hierarchy;
            FilterDialogViewModel vm;


            hierarchy = [
                CreateNode(name: "a", children: [
                    CreateNode("b", children: [
                        CreateNode("c")
                    ])
                ]),
                CreateNode(name: "d", children: [
                    CreateNode("e", children: [
                        CreateNode("f")
                    ])
                ])
            ];

            vm = CreateViewModel(hierarchy);
            await vm.OnLoadedAsync(SearchHost);

            vm.CollapseAllCommand.Execute(GetItem(vm, "b"));

            Assert.Equal(
                [
                    ("a", true),
                    ("b", false),
                    ("c", false),
                    ("d", true),
                    ("e", true),
                    ("f", true)
                ],
                vm.Items.GetFullHierarchy().Select((x) => (x.Name, x.IsExpanded))
            );
        }


        [Fact]
        public async Task CollapsesParentOfSpecifiedItemWhenSpecifiedItemHasNoChildren() {
            IEnumerable<IHierarchyNode> hierarchy;
            FilterDialogViewModel vm;


            hierarchy = [
                CreateNode(name: "a", children: [
                    CreateNode("b", children: [
                        CreateNode("c")
                    ])
                ]),
                CreateNode(name: "d", children: [
                    CreateNode("e", children: [
                        CreateNode("f")
                    ])
                ])
            ];

            vm = CreateViewModel(hierarchy);
            await vm.OnLoadedAsync(SearchHost);

            vm.CollapseAllCommand.Execute(GetItem(vm, "f"));

            Assert.Equal(
                [
                        ("a", true),
                        ("b", true),
                        ("c", true),
                        ("d", true),
                        ("e", false),
                        ("f", false)
                ],
                vm.Items.GetFullHierarchy().Select((x) => (x.Name, x.IsExpanded))
            );
        }

    }


    public class ExpandAllCommandProperty : TestBase {

        public ExpandAllCommandProperty(GlobalServiceProvider serviceProvider) : base(serviceProvider) { }


        [Fact]
        public async Task ExpandsAllItemsWhenNoParameterIsSpecified() {
            IEnumerable<IHierarchyNode> hierarchy;
            FilterDialogViewModel vm;


            hierarchy = [
                CreateNode(name: "a", children: [
                    CreateNode("b", children: [
                        CreateNode("c")
                    ])
                ]),
                CreateNode(name: "d", children: [
                    CreateNode("e", children: [
                        CreateNode("f")
                    ])
                ])
            ];

            vm = CreateViewModel(hierarchy);
            await vm.OnLoadedAsync(SearchHost);

            vm.CollapseAllCommand.Execute(null);
            vm.ExpandAllCommand.Execute(null);
            Assert.All(vm.Items.GetFullHierarchy(), (x) => Assert.True(x.IsExpanded));
        }


        [Fact]
        public async Task ExpandsTheSpecifiedItemAndAllOfItsDescendants() {
            IEnumerable<IHierarchyNode> hierarchy;
            FilterDialogViewModel vm;


            hierarchy = [
                CreateNode(name: "a", children: [
                    CreateNode("b", children: [
                        CreateNode("c")
                    ])
                ]),
                CreateNode(name: "d", children: [
                    CreateNode("e", children: [
                        CreateNode("f")
                    ])
                ])
            ];

            vm = CreateViewModel(hierarchy);
            await vm.OnLoadedAsync(SearchHost);

            vm.CollapseAllCommand.Execute(null);
            vm.ExpandAllCommand.Execute(GetItem(vm, "b"));

            Assert.Equal(
                [
                    ("a", false),
                    ("b", true),
                    ("c", true),
                    ("d", false),
                    ("e", false),
                    ("f", false)
                ],
                vm.Items.GetFullHierarchy().Select((x) => (x.Name, x.IsExpanded))
            );
        }

    }


    public class CheckAllCommandProperty : TestBase {

        public CheckAllCommandProperty(GlobalServiceProvider serviceProvider) : base(serviceProvider) { }


        [Fact]
        public async Task ChecksAllItems() {
            IEnumerable<IHierarchyNode> hierarchy;
            FilterDialogViewModel vm;


            hierarchy = [
                CreateNode(name: "a", children: [
                    CreateNode("b", children: [
                        CreateNode("c", isLoaded: false)
                    ])
                ]),
                CreateNode(name: "d", children: [
                    CreateNode("e", children: [
                        CreateNode("f", isLoaded: false)
                    ])
                ])
            ];

            vm = CreateViewModel(hierarchy);
            await vm.OnLoadedAsync(SearchHost);

            Assert.All(vm.Items.GetFullHierarchy(), (x) => Assert.False(x.IsChecked));

            vm.CheckAllCommand.Execute(null);

            Assert.All(vm.Items.GetFullHierarchy(), (x) => Assert.True(x.IsChecked));
        }

    }


    public class UncheckAllCommandProperty : TestBase {

        public UncheckAllCommandProperty(GlobalServiceProvider serviceProvider) : base(serviceProvider) { }


        [Fact]
        public async Task UnchecksAllItems() {
            IEnumerable<IHierarchyNode> hierarchy;
            FilterDialogViewModel vm;


            hierarchy = [
                CreateNode(name: "a", children: [
                    CreateNode("b", children: [
                        CreateNode("c", isLoaded: true)
                    ])
                ]),
                CreateNode(name: "d", children: [
                    CreateNode("e", children: [
                        CreateNode("f", isLoaded: true)
                    ])
                ])
            ];

            vm = CreateViewModel(hierarchy);
            await vm.OnLoadedAsync(SearchHost);

            Assert.All(vm.Items.GetFullHierarchy(), (x) => Assert.True(x.IsChecked));

            vm.UncheckAllCommand.Execute(null);

            Assert.All(vm.Items.GetFullHierarchy(), (x) => Assert.False(x.IsChecked));
        }

    }


    public class FocusSearchBoxCommandProperty : TestBase {

        public FocusSearchBoxCommandProperty(GlobalServiceProvider serviceProvider) : base(serviceProvider) { }


        [Fact]
        public async Task ActivatesSearchHost() {
            IEnumerable<IHierarchyNode> hierarchy;
            FilterDialogViewModel vm;


            hierarchy = [CreateNode(name: "a")];

            vm = CreateViewModel(hierarchy);
            await vm.OnLoadedAsync(SearchHost);

            // The search host should always be
            // activated when the view model loads.
            SearchHost.Received(1).Activate();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(
                TestContext.Current.CancellationToken
            );

            // Requesting focus should activate it again.
            vm.FocusSearchBoxCommand.Execute(null);
            SearchHost.Received(2).Activate();
        }

    }


    public class AcceptCommandProperty : TestBase {

        public AcceptCommandProperty(GlobalServiceProvider serviceProvider) : base(serviceProvider) { }


        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SetsOptionsFromProperties(bool loadDependencies) {
            IEnumerable<IHierarchyNode> hierarchy;
            FilterDialogViewModel vm;


            hierarchy = [
                CreateNode(name: "a"),
                CreateNode(name: "b")
            ];

            vm = CreateViewModel(hierarchy);
            await vm.OnLoadedAsync(SearchHost);

            vm.LoadProjectDependencies = loadDependencies;

            vm.AcceptCommand.Execute(null);

            Assert.NotNull(vm.Result);
            Assert.Equal(loadDependencies, vm.Result!.LoadProjectDependencies);
        }


        [Fact]
        public async Task SetsProjectsBasedOnCheckedState() {
            IEnumerable<IHierarchyNode> hierarchy;
            FilterDialogViewModel vm;


            hierarchy = [
                CreateNode(name: "a", children: [
                    CreateNode("b"),
                    CreateNode("c", children: [
                        CreateNode("d")
                    ])
                ]),
                CreateNode(name: "e", children: [
                    CreateNode("f"),
                    CreateNode("g", children: [
                        CreateNode("h")
                    ])
                ])
            ];

            vm = CreateViewModel(hierarchy);
            await vm.OnLoadedAsync(SearchHost);

            GetItem(vm, "b").IsChecked = true;
            GetItem(vm, "d").IsChecked = false;
            GetItem(vm, "f").IsChecked = false;
            GetItem(vm, "h").IsChecked = true;

            vm.AcceptCommand.Execute(null);

            Assert.NotNull(vm.Result);

            Assert.Equal(
                [GetItem(vm, "b").Identifier, GetItem(vm, "h").Identifier],
                vm.Result!.ProjectsToLoad
            );

            Assert.Equal(
                [GetItem(vm, "d").Identifier, GetItem(vm, "f").Identifier],
                vm.Result!.ProjectsToUnload
            );
        }


        [Fact]
        public async Task IncludesProjectsThatHaveBeenFilteredOut() {
            IEnumerable<IHierarchyNode> hierarchy;
            FilterDialogViewModel vm;


            hierarchy = [
                CreateNode(name: "a", children: [
                    CreateNode("b"),
                    CreateNode("c", children: [
                        CreateNode("d")
                    ])
                ]),
                CreateNode(name: "e", children: [
                    CreateNode("f"),
                    CreateNode("g", children: [
                        CreateNode("h")
                    ])
                ])
            ];

            vm = CreateViewModel(hierarchy);
            await vm.OnLoadedAsync(SearchHost);

            GetItem(vm, "b").IsChecked = false;
            GetItem(vm, "d").IsChecked = true;
            GetItem(vm, "f").IsChecked = true;
            GetItem(vm, "h").IsChecked = false;

            Assert.Equal(0, (await SearchAsync("d")).ErrorCode);

            // Verify that our test is setup by confirming that filtering has occurred.
            Assert.Equal(["a"], vm.Items.Select((x) => x.Name));

            vm.AcceptCommand.Execute(null);

            Assert.NotNull(vm.Result);

            Assert.Equal(
                [GetItem(vm, "d").Identifier, GetItem(vm, "f").Identifier],
                vm.Result!.ProjectsToLoad
            );

            Assert.Equal(
                [GetItem(vm, "b").Identifier, GetItem(vm, "h").Identifier],
                vm.Result!.ProjectsToUnload
            );
        }

    }


    public class Searching : TestBase {

        public Searching(GlobalServiceProvider serviceProvider) : base(serviceProvider) { }


        [Fact]
        public async Task FiltersTheItemsByTheSearchText() {
            IEnumerable<IHierarchyNode> hierarchy;
            FilterDialogViewModel vm;


            hierarchy = [
                CreateNode(name: "a", children: [
                    CreateNode("b"),
                    CreateNode("c")
                ]),
                CreateNode(name: "d", children: [
                    CreateNode("e"),
                    CreateNode("f")
                ])
            ];

            vm = CreateViewModel(hierarchy);
            await vm.OnLoadedAsync(SearchHost);

            await SearchAsync("a");

            Assert.Equal(["a"], vm.Items.Select((x) => x.Name));
        }


        [Fact]
        public async Task ClearsFilterWhenSearchIsCleared() {
            IEnumerable<IHierarchyNode> hierarchy;
            FilterDialogViewModel vm;


            hierarchy = [
                CreateNode(name: "a", children: [
                    CreateNode("b"),
                    CreateNode("c")
                ]),
                CreateNode(name: "d", children: [
                    CreateNode("e"),
                    CreateNode("f")
                ])
            ];

            vm = CreateViewModel(hierarchy);
            await vm.OnLoadedAsync(SearchHost);

            await SearchAsync("d");
            Assert.Equal(["d"], vm.Items.Select((x) => x.Name));

            SearchHost.SearchObject.ClearSearch();

            Assert.Equal(["a", "d"], vm.Items.Select((x) => x.Name));
        }


        [Fact]
        public async Task IndicatesErrorAndNoResultsWhenExceptionIsThrownByFilterFactory() {
            TextFilterFactory factory;
            FilterDialogViewModel vm;


            factory = (_, _) => throw new ArgumentException("Boom");

            vm = CreateViewModel([], textFilterFactory: factory);
            await vm.OnLoadedAsync(SearchHost);

            Assert.Equal((VSConstants.E_FAIL, 0), await SearchAsync("a"));
        }


        [Fact]
        public async Task IndicatesNoErrorAndResultsWhenFilterFactoryRunsSuccessfully() {
            TextFilterFactory factory;
            FilterDialogViewModel vm;


            factory = (_, _) => Factory.CreateTextFilter("", true);

            vm = CreateViewModel([], textFilterFactory: factory);
            await vm.OnLoadedAsync(SearchHost);

            Assert.Equal((0, 1), await SearchAsync("a"));
        }


        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UsesRegularExpressionWhenOptionIsEnabled(bool useRegularExpressions) {
            TextFilterFactory factory;
            FilterDialogViewModel vm;
            string? capturedText;
            bool? capturedUseRegularExpressions;


            capturedText = null;
            capturedUseRegularExpressions = null;


            factory = (t, r) => {
                capturedText = t;
                capturedUseRegularExpressions = r;
                return Factory.CreateTextFilter("", true);
            };

            vm = CreateViewModel([], textFilterFactory: factory);
            await vm.OnLoadedAsync(SearchHost);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(
                TestContext.Current.CancellationToken
            );

            vm.UseRegularExpressions = useRegularExpressions;

            Assert.Equal((0, 1), await SearchAsync("a"));

            Assert.Equal("a", capturedText);
            Assert.Equal(useRegularExpressions, capturedUseRegularExpressions);
        }

    }


    public abstract class TestBase : ServiceTest<object> {

        [SuppressMessage("Reliability", "VSSDK005:Avoid instantiating JoinableTaskContext", Justification = "Unit tests.")]
        protected TestBase(GlobalServiceProvider serviceProvider) : base(serviceProvider) {
            IVsWindowSearch? search;


            search = null;

            SearchHost = Substitute.For<IVsWindowSearchHost>();

            SearchHost
                .When((x) => x.SetupSearch(Arg.Any<IVsWindowSearch>()))
                .Do((info) => search = info.ArgAt<IVsWindowSearch>(0));

            SearchHost.SearchObject.Returns((_) => search);
        }


        protected static FilterDialogViewModel CreateViewModel(IEnumerable<IHierarchyNode> hierarchy, TextFilterFactory? textFilterFactory = null, Dictionary<string, SolutionNodeSettings>? nodeSettings = null) {
            return CreateViewModel(() => Task.FromResult(hierarchy), textFilterFactory, nodeSettings);
        }


        protected static FilterDialogViewModel CreateViewModel(Func<Task<IEnumerable<IHierarchyNode>>> hierarchyFactory, TextFilterFactory? textFilterFactory = null, Dictionary<string, SolutionNodeSettings>? nodeSettings = null) {
            return new FilterDialogViewModel(
                hierarchyFactory,
                textFilterFactory ?? Factory.CreateTextFilter,
                nodeSettings
            );
        }


        protected IVsWindowSearchHost SearchHost { get; }


        protected async Task<(int ErrorCode, int ResultCount)> SearchAsync(string text) {
            IVsSearchCallback callback;
            IVsSearchTask task;
            int resultCount;


            resultCount = -1;

            callback = Substitute.For<IVsSearchCallback>();
            callback
                .When((x) => x.ReportComplete(Arg.Any<IVsSearchTask>(), Arg.Any<uint>()))
                .Do((info) => resultCount = (int)info.ArgAt<uint>(1));

            SearchHost.SearchQuery.SearchString.Returns(text);
            task = SearchHost.SearchObject.CreateSearch(1, SearchHost.SearchQuery, callback);

            Assert.Equal((uint)VSConstants.VsSearchTaskStatus.Created, task.Status);

            task.Start();

            for (int i = 0; i < 10; i++) {
                if (task.Status == (uint)VSConstants.VsSearchTaskStatus.Completed) {
                    break;
                }

                await Task.Delay(100);
            }

            Assert.Equal((uint)VSConstants.VsSearchTaskStatus.Completed, task.Status);

            return (task.ErrorCode, resultCount);
        }

    }


    private static IHierarchyNode CreateNode(string name, bool isLoaded = true, IEnumerable<IHierarchyNode>? children = null) {
        IHierarchyNode node;


        children ??= [];

        node = Substitute.For<IHierarchyNode>();
        node.Name.Returns(name);
        node.Identifier.Returns(Guid.NewGuid());
        node.IsLoaded.Returns(isLoaded);
        node.Children.Returns([.. children]);
        node.IsFolder.Returns(children.Any());

        return node;
    }


    private static HierarchyTreeViewItem GetItem(FilterDialogViewModel viewModel, string name) {
        return viewModel.Items.GetFullHierarchy().First((x) => x.Name == name);
    }

}
