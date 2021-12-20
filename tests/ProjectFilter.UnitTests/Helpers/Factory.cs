using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using ProjectFilter.Services;
using ProjectFilter.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


namespace ProjectFilter.Helpers;


internal static class Factory {

    public static IVsSearchQuery CreateSearchQuery(string text) {
        Mock<IVsSearchQuery> query;
        IVsSearchToken[] tokens;


        tokens = text.Split(' ').Select((segment) => {
            Mock<IVsSearchToken> token;


            token = new Mock<IVsSearchToken>();
            token.SetupGet((x) => x.OriginalTokenText).Returns(segment);
            token.SetupGet((x) => x.ParsedTokenText).Returns(segment);
            token.SetupGet((x) => x.TokenStartPosition).Returns((uint)text.IndexOf(segment, StringComparison.Ordinal));

            return token.Object;
        }).ToArray();

        query = new Mock<IVsSearchQuery>();
        query.SetupGet((x) => x.SearchString).Returns(text);

        query
            .Setup((x) => x.GetTokens(It.IsAny<uint>(), It.IsAny<IVsSearchToken[]>()))
            .Returns((uint max, IVsSearchToken[] output) => {
                if (output is null) {
                    return (uint)tokens.Length;

                } else {
                    uint num;


                    num = Math.Min(max, (uint)tokens.Length);
                    Array.Copy(tokens, output, (int)num);

                    return num;
                }
            });

        return query.Object;
    }


    public static HierarchySearchMatchEvaluator CreateSearchEvaluator(string text) {
        return new HierarchySearchMatchEvaluator(CreateSearchQuery(text));
    }


    public static HierarchyTreeViewItem CreateTreeViewItem(string name = "", IEnumerable<HierarchyTreeViewItem>? children = null, bool? isChecked = false) {
        Mock<IHierarchyNode> node;


        if (children is null) {
            children = Enumerable.Empty<HierarchyTreeViewItem>();
        }

        node = new Mock<IHierarchyNode>();
        node.SetupGet((x) => x.Name).Returns(name);

        return new HierarchyTreeViewItem(node.Object, children) {
            IsChecked = isChecked
        };
    }


    public static T ParseHierarchies<T>(string data, TreeItemFactory<T> factory) where T : TreeItem {
        return ParseHierarchyElement(XDocument.Parse(data).Root, factory, null);
    }


    private static T ParseHierarchyElement<T>(XElement element, TreeItemFactory<T> factory, T? parent) where T : TreeItem {
        T item;


        item = factory(element, parent);

        if (parent is not null) {
            parent.Children.Add(item);
            item.Parent = parent;
        }

        foreach (var child in element.Elements()) {
            ParseHierarchyElement(child, factory, item);
        }

        return item;
    }


    public static HierarchyData CreateHierarchyData(XElement element, HierarchyData? parent = null) {
        HierarchyData data;
        HierarchyType type;


        type = ElementNameToType(element.Name.LocalName);

        data = new HierarchyData(
            GetIdentifier(element),
            element.Attribute("name")?.Value ?? "?",
            parent?.Identifier
        );

        SetType(data, type);

        return data;
    }


    private static HierarchyType ElementNameToType(string name) {
        switch (name) {
            case "solution":
                return HierarchyType.Solution;

            case "folder":
                return HierarchyType.Folder;

            case "project":
                return HierarchyType.Project;

            case "unloaded":
                return HierarchyType.UnloadedProject;

            case "misc":
                return HierarchyType.MiscellaneousFiles;

            default:
                throw new NotSupportedException($"Unsupported hierarchy type name '{name}'.");

        }
    }


    public static string TypeToElementName(HierarchyType type) {
        switch (type) {
            case HierarchyType.Solution:
                return "solution";

            case HierarchyType.Folder:
                return "folder";

            case HierarchyType.Project:
                return "project";

            case HierarchyType.UnloadedProject:
                return "unloaded";

            case HierarchyType.MiscellaneousFiles:
                return "misc";

            default:
                throw new NotSupportedException($"Unknown hierarchy type '{type}'.");
        }
    }


    private static Guid GetIdentifier(XElement element) {
        string? identifier;


        switch (element.Name.LocalName) {
            case "solution":
                return default;

            case "misc":
                return VSConstants.CLSID.MiscellaneousFilesProject_guid;

        }

        identifier = element.Attribute("guid")?.Value;

        return identifier is not null ? new Guid(identifier) : Guid.NewGuid();
    }


    private static IVsHierarchy CreateHierarchy(HierarchyData data, IVsSolution solution) {
        Mock<IVsHierarchy> hierarchy;
        object nameAsObject;


        hierarchy = new Mock<IVsHierarchy>();

        SetClassID(hierarchy, data.CLSID);

        nameAsObject = data.Name;

        hierarchy
            .Setup((x) => x.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_Name, out nameAsObject))
            .Returns(VSConstants.S_OK);

        hierarchy
            .Setup((x) => x.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ParentHierarchy, out It.Ref<object>.IsAny))
            .Returns(new GetPropertyCallback((uint itemID, int property, out object? value) => {
                Guid parentIdentifier;


                if (data.Parent is null) {
                    value = null;
                    return VSConstants.S_OK;
                }

                parentIdentifier = data.Parent.Value;

                if (ErrorHandler.Succeeded(solution.GetProjectOfGuid(ref parentIdentifier, out IVsHierarchy parent))) {
                    value = parent;
                    return VSConstants.S_OK;
                }

                value = null;
                return VSConstants.E_FAIL;
            }));

        if (data.IsProject) {
            hierarchy.As<IVsProject>();
        }

        return hierarchy.Object;
    }


    private static void SetType(TreeItem item, HierarchyType type) {
        SetType(item.Data, type);

        // Changing the type of the hierarchy
        // will cause it to be recreated.
        item.Hierarchy = null;
    }


    private static void SetType(HierarchyData data, HierarchyType type) {
        switch (type) {
            case HierarchyType.Solution:
                data.SetType(type, default, false);
                break;

            case HierarchyType.Folder:
                data.SetType(type, VSConstants.CLSID.SolutionFolderProject_guid, true);
                break;

            case HierarchyType.Project:
                data.SetType(
                    type,
                    new Guid("{9a19103f-16f7-4668-be54-9a1e7a4f7556}"),   // C# project.
                    true
                );
                break;

            case HierarchyType.UnloadedProject:
                data.SetType(type, VSConstants.CLSID.UnloadedProject_guid, false);
                break;

            case HierarchyType.MiscellaneousFiles:
                data.SetType(type, VSConstants.CLSID.MiscellaneousFilesProject_guid, true);
                break;

            default:
                throw new NotSupportedException();

        }
    }


    public static IVsSolution CreateSolution(TreeItem root) {
        Mock<IVsSolution> solution;
        IEnumHierarchies enumerator;


        solution = new Mock<IVsSolution>();

        solution
            .Setup((x) => x.GetGuidOfProject(It.IsAny<IVsHierarchy>(), out It.Ref<Guid>.IsAny))
            .Returns(new GetGuidOfProject((IVsHierarchy hierarchy, out Guid guid) => {
                foreach (var node in root.DescendantsAndSelf()) {
                    if (node.Hierarchy == hierarchy) {
                        guid = node.Data.Identifier;
                        return VSConstants.S_OK;
                    }
                }

                guid = default;
                return VSConstants.E_FAIL;
            }));

        solution
            .Setup((x) => x.GetProjectOfGuid(ref It.Ref<Guid>.IsAny, out It.Ref<IVsHierarchy?>.IsAny))
            .Returns(new GetProjectOfGuid((ref Guid guid, out IVsHierarchy? hierarchy) => {
                foreach (var node in root.DescendantsAndSelf()) {
                    if (node.Data.Identifier == guid) {
                        hierarchy = EnsureHierarchyExists(node, solution.Object);
                        return VSConstants.S_OK;
                    }
                }

                hierarchy = null;
                return VSConstants.E_FAIL;
            }));

        solution
            .Setup((x) => x.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_ALLINSOLUTION, ref It.Ref<Guid>.IsAny, out enumerator))
            .Returns(new GetProjectEnum((uint flags, ref Guid type, out IEnumHierarchies enumerator) => {
                enumerator = CreateHierarchyEnumerator(root, solution.Object);
                return VSConstants.S_OK;
            }));

        solution
            .As<IVsSolution4>()
            .Setup((x) => x.ReloadProject(ref It.Ref<Guid>.IsAny))
            .Returns(new ReloadProjectCallback((ref Guid identifier) => {
                foreach (var item in root.Descendants()) {
                    if (item.Data.Identifier == identifier) {
                        SetType(item, HierarchyType.Project);
                        return VSConstants.S_OK;
                    }
                }
                return VSConstants.E_FAIL;
            }));

        solution
            .As<IVsSolution4>()
            .Setup((x) => x.UnloadProject(ref It.Ref<Guid>.IsAny, (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser))
            .Returns(new UnloadProjectCallback((ref Guid identifier, uint status) => {
                foreach (var item in root.Descendants()) {
                    if (item.Data.Identifier == identifier) {
                        SetType(item, HierarchyType.UnloadedProject);
                        return VSConstants.S_OK;
                    }
                }
                return VSConstants.E_FAIL;
            }));

        solution.As<IVsHierarchy>();

        return solution.Object;
    }


    private static IEnumHierarchies CreateHierarchyEnumerator(TreeItem root, IVsSolution solution) {
        Mock<IEnumHierarchies> enumerator;
        Queue<TreeItem> hierarchies;


        hierarchies = new Queue<TreeItem>(root.Descendants());
        enumerator = new Mock<IEnumHierarchies>();

        enumerator
            .Setup((x) => x.Next(1, It.IsAny<IVsHierarchy[]>(), out It.Ref<uint>.IsAny))
            .Callback(new EnumHierarchiesNextCallback((uint length, IVsHierarchy[] output, out uint count) => {
                if (hierarchies.Count > 0) {
                    output[0] = EnsureHierarchyExists(hierarchies.Dequeue(), solution);
                    count = 1;
                } else {
                    count = 0;
                }
            }))
            .Returns(VSConstants.S_OK);

        return enumerator.Object;
    }


    private static void SetClassID(Mock<IVsHierarchy> hierarchy, Guid classID) {
        hierarchy
            .As<IPersist>()
            .Setup((x) => x.GetClassID(out It.Ref<Guid>.IsAny))
            .Callback(new GetClassIDCallback((out Guid pClassID) => pClassID = classID))
            .Returns(VSConstants.S_OK);
    }


    public static IVsSolutionBuildManager2 CreateBuildManager(
        TreeItem root,
        IReadOnlyDictionary<Guid, List<string>> dependencies,
        IVsSolution solution
    ) {
        Mock<IVsSolutionBuildManager2> manager;


        manager = new Mock<IVsSolutionBuildManager2>();

        manager
            .Setup((x) => x.GetProjectDependencies(It.IsAny<IVsHierarchy>(), It.IsAny<uint>(), It.IsAny<IVsHierarchy[]>(), It.IsAny<uint[]>()))
            .Returns(new GetProjectDependenciesCallback((hierarchy, count, output, size) => {
                if (count == 0) {
                    size[0] = (uint)FindDependencies(root, dependencies, hierarchy, solution).Count;
                } else {
                    FindDependencies(root, dependencies, hierarchy, solution).CopyTo(0, output, 0, (int)count);
                }

                return VSConstants.S_OK;
            }));

        return manager.Object;
    }


    private static List<IVsHierarchy> FindDependencies(
        TreeItem root,
        IReadOnlyDictionary<Guid, List<string>> dependencies,
        IVsHierarchy source,
        IVsSolution solution
    ) {
        if (ErrorHandler.Succeeded(solution.GetGuidOfProject(source, out Guid identifier))) {
            if (dependencies.TryGetValue(identifier, out List<string> names)) {
                return root
                    .Descendants()
                    .Where((x) => names.Contains(x.Data.Name))
                    .Select((x) => EnsureHierarchyExists(x, solution))
                    .ToList();
            }
        }

        return new List<IVsHierarchy>();
    }


    private static IVsHierarchy EnsureHierarchyExists(TreeItem item, IVsSolution solution) {
        if (item.Hierarchy is null) {
            item.Hierarchy = CreateHierarchy(item.Data, solution);
        }

        return item.Hierarchy;
    }


    private delegate void GetClassIDCallback(out Guid pClassID);


    private delegate void EnumHierarchiesNextCallback(uint celt, IVsHierarchy[] rgelt, out uint pceltFetched);


    private delegate int GetGuidOfProject(IVsHierarchy hierarchy, out Guid guid);


    private delegate int GetProjectOfGuid(ref Guid guid, out IVsHierarchy? hierarchy);


    private delegate int GetProjectEnum(uint grfEnumFlags, ref Guid rguidEnumOnlyThisType, out IEnumHierarchies ppenum);


    private delegate int GetProjectDependenciesCallback(IVsHierarchy pHier, uint celt, IVsHierarchy[] rgpHier, uint[] pcActual);


    private delegate int ReloadProjectCallback(ref Guid identifier);


    private delegate int UnloadProjectCallback(ref Guid identifier, uint status);


    private delegate int GetPropertyCallback(uint itemid, int propid, out object? pvar);

}


internal delegate T TreeItemFactory<T>(XElement element, T? parent) where T : TreeItem;
