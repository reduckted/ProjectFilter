using Community.VisualStudio.Toolkit;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;


namespace ProjectFilter.Services;


public class HierarchyProvider : IHierarchyProvider {

    public async Task<IEnumerable<IHierarchyNode>> GetHierarchyAsync() {
        List<(HierarchyNode Hierarchy, Guid Parent)> nodes;
        Dictionary<Guid, HierarchyNode> mapping;
        List<HierarchyNode> roots;


        nodes = await GetNodesAsync();
        mapping = new Dictionary<Guid, HierarchyNode>();

        foreach (var item in nodes) {
            mapping[item.Hierarchy.Identifier] = item.Hierarchy;
        }

        // Sort the nodes once now so that we add them to their
        // parents in the correct order, instead of needing
        // to sort each collection of children individually.
        nodes.Sort((x, y) => CompareNodes(x.Hierarchy, y.Hierarchy));

        roots = new List<HierarchyNode>();

        // Add child nodes to the parents and find the root nodes.
        foreach (var node in nodes) {
            if (mapping.TryGetValue(node.Parent, out HierarchyNode parent)) {
                parent.ChildrenList.Add(node.Hierarchy);
            } else {
                roots.Add(node.Hierarchy);
            }
        }

        RemoveEmptyFolders(roots);

        return roots;
    }


    private static async Task<List<(HierarchyNode Hierarchy, Guid Parent)>> GetNodesAsync() {
        IVsSolution solution;
        IVsImageService2 imageService;
        IVsHierarchyItemManager hierarchyItemManager;
        List<(HierarchyNode Hierarchy, Guid Parent)> output;


        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        solution = await VS.Services.GetSolutionAsync();
        imageService = (IVsImageService2)await VS.Services.GetImageServiceAsync();
        hierarchyItemManager = await VS.GetMefServiceAsync<IVsHierarchyItemManager>();

        output = new List<(HierarchyNode Hierarchy, Guid Parent)>();

        foreach (var hierarchy in solution.GetAllProjectHierarchies(ProjectStateFilter.All)) {
            if (!TryGetIdentifier(solution, hierarchy, out Guid identifier)) {
                continue;
            }

            // Always ignore the "Miscellaneous Files" project
            // because the items in it are not part of the solution.
            if (identifier.Equals(VSConstants.CLSID.MiscellaneousFilesProject_guid)) {
                continue;
            }

            output.Add(
                (
                    CreateNode(hierarchy, identifier, imageService, hierarchyItemManager),
                    GetParentIdentifier(solution, hierarchy)
                )
            );
        }

        return output;
    }


    private static HierarchyNode CreateNode(IVsHierarchy hierarchy, Guid identifier, IVsImageService2 imageService, IVsHierarchyItemManager hierarchyItemManager) {
        ThreadHelper.ThrowIfNotOnUIThread();

        ImageMoniker collapsedIcon;
        ImageMoniker expandedIcon;
        string name;
        bool isFolder;


        name = HierarchyUtilities.GetHierarchyProperty<string>(
            hierarchy,
            VSConstants.VSITEMID_ROOT,
            (int)__VSHPROPID.VSHPROPID_Name
        );

        isFolder = HierarchyUtilities.IsSolutionFolder(
            hierarchyItemManager.GetHierarchyItem(hierarchy, VSConstants.VSITEMID_ROOT).HierarchyIdentity
        );

        collapsedIcon = imageService.GetImageMonikerForHierarchyItem(
            hierarchy,
            VSConstants.VSITEMID_ROOT,
            (int)__VSHIERARCHYIMAGEASPECT.HIA_Icon
        );

        expandedIcon = imageService.GetImageMonikerForHierarchyItem(
            hierarchy,
            VSConstants.VSITEMID_ROOT,
            (int)__VSHIERARCHYIMAGEASPECT.HIA_OpenFolderIcon
        );

        // Sometimes the icons can be blank.
        // In those cases, use some default icons.
        if (collapsedIcon.Id == 0 && collapsedIcon.Guid == default) {
            collapsedIcon = isFolder ? KnownMonikers.FolderClosed : KnownMonikers.DocumentCollection;
        }

        if (expandedIcon.Id == 0 && expandedIcon.Guid == default) {
            expandedIcon = isFolder ? KnownMonikers.FolderOpened : KnownMonikers.DocumentCollection;
        }

        return new HierarchyNode(identifier, name, collapsedIcon, expandedIcon) {
            IsLoaded = !HierarchyUtilities.IsStubHierarchy(hierarchy),
            IsFolder = isFolder
        };
    }


    private static Guid GetParentIdentifier(IVsSolution solution, IVsHierarchy hierarchy) {
        IVsHierarchy? parentHierarchy;


        ThreadHelper.ThrowIfNotOnUIThread();

        parentHierarchy = HierarchyUtilities.GetHierarchyProperty<IVsHierarchy?>(
            hierarchy,
            VSConstants.VSITEMID_ROOT,
            (int)__VSHPROPID.VSHPROPID_ParentHierarchy
        );

        if (parentHierarchy is not null) {
            if (TryGetIdentifier(solution, parentHierarchy, out Guid parentIdentifier)) {
                return parentIdentifier;
            }
        }

        return default;
    }


    private static bool TryGetIdentifier(IVsSolution solution, IVsHierarchy hierarchy, out Guid identifier) {
        ThreadHelper.ThrowIfNotOnUIThread();
        return ErrorHandler.Succeeded(solution.GetGuidOfProject(hierarchy, out identifier));
    }


    [SuppressMessage("Globalization", "CA1309:Use ordinal string comparison", Justification = "Comparison should use current culture.")]
    private static int CompareNodes(HierarchyNode x, HierarchyNode y) {
        // Put folders before projects, then sub-sort by name.
        if (x.IsFolder) {
            if (!y.IsFolder) {
                return -1;
            }
        } else {
            if (y.IsFolder) {
                return 1;
            }
        }

        return string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
    }


    private void RemoveEmptyFolders(IList<HierarchyNode> nodes) {
        for (int i = nodes.Count - 1; i >= 0; i--) {
            HierarchyNode node;


            node = nodes[i];

            RemoveEmptyFolders(node.ChildrenList);

            if (node.IsFolder && (node.Children.Count == 0)) {
                nodes.RemoveAt(i);
            }
        }
    }

}
