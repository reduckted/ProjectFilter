using Community.VisualStudio.Toolkit;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ProjectFilter.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services;


public class SolutionExplorer : ISolutionExplorer {

    private static readonly CommandID HideUnloadedProjectsCommand = new(VSConstants.CMDSETID.StandardCommandSet15_guid, 1654);
    private static readonly CommandID ShowUnloadedProjectsCommand = new(VSConstants.CMDSETID.StandardCommandSet15_guid, 1653);
    private static readonly CommandID UnhideFoldersCommand = KnownCommands.Project_UnhideFolders;
    private static readonly CommandID HideSolutionFolderCommand = new(VSConstants.CMDSETID.StandardCommandSet2K_guid, 1608);


    public async Task<bool?> IsEmptyAsync() {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        SolutionExplorerWindow? solutionExplorer;


        solutionExplorer = await VS.Windows.GetSolutionExplorerWindowAsync();

        if (solutionExplorer is not null) {
            PropertyInfo navigatorProperty;


            // The Solution Explorer window doesn't directly expose whether
            // it contains any items, but the underlying window should be a
            // `HierarchyNavigatorPaneBase<T>`, which is an internal type that
            // we can use to determine whether the Solution Explorer has been populated.
            navigatorProperty = solutionExplorer.UIHierarchyWindow.GetType().GetProperty("Navigator");

            if (navigatorProperty?.GetValue(solutionExplorer.UIHierarchyWindow) is PivotNavigator navigator) {
                return navigator.FirstEntry is null;
            }
        }

        return null;
    }


    public async Task HideUnloadedProjectsAsync() {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        Solution? solution;


        solution = await VS.Solutions.GetCurrentSolutionAsync();

        if (solution is not null) {
            solution.GetItemInfo(out IVsHierarchy hierarchy, out uint itemID, out var _);

            if (hierarchy is IVsUIHierarchy uiHierarchy) {
                SolutionExplorerWindow? solutionExplorer;


                solutionExplorer = await VS.Windows.GetSolutionExplorerWindowAsync();

                if (solutionExplorer is not null) {
                    IEnumerable<SolutionItem> selection;
                    WindowFrame? window;


                    // We need to execute some commands that operate on the solution,
                    // which means we need to select the solution hierarchy first.
                    // Remember the current selection, select the solution,
                    // execute the command and then restore the original selection.
                    selection = await solutionExplorer.GetSelectionAsync();
                    solutionExplorer.SetSelection(solution);

                    // The commands that we need to execute may only work when
                    // Solution Explorer has the focus. Play it safe and remember
                    // the current window, then focus Solution Explorer.
                    window = await VS.Windows.GetCurrentWindowAsync();
                    solutionExplorer.Frame.Show();

                    // The "Hide unloaded projects" command doesn't simply recalculate which
                    // projects should be visible. It only hides visible projects that are
                    // now unloaded. Any hidden projects that are now loaded remain hidden.
                    // In addition to this, running the "Show unloaded projects" command
                    // first isn't enough, because Solution Explorer gets into a state where
                    // some projects remain hidden, even though the folders they are in become
                    // visible. To fix this, we also need to run the "Unhide folders" command.

                    // Show the unloaded projects so that all
                    // projects are supposed to be visible.
                    ExecuteCommand(uiHierarchy, itemID, ShowUnloadedProjectsCommand);

                    // Now run the "Unhide folders" command to make
                    // sure the projects that were just made visible
                    // from the previous command are actually shown.
                    ExecuteCommand(uiHierarchy, itemID, UnhideFoldersCommand);

                    // Now we're back in a state where everything in Solution Explorer
                    // is visible, so we can hide the unloaded projects.
                    ExecuteCommand(uiHierarchy, itemID, HideUnloadedProjectsCommand);

                    // Restore the original selection.
                    solutionExplorer.SetSelection(selection);

                    // Restore the previously-active window.
                    if (window is not null) {
                        await window.ShowAsync();
                    }
                }
            }
        }
    }


    public async Task HideSolutionFoldersAsync(
        bool hideSolutionFoldersWithoutLoadedProjects,
        bool hideSolutionFoldersWithOnlyFiles
    ) {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (!hideSolutionFoldersWithoutLoadedProjects && !hideSolutionFoldersWithOnlyFiles) {
            return;
        }

        SolutionExplorerWindow? solutionExplorer;


        solutionExplorer = await VS.Windows.GetSolutionExplorerWindowAsync();

        if (solutionExplorer is not null) {
            IVsSolution solution;
            IHierarchyProvider provider;
            IEnumerable<SolutionItem> selection;
            IEnumerable<IHierarchyNode> nodes;
            List<IHierarchyNode> foldersToHide;
            WindowFrame? window;
            bool focused;


            solution = await VS.Services.GetSolutionAsync();
            provider = await VS.GetRequiredServiceAsync<IHierarchyProvider, IHierarchyProvider>();

            // We need to execute some commands that operate on the solution folders,
            // which means we may need to change the selected item solution explorer.
            // Remember the current selection so that we can restore it.
            selection = await solutionExplorer.GetSelectionAsync();

            // The hierarchy we can retrieve only includes projects and not files. We can choose to
            // get the hierarchy with or without empty folders. An empty folder in this scenario is
            // a folder without any projects (either loaded or unloaded). We'll get the hierarchy
            // with the empty folders (don't omit empty folders) so that we are able to include
            // folders with only files (they would be empty folders) in the folders to hide.
            nodes = await provider.GetHierarchyAsync(false);
            foldersToHide = GetSolutionFoldersToHide(
                nodes,
                hideSolutionFoldersWithoutLoadedProjects,
                hideSolutionFoldersWithOnlyFiles
            );

            window = null;
            focused = false;

            foreach (IHierarchyNode node in foldersToHide) {
                if (ErrorHandler.Succeeded(solution.GetProjectOfGuid(node.Identifier, out IVsHierarchy hierarchy))) {
                    SolutionItem? folder;


                    folder = await SolutionItem.FromHierarchyAsync(hierarchy, VSConstants.VSITEMID_ROOT);

                    if (folder is not null) {
                        folder.GetItemInfo(out IVsHierarchy vsHierarchy, out uint itemID, out var _);

                        if (vsHierarchy is IVsUIHierarchy uiHierarchy) {
                            // The commands that we need to execute may only work when
                            // Solution Explorer has the focus. Play it safe and remember
                            // the current window, then focus Solution Explorer.
                            if (!focused) {
                                window = await VS.Windows.GetCurrentWindowAsync();
                                solutionExplorer.Frame.Show();
                                focused = true;
                            }

                            solutionExplorer.SetSelection(folder);
                            ExecuteCommand(uiHierarchy, itemID, HideSolutionFolderCommand);
                        }
                    }
                }
            }

            // Restore the original selection.
            solutionExplorer.SetSelection(selection);

            // Restore the previously-active window.
            if (window is not null) {
                await window.ShowAsync();
            }
        }
    }


    public static List<IHierarchyNode> GetSolutionFoldersToHide(
        IEnumerable<IHierarchyNode> nodes,
        bool hideSolutionFoldersWithoutLoadedProjects,
        bool hideSolutionFoldersWithOnlyFiles
    ) {
        List<IHierarchyNode> folders;


        folders = [];

        foreach (IHierarchyNode child in nodes) {
            if (child.IsFolder) {
                Visit(child);
            }
        }

        return folders;

        bool Visit(IHierarchyNode folder) {
            if (folder.Children.Count == 0) {
                // An empty folder has no projects, so we'll assume that it must therefore
                // contain files which are not included in the hierarchy that we traverse.
                if (hideSolutionFoldersWithOnlyFiles) {
                    folders.Add(folder);
                    return true;
                } else {
                    return false;
                }

            } else {
                bool canHide = true;
                bool hasLoadedProjects = false;

                foreach (IHierarchyNode child in folder.Children) {
                    if (child.IsFolder) {
                        if (!Visit(child)) {
                            // We can't hide this child folder, which means we
                            // also can't hide the folder we are visiting.
                            canHide = false;
                        }

                    } else {
                        // The node is not a folder, so it must be a project.
                        // If the project is loaded, then we can't hide the folder.
                        if (child.IsLoaded) {
                            canHide = false;
                            hasLoadedProjects = true;
                        }
                    }
                }

                // If the folder does not contain any loaded projects, but we
                // are not allowed to hide folders without loaded projects,
                // then we can't actually hide this folder. We can only determine
                // this once we have looked at all children in the given folder.
                if (canHide && !hasLoadedProjects && !hideSolutionFoldersWithoutLoadedProjects) {
                    canHide = false;
                }

                if (canHide) {
                    folders.Add(folder);
                }

                return canHide;
            }
        }
    }


    private static void ExecuteCommand(IVsUIHierarchy hierarchy, uint itemID, CommandID command) {
        ThreadHelper.ThrowIfNotOnUIThread();

        hierarchy.ExecCommand(
            itemID,
            command.Guid,
            (uint)command.ID,
            (uint)OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER,
            IntPtr.Zero,
            IntPtr.Zero
        );
    }


    public Task ExpandAsync(IEnumerable<Guid> projects) {
        return ExpandOrCollapseAsync(projects, (window, items) => window.Expand(items));
    }


    public Task CollapseAsync(IEnumerable<Guid> projects) {
        return ExpandOrCollapseAsync(projects, (window, items) => window.Collapse(items));
    }


    private static async Task ExpandOrCollapseAsync(IEnumerable<Guid> projects, Action<SolutionExplorerWindow, IEnumerable<SolutionItem>> action) {
        if (projects is null) {
            throw new ArgumentNullException(nameof(projects));
        }

        SolutionExplorerWindow? solutionExplorer;


        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        solutionExplorer = await VS.Windows.GetSolutionExplorerWindowAsync();

        if (solutionExplorer is not null) {
            IVsSolution4 solution;
            List<SolutionItem> items;


            solution = (IVsSolution4)await VS.Services.GetSolutionAsync();
            items = [];

            foreach (var project in projects) {
                if (solution.TryGetHierarchy(project, out IVsHierarchy hierarchy)) {
                    SolutionItem? item;


                    item = await SolutionItem.FromHierarchyAsync(hierarchy, VSConstants.VSITEMID_ROOT);

                    if (item is not null) {
                        items.Add(item);
                    }
                }
            }

            if (items.Count > 0) {
                action(solutionExplorer, items);
            }
        }
    }


    public async Task<IEnumerable<Guid>> GetExpandedFoldersAsync() {
        HashSet<Guid> expanded;
        SolutionExplorerWindow? solutionExplorer;
        IVsSolution4? solution;


        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        expanded = [];

        solutionExplorer = await VS.Windows.GetSolutionExplorerWindowAsync();
        solution = await VS.Services.GetSolutionAsync() as IVsSolution4;

        if ((solutionExplorer is not null) && (solution is not null)) {
            foreach (IVsHierarchy hierarchy in await VS.Solutions.GetAllProjectHierarchiesAsync()) {
                if (await IsExpandedSolutionFolderAsync(solutionExplorer.UIHierarchyWindow, hierarchy)) {
                    if (solution.TryGetIdentifier(hierarchy, out var identifier)) {
                        expanded.Add(identifier);
                    }
                }
            }
        }

        return expanded;
    }


    private static async Task<bool> IsExpandedSolutionFolderAsync(IVsUIHierarchyWindow window, IVsHierarchy hierarchy) {
        SolutionItem? item;


        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        item = await SolutionItem.FromHierarchyAsync(hierarchy, VSConstants.VSITEMID_ROOT);

        if ((item is not null) && (item.Type == SolutionItemType.SolutionFolder)) {
            if (hierarchy is IVsUIHierarchy uiHierarchy) {
                int result;


                result = window.GetItemState(
                    uiHierarchy,
                    VSConstants.VSITEMID_ROOT,
                    (uint)__VSHIERARCHYITEMSTATE.HIS_Expanded,
                    out uint state
                );

                if (ErrorHandler.Succeeded(result)) {
                    return state == (uint)__VSHIERARCHYITEMSTATE.HIS_Expanded;
                }
            }
        }

        return false;
    }


    [DebuggerDisplay("{SolutionItem.Name,nq}")]
    private class Node : ISolutionExplorerNode {

        private List<Node>? _children;


        public Node(SolutionItem solutionItem) {
            SolutionItem = solutionItem;
        }


        public SolutionItem SolutionItem { get; }


        public bool IsFolder {
            get {
                return SolutionItem.Type == SolutionItemType.SolutionFolder;
            }
        }


        public bool IsProject {
            get {
                return SolutionItem.Type == SolutionItemType.Project;
            }
        }


        public bool IsLoaded {
            get {
                return SolutionItem is Project project && project.IsLoaded;
            }
        }


        public IReadOnlyList<ISolutionExplorerNode> Children {
            get {
                // TODO: The children do not include unloaded projects.
                return _children ??= [.. SolutionItem.Children.Where((x) => x is not null).Select(x => new Node(x!))];
            }
        }

    }

}
