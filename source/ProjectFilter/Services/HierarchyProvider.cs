using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;


namespace ProjectFilter.Services {

    public class HierarchyProvider : IHierarchyProvider {

        private readonly IAsyncServiceProvider _provider;


        public HierarchyProvider(IAsyncServiceProvider provider) {
            _provider = provider;
        }


        public async Task<IEnumerable<IHierarchyNode>> GetHierarchyAsync() {
            IVsSolution solution;
            IVsImageService2 imageService;
            List<(HierarchyNode Hierarchy, Guid Parent)> nodes;
            Dictionary<Guid, HierarchyNode> mapping;
            List<HierarchyNode> roots;


            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            solution = await _provider.GetServiceAsync<SVsSolution, IVsSolution>();
            imageService = await _provider.GetServiceAsync<SVsImageService, IVsImageService2>();

            nodes = new List<(HierarchyNode hierarchy, Guid parent)>();
            mapping = new Dictionary<Guid, HierarchyNode>();

            foreach (var item in GetHierarchies(solution)) {
                HierarchyNode node;
                Guid parent;


                node = CreateNode(item.Hierarchy, item.Identifier, imageService);
                parent = GetParentIdentifier(solution, item.Hierarchy);

                nodes.Add((node, parent));
                mapping[node.Identifier] = node;
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


        private static IEnumerable<(IVsHierarchy Hierarchy, Guid Identifier)> GetHierarchies(IVsSolution solution) {
            Guid empty;


            ThreadHelper.ThrowIfNotOnUIThread();

            empty = default;

            if (ErrorHandler.Succeeded(solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_ALLINSOLUTION, ref empty, out IEnumHierarchies enumerator))) {
                IVsHierarchy[] hierarchy;


                hierarchy = new IVsHierarchy[1];

                while (true) {
                    if (ErrorHandler.Failed(enumerator.Next((uint)hierarchy.Length, hierarchy, out uint count))) {
                        break;
                    }

                    if (count <= 0) {
                        break;
                    }

                    if (TryGetIdentifier(solution, hierarchy[0], out Guid identifier)) {
                        // Always ignore the "Miscellaneous Files" project
                        // because the items in it are not part of the solution.
                        if (!identifier.Equals(VSConstants.CLSID.MiscellaneousFilesProject_guid)) {
                            yield return (hierarchy[0], identifier);
                        }
                    }
                }
            }
        }


        private static HierarchyNode CreateNode(IVsHierarchy hierarchy, Guid identifier, IVsImageService2 imageService) {
            ThreadHelper.ThrowIfNotOnUIThread();

            ImageMoniker collapsedIcon;
            ImageMoniker expandedIcon;
            string name;
            bool isFolder;


            name = HierarchyUtilities.GetHierarchyProperty<string>(
                hierarchy,
                (uint)VSConstants.VSITEMID.Root,
                (int)__VSHPROPID.VSHPROPID_Name
            );

            isFolder = IsSolutionFolder(hierarchy);

            collapsedIcon = imageService.GetImageMonikerForHierarchyItem(
                hierarchy,
                (uint)VSConstants.VSITEMID.Root,
                (int)__VSHIERARCHYIMAGEASPECT.HIA_Icon
            );

            expandedIcon = imageService.GetImageMonikerForHierarchyItem(
                hierarchy,
                (uint)VSConstants.VSITEMID.Root,
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


        private static bool IsSolutionFolder(IVsHierarchy hierarchy) {
            ThreadHelper.ThrowIfNotOnUIThread();

            // This is similar to `HierarchyUtilities.IsSolutionFolder()`,
            // but that requires an `IVsHierarchyItemIdentity` which we don't have.
            if (hierarchy is IVsProject) {
                IPersist? persist;


                persist = hierarchy as IPersist;

                if (persist is not null) {
                    if (ErrorHandler.Succeeded(persist.GetClassID(out Guid guid))) {
                        return guid == VSConstants.CLSID.SolutionFolderProject_guid;
                    }
                }
            }

            return false;
        }


        private static Guid GetParentIdentifier(IVsSolution solution, IVsHierarchy hierarchy) {
            IVsHierarchy? parentHierarchy;


            ThreadHelper.ThrowIfNotOnUIThread();

            parentHierarchy = HierarchyUtilities.GetHierarchyProperty<IVsHierarchy?>(
                hierarchy,
                (uint)VSConstants.VSITEMID.Root,
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

}
