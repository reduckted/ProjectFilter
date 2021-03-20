using Moq;
using ProjectFilter.Helpers;
using ProjectFilter.Services;
using ProjectFilter.UI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;


namespace ProjectFilter.UI {

    public static class FilterDialogViewModelTests {

        public class ItemsProperty {

            [Fact]
            public void InitiallyChecksItemsBasedOnWhetherTheyAreLoadedOrTheirChildrenAreChecked() {
                IEnumerable<IHierarchyNode> hierarchy;


                hierarchy = new[] {
                    CreateNode("a", isLoaded: true),
                    CreateNode("b", isLoaded: false),
                    CreateNode("c", children: new[] { CreateNode("d", isLoaded: true) }),
                    CreateNode("e", children: new[] { CreateNode("f", isLoaded: true), CreateNode("g", isLoaded: false) }),
                };

                using (var vm = CreateViewModel(hierarchy)) {
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


        public class ToggleLoadProjectDependenciesCommandProperty {

            [Fact]
            public void TogglesTheLoadProjectDependenciesProperty() {
                using (var vm = CreateViewModel(Enumerable.Empty<IHierarchyNode>())) {
                    Assert.False(vm.LoadProjectDependencies);

                    vm.ToggleLoadProjectDependenciesCommand.Execute(null);

                    Assert.True(vm.LoadProjectDependencies);

                    vm.ToggleLoadProjectDependenciesCommand.Execute(null);

                    Assert.False(vm.LoadProjectDependencies);
                }
            }

        }


        public class CollapseAllCommandProperty {

            [Fact]
            public void CollapsesAllItemsWhenNoParameterIsSpecified() {
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
                    vm.CollapseAllCommand.Execute(null);
                    Assert.All(vm.Items.GetFullHierarchy(), (x) => Assert.False(x.IsExpanded));
                }
            }


            [Fact]
            public void CollapsesSpecifiedItemAndAllOfItsDescendantsWhenItHasChildren() {
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
            public void CollapsesParentOfSpecifiedItemWhenSpecifiedItemHasNoChildren() {
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


        public class ExpandAllCommandProperty {

            [Fact]
            public void ExpandsAllItemsWhenNoParameterIsSpecified() {
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
                    vm.CollapseAllCommand.Execute(null);
                    vm.ExpandAllCommand.Execute(null);
                    Assert.All(vm.Items.GetFullHierarchy(), (x) => Assert.True(x.IsExpanded));
                }
            }


            [Fact]
            public void ExpandsTheSpecifiedItemAndAllOfItsDescendants() {
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


        public class CheckAllCommandProperty {

            [Fact]
            public void ChecksAllItems() {
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
                    Assert.All(vm.Items.GetFullHierarchy(), (x) => Assert.False(x.IsChecked));

                    vm.CheckAllCommand.Execute(null);

                    Assert.All(vm.Items.GetFullHierarchy(), (x) => Assert.True(x.IsChecked));
                }
            }

        }


        public class UncheckAllCommandProperty {

            [Fact]
            public void UnchecksAllItems() {
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
                    Assert.All(vm.Items.GetFullHierarchy(), (x) => Assert.True(x.IsChecked));

                    vm.UncheckAllCommand.Execute(null);

                    Assert.All(vm.Items.GetFullHierarchy(), (x) => Assert.False(x.IsChecked));
                }
            }

        }


        public class AcceptCommandProperty {

            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public void SetsOptionsFromProperties(bool loadDependencies) {
                IEnumerable<IHierarchyNode> hierarchy;


                hierarchy = new[] {
                    CreateNode(name: "a"),
                    CreateNode(name: "b")
                };

                using (var vm = CreateViewModel(hierarchy)) {
                    vm.LoadProjectDependencies = loadDependencies;

                    vm.AcceptCommand.Execute(null);

                    Assert.NotNull(vm.Result);
                    Assert.Equal(loadDependencies, vm.Result!.LoadProjectDependencies);
                }
            }


            [Fact]
            public void SetsProjectsBasedOnCheckedState() {
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
            public void InculdesProjectsThatHaveBeenFilteredOut() {
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


        public class SearchTextProperty {

            [Fact]
            public void WaitsBeforeFilteringWhenTextIsNotEmpty() {
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
            public void CancelsDebouncerWhenTextIsEmpty() {
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
            public void ClearsFilterImmediatelyWhenTextIsEmpty() {
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
                    vm.SearchText = "d";
                    debouncer.Raise((x) => x.Stable += null, EventArgs.Empty);

                    Assert.Equal(new[] { "d" }, vm.Items.Select((x) => x.Name));

                    vm.SearchText = "";
                    Assert.Equal(new[] { "a", "d" }, vm.Items.Select((x) => x.Name));
                }
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


        private static FilterDialogViewModel CreateViewModel(IEnumerable<IHierarchyNode> hierarchy, IDebouncer? debouncer = null) {
            if (debouncer is null) {
                debouncer = Mock.Of<IDebouncer>();
            }

            return new FilterDialogViewModel(hierarchy, (x) => debouncer, Factory.CreateSearchQuery);
        }

    }

}
