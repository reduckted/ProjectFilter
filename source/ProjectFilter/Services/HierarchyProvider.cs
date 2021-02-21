using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Threading;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services {

    public class HierarchyProvider : IAsyncInitializable, IHierarchyProvider {

#nullable disable
        private IVsSolution _solution;
        private IVsImageService2 _imageService;
#nullable restore


        public async Task InitializeAsync(IAsyncServiceProvider provider, CancellationToken cancellationToken) {
            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _solution = await provider.GetServiceAsync<SVsSolution, IVsSolution>();
            _imageService = await provider.GetServiceAsync<SVsImageService, IVsImageService2>();
        }


        public IEnumerable<IHierarchyNode> GetHierarchy() {
            List<(HierarchyNode Hierarchy, Guid Parent)> nodes;
            Dictionary<Guid, HierarchyNode> mapping;
            List<HierarchyNode> roots;


            ThreadHelper.ThrowIfNotOnUIThread();

            nodes = new List<(HierarchyNode hierarchy, Guid parent)>();
            mapping = new Dictionary<Guid, HierarchyNode>();

            foreach (var item in GetHierarchies()) {
                HierarchyNode node;
                Guid parent;


                node = CreateNode(item.Hierarchy, item.Identifier);
                parent = GetParentIdentifier(item.Hierarchy);

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


        private IEnumerable<(IVsHierarchy Hierarchy, Guid Identifier)> GetHierarchies() {
            Guid empty;


            ThreadHelper.ThrowIfNotOnUIThread();

            empty = default;

            if (ErrorHandler.Succeeded(_solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_ALLINSOLUTION, ref empty, out IEnumHierarchies enumerator))) {
                IVsHierarchy[] hierarchy;


                hierarchy = new IVsHierarchy[1];

                while (true) {
                    if (ErrorHandler.Failed(enumerator.Next((uint)hierarchy.Length, hierarchy, out uint count))) {
                        break;
                    }

                    if (count <= 0) {
                        break;
                    }

                    if (TryGetIdentifier(hierarchy[0], out Guid identifier)) {
                        // Always ignore the "Miscellaneous Files" project
                        // because the items in it are not part of the solution.
                        if (!identifier.Equals(VSConstants.CLSID.MiscellaneousFilesProject_guid)) {
                            yield return (hierarchy[0], identifier);
                        }
                    }
                }
            }
        }


        private HierarchyNode CreateNode(IVsHierarchy hierarchy, Guid identifier) {
            ThreadHelper.ThrowIfNotOnUIThread();

            ImageMoniker collapsedIcon;
            ImageMoniker expandedIcon;
            string name;


            name = HierarchyUtilities.GetHierarchyProperty<string>(
                hierarchy,
                (uint)VSConstants.VSITEMID.Root,
                (int)__VSHPROPID.VSHPROPID_Name
            );

            collapsedIcon = _imageService.GetImageMonikerForHierarchyItem(
                hierarchy,
                (uint)VSConstants.VSITEMID.Root,
                (int)__VSHIERARCHYIMAGEASPECT.HIA_Icon
            );

            expandedIcon = _imageService.GetImageMonikerForHierarchyItem(
                hierarchy,
                (uint)VSConstants.VSITEMID.Root,
                (int)__VSHIERARCHYIMAGEASPECT.HIA_OpenFolderIcon
            );

            return new HierarchyNode(identifier, name, collapsedIcon, expandedIcon) {
                IsLoaded = !HierarchyUtilities.IsStubHierarchy(hierarchy),
                IsFolder = IsSolutionFolder(hierarchy)
            };
        }


        private static bool IsSolutionFolder(IVsHierarchy hierarchy) {
            ThreadHelper.ThrowIfNotOnUIThread();

            // This is similar to `HierarchyUtilities.IsSolutionFolder()`,
            // but that requires an `IVsHierarchyItemIdentity` which we don't have.
            if (hierarchy is IVsProject) {
                IPersist? persist;


                persist = hierarchy as IPersist;

                if (persist != null) {
                    if (ErrorHandler.Succeeded(persist.GetClassID(out Guid guid))) {
                        return guid == VSConstants.CLSID.SolutionFolderProject_guid;
                    }
                }
            }

            return false;
        }


        private Guid GetParentIdentifier(IVsHierarchy hierarchy) {
            IVsHierarchy? parentHierarchy;


            ThreadHelper.ThrowIfNotOnUIThread();

            parentHierarchy = HierarchyUtilities.GetHierarchyProperty<IVsHierarchy?>(
                hierarchy,
                (uint)VSConstants.VSITEMID.Root,
                (int)__VSHPROPID.VSHPROPID_ParentHierarchy
            );

            if (parentHierarchy != null) {
                if (TryGetIdentifier(parentHierarchy, out Guid parentIdentifier)) {
                    return parentIdentifier;
                }
            }

            return default;
        }


        private bool TryGetIdentifier(IVsHierarchy hierarchy, out Guid identifier) {
            ThreadHelper.ThrowIfNotOnUIThread();
            return ErrorHandler.Succeeded(_solution.GetGuidOfProject(hierarchy, out identifier));
        }


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

            return string.Compare(x.Name, y.Name, true);
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
