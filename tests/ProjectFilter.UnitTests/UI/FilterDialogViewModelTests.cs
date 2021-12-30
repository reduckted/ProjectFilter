using Microsoft.VisualStudio.Threading;
using Moq;
using ProjectFilter.Helpers;
using ProjectFilter.Services;
using ProjectFilter.UI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Xunit;


namespace ProjectFilter.UI;


public static class FilterDialogViewModelTests {

    public class LoadingVisibilityProperty : TestBase {

        [Fact]
        public async Task IsVisibleUntilHierarchyIsRetrieved() {
            TaskCompletionSource<IEnumerable<IHierarchyNode>> hierarchy;


            hierarchy = new TaskCompletionSource<IEnumerable<IHierarchyNode>>();

            using (var vm = CreateViewModel(() => hierarchy.Task)) {
                Task loaded;


                Assert.Equal(Visibility.Visible, vm.LoadingVisibility);

                loaded = vm.OnLoadedAsync();

                Assert.Equal(Visibility.Visible, vm.LoadingVisibility);

                hierarchy.SetResult(
                    new[] {
                            CreateNode("a", isLoaded: true),
                            CreateNode("b", isLoaded: false)
                    }
                );

                await loaded;

                Assert.Equal(Visibility.Collapsed, vm.LoadingVisibility);
            }
        }

    }


    public class LoadedVisibilityProperty : TestBase {

        [Fact]
        public async Task IsCollapsedUntilHierarchyIsRetrieved() {
            TaskCompletionSource<IEnumerable<IHierarchyNode>> hierarchy;


            hierarchy = new TaskCompletionSource<IEnumerable<IHierarchyNode>>();

            using (var vm = CreateViewModel(() => hierarchy.Task)) {
                Task loaded;


                Assert.Equal(Visibility.Collapsed, vm.LoadedVisibility);

                loaded = vm.OnLoadedAsync();

                Assert.Equal(Visibility.Collapsed, vm.LoadedVisibility);

                hierarchy.SetResult(
                    new[] {
                            CreateNode("a", isLoaded: true),
                            CreateNode("b", isLoaded: false)
                    }
                );

                await loaded;

                Assert.Equal(Visibility.Visible, vm.LoadedVisibility);
            }
        }

    }


    public class ItemsProperty : TestBase {

        [Fact]
        public async Task IsEmptyUntilHierarchyIsRetrieved() {
            TaskCompletionSource<IEnumerable<IHierarchyNode>> hierarchy;


            hierarchy = new TaskCompletionSource<IEnumerable<IHierarchyNode>>();

            using (var vm = CreateViewModel(() => hierarchy.Task)) {
                Task loaded;


                Assert.Empty(vm.Items);

                loaded = vm.OnLoadedAsync();

                Assert.Empty(vm.Items);

                hierarchy.SetResult(
                    new[] {
                            CreateNode("a", isLoaded: true),
                            CreateNode("b", isLoaded: false)
                    }
                );

                await loaded;

                Assert.Equal(
                    new[] {
                            ("a", (bool?)true),
                            ("b", (bool?)false)
                    },
                    vm.Items.Select((x) => (x.Name, x.IsChecked))
                );
            }
        }


        [Fact]
        public async Task InitiallyChecksItemsBasedOnWhetherTheyAreLoadedOrTheirChildrenAreChecked() {
            IEnumerable<IHierarchyNode> hierarchy;


            hierarchy = new[] {
                    CreateNode("a", isLoaded: true),
                    CreateNode("b", isLoaded: false),
                    CreateNode("c", children: new[] { CreateNode("d", isLoaded: true) }),
                    CreateNode("e", children: new[] { CreateNode("f", isLoaded: true), CreateNode("g", isLoaded: false) }),
                };

            using (var vm = CreateViewModel(hierarchy)) {
                await vm.OnLoadedAsync();

                Assert.Equal(
                    new[] {
                            ("a",true),
                            ("b",false),
                            ("c",true),
                            ("e",(bool?)null)
                    },
                    vm.Items.Select((x) => (x.Name, x.IsChecked))
                );
            }
        }

    }


    public class ToggleLoadProjectDependenciesCommandProperty : TestBase {

        [Fact]
        public async Task TogglesTheLoadProjectDependenciesProperty() {
            using (var vm = CreateViewModel(Enumerable.Empty<IHierarchyNode>())) {
                await vm.OnLoadedAsync();

                Assert.False(vm.LoadProjectDependencies);

                vm.ToggleLoadProjectDependenciesCommand.Execute(null);

                Assert.True(vm.LoadProjectDependencies);

                vm.ToggleLoadProjectDependenciesCommand.Execute(null);

                Assert.False(vm.LoadProjectDependencies);
            }
        }

    }


    public class CollapseAllCommandProperty : TestBase {

        [Fact]
        public async Task CollapsesAllItemsWhenNoParameterIsSpecified() {
            IEnumerable<IHierarchyNode> hierarchy;


            hierarchy = new[] {
                    CreateNode(name: "a", children: new [] {
                        CreateNode("b", children: new[] {
                            CreateNode("c")
                        })
                    }),
                    CreateNode(name: "d", children: new [] {
                        CreateNode("e", children: new[] {
                            CreateNode("f")
                        })
                    })
                };

            using (var vm = CreateViewModel(hierarchy)) {
                await vm.OnLoadedAsync();

                vm.CollapseAllCommand.Execute(null);
                Assert.All(vm.Items.GetFullHierarchy(), (x) => Assert.False(x.IsExpanded));
            }
        }


        [Fact]
        public async Task CollapsesSpecifiedItemAndAllOfItsDescendantsWhenItHasChildren() {
            IEnumerable<IHierarchyNode> hierarchy;


            hierarchy = new[] {
                    CreateNode(name: "a", children: new [] {
                        CreateNode("b", children: new[] {
                            CreateNode("c")
                        })
                    }),
                    CreateNode(name: "d", children: new [] {
                        CreateNode("e", children: new[] {
                            CreateNode("f")
                        })
                    })
                };

            using (var vm = CreateViewModel(hierarchy)) {
                await vm.OnLoadedAsync();

                vm.CollapseAllCommand.Execute(GetItem(vm, "b"));

                Assert.Equal(
                    new[] {
                            ("a", true),
                            ("b", false),
                            ("c", false),
                            ("d", true),
                            ("e", true),
                            ("f", true)
                    },
                    vm.Items.GetFullHierarchy().Select((x) => (x.Name, x.IsExpanded))
                );
            }
        }


        [Fact]
        public async Task CollapsesParentOfSpecifiedItemWhenSpecifiedItemHasNoChildren() {
            IEnumerable<IHierarchyNode> hierarchy;


            hierarchy = new[] {
                    CreateNode(name: "a", children: new [] {
                        CreateNode("b", children: new[] {
                            CreateNode("c")
                        })
                    }),
                    CreateNode(name: "d", children: new [] {
                        CreateNode("e", children: new[] {
                            CreateNode("f")
                        })
                    })
                };

            using (var vm = CreateViewModel(hierarchy)) {
                await vm.OnLoadedAsync();

                vm.CollapseAllCommand.Execute(GetItem(vm, "f"));

                Assert.Equal(
                    new[] {
                            ("a", true),
                            ("b", true),
                            ("c", true),
                            ("d", true),
                            ("e", false),
                            ("f", false)
                    },
                    vm.Items.GetFullHierarchy().Select((x) => (x.Name, x.IsExpanded))
                );
            }
        }

    }


    public class ExpandAllCommandProperty : TestBase {

        [Fact]
        public async Task ExpandsAllItemsWhenNoParameterIsSpecified() {
            IEnumerable<IHierarchyNode> hierarchy;


            hierarchy = new[] {
                    CreateNode(name: "a", children: new [] {
                        CreateNode("b", children: new[] {
                            CreateNode("c")
                        })
                    }),
                    CreateNode(name: "d", children: new [] {
                        CreateNode("e", children: new[] {
                            CreateNode("f")
                        })
                    })
                };

            using (var vm = CreateViewModel(hierarchy)) {
                await vm.OnLoadedAsync();

                vm.CollapseAllCommand.Execute(null);
                vm.ExpandAllCommand.Execute(null);
                Assert.All(vm.Items.GetFullHierarchy(), (x) => Assert.True(x.IsExpanded));
            }
        }


        [Fact]
        public async Task ExpandsTheSpecifiedItemAndAllOfItsDescendants() {
            IEnumerable<IHierarchyNode> hierarchy;


            hierarchy = new[] {
                    CreateNode(name: "a", children: new [] {
                        CreateNode("b", children: new[] {
                            CreateNode("c")
                        })
                    }),
                    CreateNode(name: "d", children: new [] {
                        CreateNode("e", children: new[] {
                            CreateNode("f")
                        })
                    })
                };

            using (var vm = CreateViewModel(hierarchy)) {
                await vm.OnLoadedAsync();

                vm.CollapseAllCommand.Execute(null);
                vm.ExpandAllCommand.Execute(GetItem(vm, "b"));

                Assert.Equal(
                    new[] {
                            ("a", false),
                            ("b", true),
                            ("c", true),
                            ("d", false),
                            ("e", false),
                            ("f", false)
                    },
                    vm.Items.GetFullHierarchy().Select((x) => (x.Name, x.IsExpanded))
                );
            }
        }

    }


    public class CheckAllCommandProperty : TestBase {

        [Fact]
        public async Task ChecksAllItems() {
            IEnumerable<IHierarchyNode> hierarchy;


            hierarchy = new[] {
                    CreateNode(name: "a", children: new [] {
                        CreateNode("b", children: new[] {
                            CreateNode("c", isLoaded: false)
                        })
                    }),
                    CreateNode(name: "d", children: new [] {
                        CreateNode("e", children: new[] {
                            CreateNode("f", isLoaded: false)
                        })
                    })
                };

            using (var vm = CreateViewModel(hierarchy)) {
                await vm.OnLoadedAsync();

                Assert.All(vm.Items.GetFullHierarchy(), (x) => Assert.False(x.IsChecked));

                vm.CheckAllCommand.Execute(null);

                Assert.All(vm.Items.GetFullHierarchy(), (x) => Assert.True(x.IsChecked));
            }
        }

    }


    public class UncheckAllCommandProperty : TestBase {

        [Fact]
        public async Task UnchecksAllItems() {
            IEnumerable<IHierarchyNode> hierarchy;


            hierarchy = new[] {
                    CreateNode(name: "a", children: new [] {
                        CreateNode("b", children: new[] {
                            CreateNode("c", isLoaded: true)
                        })
                    }),
                    CreateNode(name: "d", children: new [] {
                        CreateNode("e", children: new[] {
                            CreateNode("f", isLoaded: true)
                        })
                    })
                };

            using (var vm = CreateViewModel(hierarchy)) {
                await vm.OnLoadedAsync();

                Assert.All(vm.Items.GetFullHierarchy(), (x) => Assert.True(x.IsChecked));

                vm.UncheckAllCommand.Execute(null);

                Assert.All(vm.Items.GetFullHierarchy(), (x) => Assert.False(x.IsChecked));
            }
        }

    }


    public class AcceptCommandProperty : TestBase {

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SetsOptionsFromProperties(bool loadDependencies) {
            IEnumerable<IHierarchyNode> hierarchy;


            hierarchy = new[] {
                    CreateNode(name: "a"),
                    CreateNode(name: "b")
                };

            using (var vm = CreateViewModel(hierarchy)) {
                await vm.OnLoadedAsync();

                vm.LoadProjectDependencies = loadDependencies;

                vm.AcceptCommand.Execute(null);

                Assert.NotNull(vm.Result);
                Assert.Equal(loadDependencies, vm.Result!.LoadProjectDependencies);
            }
        }


        [Fact]
        public async Task SetsProjectsBasedOnCheckedState() {
            IEnumerable<IHierarchyNode> hierarchy;


            hierarchy = new[] {
                    CreateNode(name: "a", children: new [] {
                        CreateNode("b"),
                        CreateNode("c", children: new[] {
                            CreateNode("d")
                        })
                    }),
                    CreateNode(name: "e", children: new [] {
                        CreateNode("f"),
                        CreateNode("g", children: new[] {
                            CreateNode("h")
                        })
                    })
                };

            using (var vm = CreateViewModel(hierarchy)) {
                await vm.OnLoadedAsync();

                GetItem(vm, "b").IsChecked = true;
                GetItem(vm, "d").IsChecked = false;
                GetItem(vm, "f").IsChecked = false;
                GetItem(vm, "h").IsChecked = true;

                vm.AcceptCommand.Execute(null);

                Assert.NotNull(vm.Result);

                Assert.Equal(
                    new[] { GetItem(vm, "b").Identifier, GetItem(vm, "h").Identifier },
                    vm.Result!.ProjectsToLoad
                );

                Assert.Equal(
                    new[] { GetItem(vm, "d").Identifier, GetItem(vm, "f").Identifier },
                    vm.Result!.ProjectsToUnload
                );
            }
        }


        [Fact]
        public async Task InculdesProjectsThatHaveBeenFilteredOut() {
            IEnumerable<IHierarchyNode> hierarchy;
            Mock<IDebouncer> debouncer;


            hierarchy = new[] {
                    CreateNode(name: "a", children: new [] {
                        CreateNode("b"),
                        CreateNode("c", children: new[] {
                            CreateNode("d")
                        })
                    }),
                    CreateNode(name: "e", children: new [] {
                        CreateNode("f"),
                        CreateNode("g", children: new[] {
                            CreateNode("h")
                        })
                    })
                };

            debouncer = new Mock<IDebouncer>();

            using (var vm = CreateViewModel(hierarchy, debouncer: debouncer.Object)) {
                await vm.OnLoadedAsync();

                GetItem(vm, "b").IsChecked = false;
                GetItem(vm, "d").IsChecked = true;
                GetItem(vm, "f").IsChecked = true;
                GetItem(vm, "h").IsChecked = false;

                vm.SearchText = "d";
                debouncer.Raise((x) => x.Stable += null, EventArgs.Empty);

                // Verify that our test is setup by confirming that filtering has occurred.
                Assert.Equal(new[] { "a" }, vm.Items.Select((x) => x.Name));

                vm.AcceptCommand.Execute(null);

                Assert.NotNull(vm.Result);

                Assert.Equal(
                    new[] { GetItem(vm, "d").Identifier, GetItem(vm, "f").Identifier },
                    vm.Result!.ProjectsToLoad
                );

                Assert.Equal(
                    new[] { GetItem(vm, "b").Identifier, GetItem(vm, "h").Identifier },
                    vm.Result!.ProjectsToUnload
                );
            }
        }

    }


    public class SearchTextProperty : TestBase {

        [Fact]
        public async Task WaitsBeforeFilteringWhenTextIsNotEmpty() {
            IEnumerable<IHierarchyNode> hierarchy;
            Mock<IDebouncer> debouncer;


            hierarchy = new[] {
                    CreateNode(name: "a", children: new [] {
                        CreateNode("b"),
                        CreateNode("c")
                    }),
                    CreateNode(name: "d", children: new [] {
                        CreateNode("e"),
                        CreateNode("f")
                    })
                };

            debouncer = new Mock<IDebouncer>();

            using (var vm = CreateViewModel(hierarchy, debouncer: debouncer.Object)) {
                await vm.OnLoadedAsync();

                debouncer.Verify((x) => x.Start(), Times.Never);

                vm.SearchText = "d";
                debouncer.Verify((x) => x.Start(), Times.Once);

                Assert.Equal(new[] { "a", "d" }, vm.Items.Select((x) => x.Name));

                vm.SearchText = "a";
                debouncer.Verify((x) => x.Start(), Times.Exactly(2));

                Assert.Equal(new[] { "a", "d" }, vm.Items.Select((x) => x.Name));

                debouncer.Raise((x) => x.Stable += null, EventArgs.Empty);

                Assert.Equal(new[] { "a" }, vm.Items.Select((x) => x.Name));
            }
        }


        [Fact]
        public async Task CancelsDebouncerWhenTextIsEmpty() {
            IEnumerable<IHierarchyNode> hierarchy;
            Mock<IDebouncer> debouncer;


            hierarchy = new[] {
                    CreateNode(name: "a", children: new [] {
                        CreateNode("b"),
                        CreateNode("c")
                    }),
                    CreateNode(name: "d", children: new [] {
                        CreateNode("e"),
                        CreateNode("f")
                    })
                };

            debouncer = new Mock<IDebouncer>();

            using (var vm = CreateViewModel(hierarchy, debouncer: debouncer.Object)) {
                await vm.OnLoadedAsync();

                debouncer.Verify((x) => x.Start(), Times.Never);

                vm.SearchText = "d";
                debouncer.Verify((x) => x.Cancel(), Times.Once);
                debouncer.Verify((x) => x.Start(), Times.Once);

                vm.SearchText = "";
                debouncer.Verify((x) => x.Cancel(), Times.Exactly(2));
                debouncer.Verify((x) => x.Start(), Times.Once);
            }
        }


        [Fact]
        public async Task ClearsFilterImmediatelyWhenTextIsEmpty() {
            IEnumerable<IHierarchyNode> hierarchy;
            Mock<IDebouncer> debouncer;


            hierarchy = new[] {
                    CreateNode(name: "a", children: new [] {
                        CreateNode("b"),
                        CreateNode("c")
                    }),
                    CreateNode(name: "d", children: new [] {
                        CreateNode("e"),
                        CreateNode("f")
                    })
                };

            debouncer = new Mock<IDebouncer>();

            using (var vm = CreateViewModel(hierarchy, debouncer: debouncer.Object)) {
                await vm.OnLoadedAsync();

                vm.SearchText = "d";
                debouncer.Raise((x) => x.Stable += null, EventArgs.Empty);

                Assert.Equal(new[] { "d" }, vm.Items.Select((x) => x.Name));

                vm.SearchText = "";
                Assert.Equal(new[] { "a", "d" }, vm.Items.Select((x) => x.Name));
            }
        }

    }


    public class InvalidFilterProperty : TestBase {

        [Fact]
        public async Task IsTrueWhenExceptionIsThrownByFilterFactory() {
            IEnumerable<IHierarchyNode> hierarchy;
            Mock<IDebouncer> debouncer;
            TextFilterFactory factory;


            hierarchy = new[] {
                CreateNode(name: "a", children: new [] {
                    CreateNode("b"),
                    CreateNode("c")
                })
            };

            debouncer = new Mock<IDebouncer>();

            factory = (_, _) => throw new ArgumentException("Boom");

            using (var vm = CreateViewModel(hierarchy, debouncer: debouncer.Object, textFilterFactory: factory)) {
                await vm.OnLoadedAsync();

                vm.SearchText = "a";
                debouncer.Raise((x) => x.Stable += null, EventArgs.Empty);
                Assert.True(vm.InvalidFilter);
            }
        }


        [Fact]
        public async Task IsFalseWhenExceptionIsNotThrownFromFilterFactory() {
            IEnumerable<IHierarchyNode> hierarchy;
            Mock<IDebouncer> debouncer;
            TextFilterFactory factory;
            bool throwError;


            hierarchy = new[] {
                CreateNode(name: "a", children: new [] {
                    CreateNode("b"),
                    CreateNode("c")
                })
            };

            debouncer = new Mock<IDebouncer>();

            throwError = false;
            factory = (_, _) => throwError ? throw new ArgumentException("Boom") : Factory.CreateTextFilter("", true);

            using (var vm = CreateViewModel(hierarchy, debouncer: debouncer.Object, textFilterFactory: factory)) {
                await vm.OnLoadedAsync();

                // Cause the property to be set to true so that
                // we can verify that it is set back to false.
                throwError = true;
                vm.SearchText = "a";
                debouncer.Raise((x) => x.Stable += null, EventArgs.Empty);
                Assert.True(vm.InvalidFilter);

                throwError = false;
                vm.SearchText = "b";
                debouncer.Raise((x) => x.Stable += null, EventArgs.Empty);
                Assert.False(vm.InvalidFilter);
            }
        }


        [Fact]
        public async Task IsFalseWhenFilterTextIsCleared() {
            IEnumerable<IHierarchyNode> hierarchy;
            Mock<IDebouncer> debouncer;
            TextFilterFactory factory;


            hierarchy = new[] {
                CreateNode(name: "a", children: new [] {
                    CreateNode("b"),
                    CreateNode("c")
                })
            };

            debouncer = new Mock<IDebouncer>();

            factory = (_, _) => throw new ArgumentException("Boom");

            using (var vm = CreateViewModel(hierarchy, debouncer: debouncer.Object, textFilterFactory: factory)) {
                await vm.OnLoadedAsync();

                // Cause the property to be set to true so that
                // we can verify that it is set back to false.
                vm.SearchText = "d";
                debouncer.Raise((x) => x.Stable += null, EventArgs.Empty);
                Assert.True(vm.InvalidFilter);

                vm.SearchText = "";
                Assert.False(vm.InvalidFilter);
            }
        }

    }


    public abstract class TestBase : IDisposable {

        private readonly JoinableTaskContext _joinableTaskContext;
        private readonly JoinableTaskFactory _joinableTaskFactory;


        protected TestBase() {
            _joinableTaskContext = new JoinableTaskContext();
            _joinableTaskFactory = new JoinableTaskFactory(_joinableTaskContext);
        }


        protected FilterDialogViewModel CreateViewModel(IEnumerable<IHierarchyNode> hierarchy, IDebouncer? debouncer = null, TextFilterFactory? textFilterFactory = null) {
            return CreateViewModel(() => Task.FromResult(hierarchy), debouncer, textFilterFactory);
        }


        protected FilterDialogViewModel CreateViewModel(Func<Task<IEnumerable<IHierarchyNode>>> hierarchyFactory, IDebouncer? debouncer = null, TextFilterFactory? textFilterFactory = null) {
            if (debouncer is null) {
                debouncer = Mock.Of<IDebouncer>();
            }


            return new FilterDialogViewModel(
                hierarchyFactory,
                (x) => debouncer,
                textFilterFactory ?? Factory.CreateTextFilter,
                _joinableTaskFactory
            );
        }


        protected virtual void Dispose(bool disposing) {
            _joinableTaskContext.Dispose();
        }


        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }


    private static IHierarchyNode CreateNode(string name, bool isLoaded = true, IEnumerable<IHierarchyNode>? children = null) {
        Mock<IHierarchyNode> node;


        if (children is null) {
            children = Enumerable.Empty<IHierarchyNode>();
        }

        node = new Mock<IHierarchyNode>();
        node.SetupGet((x) => x.Name).Returns(name);
        node.SetupGet((x) => x.Identifier).Returns(Guid.NewGuid());
        node.SetupGet((x) => x.IsLoaded).Returns(isLoaded);
        node.SetupGet((x) => x.Children).Returns(children.ToList());
        node.SetupGet((x) => x.IsFolder).Returns(children.Any());

        return node.Object;
    }


    private static HierarchyTreeViewItem GetItem(FilterDialogViewModel viewModel, string name) {
        return viewModel.Items.GetFullHierarchy().First((x) => x.Name == name);
    }

}
