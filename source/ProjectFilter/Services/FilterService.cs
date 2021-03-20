using EnvDTE;
using EnvDTE80;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services {

    public partial class FilterService : IAsyncInitializable, IFilterService {

        private const string HideUnloadedProjectsCommand = "ProjectandSolutionContextMenus.WebProjectFolder.HideUnloadedProjects";
        private const string ShowUnloadedProjectsCommand = "ProjectandSolutionContextMenus.WebProjectFolder.ShowUnloadedProjects";
        private const string UnhideFoldersCommand = "Project.UnhideFolders";


        private readonly IAsyncServiceProvider _provider;


#nullable disable
        private ILogger _logger;
        private IVsSolution4 _solution;
        private DTE2 _dte;
        private IVsSolutionBuildManager2 _solutionBuildManager;
        private IWaitDialogFactory _waitDialogFactory;
#nullable restore


        public FilterService(IAsyncServiceProvider provider) {
            _provider = provider;
        }


        public async Task InitializeAsync(CancellationToken cancellationToken) {
            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _logger = await _provider.GetServiceAsync<ILogger, ILogger>();
            _solution = await _provider.GetServiceAsync<SVsSolution, IVsSolution4>();
            _dte = await _provider.GetServiceAsync<DTE, DTE2>();
            _solutionBuildManager = await _provider.GetServiceAsync<SVsSolutionBuildManager, IVsSolutionBuildManager2>();
            _waitDialogFactory = await _provider.GetServiceAsync<IWaitDialogFactory, IWaitDialogFactory>();
        }


        public async Task ApplyAsync(FilterOptions options) {
            ThreadedWaitDialogProgressData progress;


            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if ((options.ProjectsToLoad.Count == 0) && (options.ProjectsToUnload.Count == 0)) {
                return;
            }

            progress = new ThreadedWaitDialogProgressData("Filtering projects...", "Getting ready...", "", true);

            using (var dialog = await _waitDialogFactory.CreateAsync(Vsix.Name, progress)) {
                IEnumerable<string> selectedSolutionExplorerItems;
                IEnumerable<Guid> projectsToUnload;
                IEnumerable<Guid> projectsToLoad;
                State state;


                // To hide unloaded projects and show the loaded projects, we need to
                // change the selection in Solution Explorer to allow the commands to
                // run. We'll revert the selection back to what it was after we run
                // the necessary commands, but if we get the selected items _after_
                // we've loaded or unloaded projects, it can actually give us the
                // wrong items. To avoid this, we'll get the selected items now.
                selectedSolutionExplorerItems = GetSolutionExplorerSelection();

                state = new State(dialog, progress);
                projectsToUnload = options.ProjectsToUnload.ToList();
                projectsToLoad = options.ProjectsToLoad.ToList();

                // Work out which projects actually need to be unloaded
                // so that we can calculate an accurate progress. If a
                // project is already unloaded, then we can skip it.
                state.AddProjectsToUnload(projectsToUnload.Where((x) => IsLoaded(x)));

                // Do the same for the projects that we need to load. We may add
                // to this list as we load projects and find their dependencies,
                // but we need to start with a known list so that we can give a
                // reasonable initial estimation for the progress. Start with the
                // projects that we were asked to load that are not already loaded.
                state.AddProjectsToLoad(projectsToLoad.Where((x) => !IsLoaded(x)));

                // If we're loading dependencies, then we can add the unloaded
                // dependencies of the loaded projects that were requested to be loaded.
                if (options.LoadProjectDependencies) {
                    foreach (var identifier in projectsToLoad.Where(IsLoaded)) {
                        foreach (var dependency in await GetProjectDependenciesAsync(identifier, state)) {
                            if (!IsLoaded(dependency)) {
                                state.AddProjectToLoad(dependency);
                            }
                        }
                    }
                }

                // Now we can start loading and unloading projects. We're
                // filtering projects because the user wants to keep the number
                // of projects loaded to a minimum, so start by unloading the
                // requested projects before we start loading any new projects.
                foreach (var identifier in projectsToUnload) {
                    if (state.IsCancellationRequested) {
                        break;
                    }

                    await UnloadProjectAsync(identifier, state);
                }

                foreach (var identifier in projectsToLoad) {
                    if (state.IsCancellationRequested) {
                        break;
                    }

                    await LoadProjectAsync(identifier, options.LoadProjectDependencies, state);
                }

                // Even if we've been cancelled, we'll still hide the unloaded
                // projects and show the loaded projects. This prevents us from
                // getting into a state where we've loaded some projects but they
                // remain hidden because the user cancelled half way through.
                await ShowOnlyLoadedProjectsAsync(selectedSolutionExplorerItems);

                // If a project has been loaded, then the user probably
                // wants to see it, so expand any parent folders.
                EnsureExpanded(state.GetLoadedProjects());
            }
        }


        private async Task UnloadProjectAsync(Guid identifier, State state) {
            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (IsLoaded(identifier)) {
                string name;
                int result;


                name = GetName(identifier);
                state.SetProgressText($"Unloading {name}...");

                result = _solution.UnloadProject(
                    identifier,
                    (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser
                );

                if (ErrorHandler.Failed(result)) {
                    await LogFailureAsync($"Failed to unload project '{name}'.", result);
                }

                state.OnProjectUnloaded(identifier);
            }
        }


        private async Task LoadProjectAsync(Guid identifier, bool loadProjectDependencies, State state) {
            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Record that we've visited this project. We may have already visited
            // it as a dependency of another project, or maybe we're visiting
            // it this time because it's a dependency. Either way, if we've
            // visited it before, then we don't need to do anything this time.
            if (state.ProjectsVisitedWhileLoading.Add(identifier)) {
                IEnumerable<Guid>? dependencies;
                bool loaded;


                dependencies = null;
                loaded = false;

                if (!IsLoaded(identifier)) {
                    string name;
                    int result;


                    name = GetName(identifier);
                    state.SetProgressText($"Loading {name}...");
                    result = _solution.ReloadProject(identifier);

                    if (ErrorHandler.Failed(result)) {
                        await LogFailureAsync($"Failed to load project '{name}'.", result);
                    }

                    // Don't record that we've loaded the project _yet_. If we do that
                    // now, then the current progress will increase, but we may then
                    // record that the dependencies need to be loaded, and that will
                    // cause the progress to decrease. This can be a bit ugly if this
                    // is the last project that needs to be loaded at this point, because
                    // the progress will increase to 100% and then immediately decrease.
                    // We'll wait until we've recorded that the dependencies need to be
                    // loaded before we record that this project was loaded.
                    loaded = true;

                    // Whenever a project is loaded, we should recalculate project dependencies
                    // to ensure that the new project's dependencies are correct.
                    state.RequiresProjectDependencyCalculation = true;
                }

                if (state.IsCancellationRequested) {
                    return;
                }

                if (loadProjectDependencies) {
                    // Find the dependencies of the project, regardless of whether
                    // we just loaded the project or if it was already loaded.
                    // Record all of the unloaded dependencies as projects
                    // that need to be loaded. This ensures that the progress
                    // is adjusted to account for the new projects that will
                    // be loaded before we actually start loading them.
                    dependencies = await GetProjectDependenciesAsync(identifier, state);
                    state.AddProjectsToLoad(dependencies.Where((x) => !IsLoaded(x)));
                }

                // Now that we've recorded the dependencies as needing to be loaded,
                // if we actually loaded the given project, then we can record that
                // it has been loaded. This prevents the progress from increasing
                // and then decreasing (instead, it will decrease and then increase).
                if (loaded) {
                    state.OnProjectLoaded(identifier);
                }

                if (dependencies != null) {
                    foreach (var dependency in dependencies) {
                        if (state.IsCancellationRequested) {
                            break;
                        }

                        await LoadProjectAsync(dependency, true, state);
                    }
                }
            }
        }


        private async Task<IEnumerable<Guid>> GetProjectDependenciesAsync(Guid identifier, State state) {
            List<Guid> output;


            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            output = new List<Guid>();

            if (TryGetHierarchy(identifier, out IVsHierarchy hierarchy)) {
                IVsHierarchy[] dependencies;
                uint[] size;
                int result;


                // Make sure we calculate the project dependencies
                // before we fetch them if we haven't already done so.
                if (state.RequiresProjectDependencyCalculation) {
                    result = _solutionBuildManager.CalculateProjectDependencies();

                    if (ErrorHandler.Failed(result)) {
                        await LogFailureAsync("Failed to calculate project dependencies.", result);
                    }

                    state.RequiresProjectDependencyCalculation = false;
                }

                dependencies = Array.Empty<IVsHierarchy>();

                // First we need to ask for the dependencies without specifying an
                // array so that we can find out how many dependencies there are.
                size = new uint[1];
                result = _solutionBuildManager.GetProjectDependencies(hierarchy, 0, null, size);

                if (ErrorHandler.Succeeded(result) && size[0] > 0) {
                    // Now we know how many dependencies there are, we can
                    // create an array of the correct size and ask again.
                    dependencies = new IVsHierarchy[size[0]];
                    result = _solutionBuildManager.GetProjectDependencies(hierarchy, size[0], dependencies);
                }

                if (ErrorHandler.Succeeded(result)) {
                    foreach (var dependency in dependencies) {
                        if (TryGetIdentifier(dependency, out Guid dependencyIdentifier)) {
                            output.Add(dependencyIdentifier);
                        }
                    }

                } else {
                    await LogFailureAsync($"Failed to get project dependencies for '{GetName(identifier)}'.", result);
                }
            }

            return output;
        }


        public async Task ShowOnlyLoadedProjectsAsync() {
            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await ShowOnlyLoadedProjectsAsync(GetSolutionExplorerSelection());
        }


        private async Task ShowOnlyLoadedProjectsAsync(IEnumerable<string> selectedItems) {
            Window? activeWindow;
            bool solutionExplorerVisible;


            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // The commands may not be available unless the Solution Explorer
            // window is active, so make sure it's visible, then activate it.
            activeWindow = _dte.ActiveWindow;
            solutionExplorerVisible = _dte.ToolWindows.SolutionExplorer.Parent.Visible;
            _dte.ToolWindows.SolutionExplorer.Parent.Activate();

            // The "Hide unloaded projects" command doesn't simply recalculate which
            // projects should be visible. It only hides visible projects that are
            // now unloaded. Any hidden projects that are now loaded remain hidden.
            // In addition to this, running the "Show unloaded projects" command
            // first isn't enough, because Solution Explorer gets into a state where
            // some projects remain hidden, even though the folders they are in become
            // visible. To fix this, we also need to run the "Unhide folders" command.

            // Start by selecting the solution node in Solution Explorer
            // so that the commands are definitely available.
            SetSolutionExplorerSelection(new[] { GetName((IVsHierarchy)_solution) });

            // Show the unloaded projects so that all
            // projects are supposed to be visible.
            await TryExecuteCommandAsync(ShowUnloadedProjectsCommand);

            // Now run the "Unhide folders" command to make
            // sure the projects that were just made visible
            // from the previous command are actually shown.
            await TryExecuteCommandAsync(UnhideFoldersCommand);

            // Now we're back in a state where everything in Solution Explorer
            // is visible, so we can hide the unloaded projects.
            await TryExecuteCommandAsync(HideUnloadedProjectsCommand);

            // Restore the original selection in Solution Explorer.
            SetSolutionExplorerSelection(selectedItems);

            // Activate the previously active window and
            // restore the visibility of Solution Explorer.
            activeWindow?.Activate();
            _dte.ToolWindows.SolutionExplorer.Parent.Visible = solutionExplorerVisible;
        }


        private async Task TryExecuteCommandAsync(string commandName) {
            Command? command;


            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            command = _dte.Commands.Item(commandName);

            if (command != null) {
                if (command.IsAvailable) {
                    _dte.ExecuteCommand(commandName);
                } else {
                    await _logger.WriteLineAsync($"Command '{commandName}' is not available.");
                }

            } else {
                await _logger.WriteLineAsync($"Could not find command '{commandName}'.");
            }
        }


        private IEnumerable<string> GetSolutionExplorerSelection() {
            List<string> output;
            Stack<string> segments;


            ThreadHelper.ThrowIfNotOnUIThread();

            output = new List<string>();
            segments = new Stack<string>();

            foreach (UIHierarchyItem item in (object[])_dte.ToolWindows.SolutionExplorer.SelectedItems) {
                UIHierarchyItem? node;


                segments.Clear();
                node = item;

                // Items are selected by specifying the full path down the tree,
                // so construct that path by walking up through the ancestors.
                do {
                    segments.Push(node.Name);
                    node = node.Collection?.Parent as UIHierarchyItem;
                } while (node != null);

                output.Add(string.Join("\\", segments));
            }

            return output;
        }


        private void SetSolutionExplorerSelection(IEnumerable<string> itemPaths) {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (var path in itemPaths) {
                try {
                    UIHierarchyItem? item;


                    item = _dte.ToolWindows.SolutionExplorer.GetItem(path);

                    if (item != null) {
                        item.Select(vsUISelectionType.vsUISelectionTypeSelect);

                        // There doesn't seem to be a way to programatically select multiple
                        // items, so once we successfully select one item, we can stop.
                        break;
                    }

                } catch (ArgumentException) {
                    // The item cannot be found. It may have been unloaded,
                    // so just move on and try to select the next item.
                }
            }
        }


        private void EnsureExpanded(IEnumerable<Guid> projects) {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (var project in projects) {
                try {
                    UIHierarchyItem? item = null;


                    // For some reason, getting the parent item by building a path
                    // doesn't always work. Sometimes it results in an ArgumentException.
                    // To work around that, we can step down to the parent ourselves.
                    foreach (var ancestor in GetAncestors(project).Reverse()) {
                        string name;


                        name = GetName(ancestor);

                        if (item == null) {
                            item = _dte.ToolWindows.SolutionExplorer.GetItem(name);
                        } else {
                            item = item.UIHierarchyItems.Item(name);
                        }

                        item.UIHierarchyItems.Expanded = true;
                    }

                } catch (ArgumentException) {
                    // The item doesn't exist. Ignore this
                    // project and try the next project.
                }
            }
        }


        private IEnumerable<Guid> GetAncestors(Guid identifier) {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (TryGetHierarchy(identifier, out IVsHierarchy hierarchy)) {
                while (true) {
                    int result;


                    result = hierarchy.GetProperty(
                        (uint)VSConstants.VSITEMID.Root,
                        (int)__VSHPROPID.VSHPROPID_ParentHierarchy,
                        out object value
                    );

                    if (ErrorHandler.Failed(result)) {
                        break;
                    }

                    if (value is IVsHierarchy parent && TryGetIdentifier(parent, out Guid parentIdentifier)) {
                        yield return parentIdentifier;
                        hierarchy = parent;

                    } else {
                        break;
                    }
                }
            }
        }


        private bool IsLoaded(Guid identifier) {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (TryGetHierarchy(identifier, out IVsHierarchy hierarchy)) {
                return !HierarchyUtilities.IsStubHierarchy(hierarchy);
            } else {
                return false;
            }
        }


        private string GetName(Guid identifier) {
            ThreadHelper.ThrowIfNotOnUIThread();

            TryGetHierarchy(identifier, out IVsHierarchy hierarchy);

            return GetName(hierarchy);
        }


        private static string GetName(IVsHierarchy? hierarchy) {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (hierarchy != null) {
                int result;


                result = hierarchy.GetProperty(
                    (uint)VSConstants.VSITEMID.Root,
                    (int)__VSHPROPID.VSHPROPID_Name,
                    out object value
                );

                if (ErrorHandler.Succeeded(result) && value is string name) {
                    return name;
                }
            }

            return "?";
        }


        private bool TryGetHierarchy(Guid identifier, out IVsHierarchy hierarchy) {
            ThreadHelper.ThrowIfNotOnUIThread();
            return ErrorHandler.Succeeded(((IVsSolution)_solution).GetProjectOfGuid(identifier, out hierarchy));
        }


        private bool TryGetIdentifier(IVsHierarchy hierarchy, out Guid identifier) {
            ThreadHelper.ThrowIfNotOnUIThread();
            return ErrorHandler.Succeeded(((IVsSolution)_solution).GetGuidOfProject(hierarchy, out identifier));
        }


        private async Task LogFailureAsync(string message, int result) {
            await _logger.WriteLineAsync($"{message} {Marshal.GetExceptionForHR(result).Message}");
        }

    }

}
