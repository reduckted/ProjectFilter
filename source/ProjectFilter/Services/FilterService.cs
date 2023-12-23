using Community.VisualStudio.Toolkit;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ProjectFilter.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services;


public partial class FilterService : IFilterService {

    public async Task ApplyAsync(FilterOptions options) {
        ThreadedWaitDialogProgressData progress;
        IWaitDialogFactory waitDialogFactory;


        if (options is null) {
            throw new ArgumentNullException(nameof(options));
        }

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        if ((options.ProjectsToLoad.Count == 0) && (options.ProjectsToUnload.Count == 0)) {
            return;
        }

        waitDialogFactory = await VS.GetRequiredServiceAsync<IWaitDialogFactory, IWaitDialogFactory>();
        progress = new ThreadedWaitDialogProgressData("Filtering projects...", "Getting ready...", "", true);

        using (var dialog = await waitDialogFactory.CreateAsync(Vsix.Name, progress)) {
            IEnumerable<Guid> projectsToUnload;
            IEnumerable<Guid> projectsToLoad;
            State state;
            ISolutionExplorer solutionExplorer;
            IEnumerable<Guid> expandedFolders;


            state = new State(
                dialog,
                progress,
                (IVsSolution4)await VS.Services.GetSolutionAsync()
            );

            solutionExplorer = await VS.GetRequiredServiceAsync<ISolutionExplorer, ISolutionExplorer>();

            // If we are not going to expand the loaded projects, then we will need to collapse
            // them instead, because Visual Studio seems to automatically expand them and
            // the folders they are in. To work out what should be collapsed after the
            // projects are loaded, we first need to know what was originally expanded.
            if (!options.ExpandLoadedProjects) {
                expandedFolders = await solutionExplorer.GetExpandedFoldersAsync();
            } else {
                expandedFolders = new HashSet<Guid>();
            }

            projectsToUnload = options.ProjectsToUnload.ToList();
            projectsToLoad = options.ProjectsToLoad.ToList();

            // Work out which projects actually need to be unloaded
            // so that we can calculate an accurate progress. If a
            // project is already unloaded, then we can skip it.
            state.AddProjectsToUnload(projectsToUnload.Where((x) => IsLoaded(state.Solution, x)));

            // Do the same for the projects that we need to load. We may add
            // to this list as we load projects and find their dependencies,
            // but we need to start with a known list so that we can give a
            // reasonable initial estimation for the progress. Start with the
            // projects that we were asked to load that are not already loaded.
            state.AddProjectsToLoad(projectsToLoad.Where((x) => !IsLoaded(state.Solution, x)));

            // If we're loading dependencies, then we can add the unloaded
            // dependencies of the loaded projects that were requested to be loaded.
            if (options.LoadProjectDependencies) {
                foreach (var identifier in projectsToLoad.Where((x) => IsLoaded(state.Solution, x))) {
                    foreach (var dependency in await GetProjectDependenciesAsync(identifier, state)) {
                        if (!IsLoaded(state.Solution, dependency)) {
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

            // Let Visual Studio catch up with the changes we made before we do
            // anything else. If we don't do this, then Solution Explorer can
            // end up hiding projects that we just loaded (though, that only seems
            // to be a problem if you've loaded a Solution Filter ¯\_(ツ)_/¯).
            await Task.Yield();

            // Even if we've been cancelled, we'll still hide the unloaded
            // projects and show the loaded projects. This prevents us from
            // getting into a state where we've loaded some projects but they
            // remain hidden because the user cancelled half way through.
            await solutionExplorer.HideUnloadedProjectsAsync();

            // Expand the projects if we are supposed to. For some reason, Visual Studio seems to expand
            // the projects anyway, so if we are not supposed to expand them, then we will collapse them.
            if (options.ExpandLoadedProjects) {
                await solutionExplorer.ExpandAsync(state.GetLoadedProjects());
            } else {
                await solutionExplorer.CollapseAsync(await GetProjectsToCollapseAsync(solutionExplorer, expandedFolders, state));
            }
        }
    }


    private static async Task UnloadProjectAsync(Guid identifier, State state) {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (IsLoaded(state.Solution, identifier)) {
            string name;
            int result;


            name = state.Solution.GetName(identifier);
            state.SetProgressText($"Unloading {name}...");

            result = state.Solution.UnloadProject(
                identifier,
                (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser
            );

            if (ErrorHandler.Failed(result)) {
                await LogFailureAsync($"Failed to unload project '{name}'.", result);
            }

            state.OnProjectUnloaded(identifier);
        }
    }


    private static async Task LoadProjectAsync(Guid identifier, bool loadProjectDependencies, State state) {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        // Record that we've visited this project. We may have already visited
        // it as a dependency of another project, or maybe we're visiting
        // it this time because it's a dependency. Either way, if we've
        // visited it before, then we don't need to do anything this time.
        if (state.ProjectsVisitedWhileLoading.Add(identifier)) {
            IEnumerable<Guid>? dependencies;
            bool loaded;


            dependencies = null;
            loaded = false;

            if (!IsLoaded(state.Solution, identifier)) {
                string name;
                int result;


                name = state.Solution.GetName(identifier);
                state.SetProgressText($"Loading {name}...");
                result = state.Solution.ReloadProject(identifier);

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
                state.AddProjectsToLoad(dependencies.Where((x) => !IsLoaded(state.Solution, x)));
            }

            // Now that we've recorded the dependencies as needing to be loaded,
            // if we actually loaded the given project, then we can record that
            // it has been loaded. This prevents the progress from increasing
            // and then decreasing (instead, it will decrease and then increase).
            if (loaded) {
                state.OnProjectLoaded(identifier);
            }

            if (dependencies is not null) {
                foreach (var dependency in dependencies) {
                    if (state.IsCancellationRequested) {
                        break;
                    }

                    await LoadProjectAsync(dependency, true, state);
                }
            }
        }
    }


    private static async Task<IEnumerable<Guid>> GetProjectDependenciesAsync(Guid identifier, State state) {
        List<Guid> output;
        IVsSolutionBuildManager2 solutionBuildManager;


        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        solutionBuildManager = (IVsSolutionBuildManager2)await VS.Services.GetSolutionBuildManagerAsync();

        output = new List<Guid>();

        if (state.Solution.TryGetHierarchy(identifier, out IVsHierarchy hierarchy)) {
            IVsHierarchy[] dependencies;
            uint[] size;
            int result;


            // Make sure we calculate the project dependencies
            // before we fetch them if we haven't already done so.
            if (state.RequiresProjectDependencyCalculation) {
                result = solutionBuildManager.CalculateProjectDependencies();

                if (ErrorHandler.Failed(result)) {
                    await LogFailureAsync("Failed to calculate project dependencies.", result);
                }

                state.RequiresProjectDependencyCalculation = false;
            }

            dependencies = Array.Empty<IVsHierarchy>();

            // First we need to ask for the dependencies without specifying an
            // array so that we can find out how many dependencies there are.
            size = new uint[1];
            result = solutionBuildManager.GetProjectDependencies(hierarchy, 0, null, size);

            if (ErrorHandler.Succeeded(result) && size[0] > 0) {
                // Now we know how many dependencies there are, we can
                // create an array of the correct size and ask again.
                dependencies = new IVsHierarchy[size[0]];
                result = solutionBuildManager.GetProjectDependencies(hierarchy, size[0], dependencies);
            }

            if (ErrorHandler.Succeeded(result)) {
                foreach (var dependency in dependencies) {
                    if (state.Solution.TryGetIdentifier(dependency, out Guid dependencyIdentifier)) {
                        output.Add(dependencyIdentifier);
                    }
                }

            } else {
                await LogFailureAsync($"Failed to get project dependencies for '{state.Solution.GetName(identifier)}'.", result);
            }

            // Shared projects are not considered project dependencies,
            // but we still want to load them as though they are dependencies.
            output.AddRange(GetSharedProjectDependencies(hierarchy, state));
        }

        return output;
    }


    private static IEnumerable<Guid> GetSharedProjectDependencies(IVsHierarchy hierarchy, State state) {
        string[] projitemsPaths;


        ThreadHelper.ThrowIfNotOnUIThread();

        projitemsPaths = SharedProjectUtilities.GetSharedItemsImportFullPaths(hierarchy);

        if ((projitemsPaths is not null) && (projitemsPaths.Length > 0)) {
            HashSet<string> shprojPaths;


            // The hierarchy of the shared project is available as a property against the `IVsHierarchy` of
            // the item that represents the `.projitems` file. However, that property is only populated if the 
            // shared project is already loaded. We need to find shared projects that are both loaded and unloaded,
            // so we can't use that property. Instead, we'll look through all of the projects in the solution and
            // check if the file name is the `.shproj` file that's next to the corresponding `.projitems` file.
            shprojPaths = projitemsPaths.Select((x) => Path.ChangeExtension(x, ".shproj")).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var item in ((IVsSolution)state.Solution).GetAllProjectHierarchies(ProjectStateFilter.All)) {
                if (ErrorHandler.Succeeded(item.GetCanonicalName(VSConstants.VSITEMID_ROOT, out string fileName))) {
                    if (shprojPaths.Contains(fileName)) {
                        if (state.Solution.TryGetIdentifier(item, out Guid identifier)) {
                            yield return identifier;
                        }
                    }
                }
            }
        }
    }


    private static bool IsLoaded(IVsSolution4 solution, Guid identifier) {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (solution.TryGetHierarchy(identifier, out IVsHierarchy hierarchy)) {
            return !HierarchyUtilities.IsStubHierarchy(hierarchy);
        } else {
            return false;
        }
    }


    private static async Task<IEnumerable<Guid>> GetProjectsToCollapseAsync(ISolutionExplorer solutionExplorer, IEnumerable<Guid> originalExpandedFolders, State state) {
        HashSet<Guid> projects;


        // All of the projects that were loaded should be collapsed.
        projects = state.GetLoadedProjects().ToHashSet();

        // Any solution folders that are now expanded
        // and were not originally expanded should also be collapsed.
        projects.UnionWith((await solutionExplorer.GetExpandedFoldersAsync()).Except(originalExpandedFolders));

        return projects;
    }


    private static async Task LogFailureAsync(string message, int result) {
        ILogger logger;


        logger = await VS.GetRequiredServiceAsync<ILogger, ILogger>();

        await logger.WriteLineAsync($"{message} {Marshal.GetExceptionForHR(result).Message}");
    }

}
