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
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services;


public class SolutionExplorer : ISolutionExplorer {

    private static readonly CommandID HideUnloadedProjectsCommand = new(VSConstants.CMDSETID.StandardCommandSet15_guid, 1654);
    private static readonly CommandID ShowUnloadedProjectsCommand = new(VSConstants.CMDSETID.StandardCommandSet15_guid, 1653);
    private static readonly CommandID UnhideFoldersCommand = KnownCommands.Project_UnhideFolders;


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

                    // The commands that we need to execute only work when Solution Explorer
                    // has the focus. Remember the current window, then focus Solution Explorer.
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


    public async Task ExpandAsync(IEnumerable<Guid> projects) {
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
            items = new List<SolutionItem>();

            foreach (var project in projects) {
                if (solution.TryGetHierarchy(project, out IVsHierarchy hierarchy)) {
                    SolutionItem? item;


                    item = await SolutionItem.FromHierarchyAsync(hierarchy, VSConstants.VSITEMID_ROOT);

                    if (item is not null) {
                        items.Add(item);
                    }
                }
            }

            solutionExplorer.Expand(items, SolutionItemExpansionMode.Ancestors | SolutionItemExpansionMode.Single);
        }
    }

}
