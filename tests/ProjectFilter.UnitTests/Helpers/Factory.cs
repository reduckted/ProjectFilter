using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.PatternMatching;
using NSubstitute;
using ProjectFilter.Services;
using ProjectFilter.UI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;


namespace ProjectFilter.Helpers;


internal static class Factory {

    private const string SolutionRoot = "C:\\Solution";


    public static IPatternMatcherFactory CreatePatternMatcherFactory(IEnumerable<Span> matches) {
        IPatternMatcherFactory factory;
        IPatternMatcher matcher;


        matcher = Substitute.For<IPatternMatcher>();
        matcher.TryMatch(default).ReturnsForAnyArgs(
            new PatternMatch(
                PatternMatchKind.Exact,
                false,
                false,
                ImmutableArray.CreateRange(matches)
            )
        );

        factory = Substitute.For<IPatternMatcherFactory>();
        factory.CreatePatternMatcher(default, default).ReturnsForAnyArgs(matcher);

        return factory;
    }


    public static ITextFilter CreateTextFilter(string pattern, bool _) {
        // Ignore the type of pattern requested and always create the filter
        // from a regular expression because that's the simplest thing to do.
        return new RegexTextFilter(pattern);
    }


    public static HierarchyTreeViewItem CreateTreeViewItem(string name = "", IEnumerable<HierarchyTreeViewItem>? children = null, bool? isChecked = false) {
        IHierarchyNode node;


        node = Substitute.For<IHierarchyNode>();
        node.Name.Returns(name);

        return new HierarchyTreeViewItem(node, true, children ?? Enumerable.Empty<HierarchyTreeViewItem>()) {
            IsChecked = isChecked
        };
    }


    public static T ParseHierarchies<T>(string data, TreeItemFactory<T> factory) where T : TreeItem {
        return ParseHierarchyElement(XDocument.Parse(data).Root, factory, new Dictionary<string, HierarchyData>(), null);
    }


    private static T ParseHierarchyElement<T>(
        XElement element,
        TreeItemFactory<T> factory,
        Dictionary<string, HierarchyData> lookup,
        T? parent
    ) where T : TreeItem {
        T item;
        HierarchyData data;


        data = CreateHierarchyData(element, lookup, parent?.Data);
        item = factory(element, data);

        if (parent is not null) {
            parent.Children.Add(item);
            item.Parent = parent;
        }

        foreach (var child in element.Elements()) {
            ParseHierarchyElement(child, factory, lookup, item);
        }

        lookup[data.Name] = data;

        return item;
    }


    private static HierarchyData CreateHierarchyData(
        XElement element,
        Dictionary<string, HierarchyData> lookup,
        HierarchyData? parent = null
    ) {
        HierarchyData data;
        HierarchyType type;
        string? dependencies;


        type = ElementNameToType(element.Name.LocalName);

        data = new HierarchyData(
            GetIdentifier(element),
            element.Attribute("name")?.Value ?? "?",
            parent?.Identifier,
            element.Attribute("shared")?.Value == "true"
        );

        dependencies = element.Attribute("dependsOn")?.Value;

        if (dependencies is not null) {
            data.SetDependencies(dependencies.Split(','), lookup);
        }

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
        IVsHierarchy hierarchy;
        string canonicalName;


        if (data.IsProject) {
            hierarchy = Substitute.For<IVsHierarchy, IPersist, IVsProject>();
        } else {
            hierarchy = Substitute.For<IVsHierarchy, IPersist>();
        }

        SetClassID((IPersist)hierarchy, data.CLSID);

        switch (data.Type) {
            case HierarchyType.Solution:
                canonicalName = Path.Combine(SolutionRoot, data.Name + ".sln");
                break;

            case HierarchyType.Project:
            case HierarchyType.UnloadedProject:
                canonicalName = Path.Combine(SolutionRoot, data.Name, data.Name + (data.IsShared ? ".shproj" : ".csproj"));
                break;

            default:
                canonicalName = data.Identifier.ToString();
                break;
        }

        hierarchy
            .GetCanonicalName((uint)VSConstants.VSITEMID.Root, out Arg.Any<string>())
            .Returns((args) => {
                args[1] = canonicalName;
                return VSConstants.S_OK;
            });

        hierarchy
            .GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_Name, out Arg.Any<object>())
            .Returns((args) => {
                args[2] = data.Name;
                return VSConstants.S_OK;
            });

        hierarchy
            .GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ParentHierarchy, out Arg.Any<object>())
            .Returns((args) => {
                Guid parentIdentifier;


                if (data.Parent is null) {
                    args[2] = null;
                    return VSConstants.S_OK;
                }

                parentIdentifier = data.Parent.Value;

                if (ErrorHandler.Succeeded(solution.GetProjectOfGuid(ref parentIdentifier, out IVsHierarchy parent))) {
                    args[2] = parent;
                    return VSConstants.S_OK;
                }

                args[2] = null;
                return VSConstants.E_FAIL;
            });

        hierarchy
            .GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID7.VSHPROPID_SharedItemsImportFullPaths, out Arg.Any<object>())
            .Returns((args) => {
                List<HierarchyData> dependencies;


                dependencies = data.GetDependencyData().Where((x) => x.IsShared).ToList();

                if (dependencies.Count > 0) {
                    args[2] = string.Join(
                        "|",
                        dependencies.Select((x) => Path.Combine(SolutionRoot, x.Name, x.Name + ".projitems"))
                    );
                    return VSConstants.S_OK;
                }

                args[2] = null;
                return VSConstants.E_FAIL;
            });

        return hierarchy;
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
                    new Guid(
                        data.IsShared ?
                        "{9a19103f-16f7-4668-be54-9a1e7a4f7556}" :  // C# project.
                        "{d954291e-2a0b-460d-934e-dc6b0785db48}"    // C# shared project.
                    ),
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
        IVsSolution solution;


        solution = Substitute.For<IVsSolution, IVsSolution4, IVsHierarchy>();

        solution
            .GetGuidOfProject(Arg.Any<IVsHierarchy>(), out Arg.Any<Guid>())
            .Returns((args) => {
                IVsHierarchy hierarchy;


                hierarchy = args.ArgAt<IVsHierarchy>(0);

                foreach (var node in root.DescendantsAndSelf()) {
                    if (node.Hierarchy == hierarchy) {
                        args[1] = node.Data.Identifier;
                        return VSConstants.S_OK;
                    }
                }

                args[1] = default;
                return VSConstants.E_FAIL;
            });

        solution
            .GetProjectOfGuid(ref Arg.Any<Guid>(), out Arg.Any<IVsHierarchy?>())
            .Returns((args) => {
                Guid guid;


                guid = args.ArgAt<Guid>(0);

                foreach (var node in root.DescendantsAndSelf()) {
                    if (node.Data.Identifier == guid) {
                        args[1] = EnsureHierarchyExists(node, solution);
                        return VSConstants.S_OK;
                    }
                }

                args[1] = null;
                return VSConstants.E_FAIL;
            });

        solution
            .GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_ALLINSOLUTION, ref Arg.Any<Guid>(), out Arg.Any<IEnumHierarchies>())
            .Returns((args) => {
                args[2] = CreateHierarchyEnumerator(root, solution);
                return VSConstants.S_OK;
            });

        ((IVsSolution4)solution)
            .ReloadProject(ref Arg.Any<Guid>())
            .Returns((args) => {
                Guid identifier;


                identifier = args.ArgAt<Guid>(0);

                foreach (var item in root.Descendants()) {
                    if (item.Data.Identifier == identifier) {
                        SetType(item, HierarchyType.Project);
                        return VSConstants.S_OK;
                    }
                }
                return VSConstants.E_FAIL;
            });

        ((IVsSolution4)solution)
            .UnloadProject(ref Arg.Any<Guid>(), (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser)
            .Returns((args) => {
                Guid identifier;


                identifier = args.ArgAt<Guid>(0);

                foreach (var item in root.Descendants()) {
                    if (item.Data.Identifier == identifier) {
                        SetType(item, HierarchyType.UnloadedProject);
                        return VSConstants.S_OK;
                    }
                }
                return VSConstants.E_FAIL;
            });

        return solution;
    }


    private static IEnumHierarchies CreateHierarchyEnumerator(TreeItem root, IVsSolution solution) {
        IEnumHierarchies enumerator;
        Queue<TreeItem> hierarchies;


        hierarchies = new Queue<TreeItem>(root.Descendants());
        enumerator = Substitute.For<IEnumHierarchies>();

        enumerator
            .Next(1, Arg.Any<IVsHierarchy[]>(), out Arg.Any<uint>())
            .Returns((args) => {
                if (hierarchies.Count > 0) {
                    args.ArgAt<IVsHierarchy[]>(1)[0] = EnsureHierarchyExists(hierarchies.Dequeue(), solution);
                    args[2] = 1u;
                } else {
                    args[2] = 0u;
                }

                return VSConstants.S_OK;
            });

        return enumerator;
    }


    private static void SetClassID(IPersist persist, Guid classID) {
        persist.GetClassID(out Arg.Any<Guid>()).Returns((args) => {
            args[0] = classID;
            return VSConstants.S_OK;
        });
    }


    public static IVsSolutionBuildManager2 CreateBuildManager(
        TreeItem root,
        IReadOnlyDictionary<Guid, List<string>> dependencies,
        IVsSolution solution
    ) {
        IVsSolutionBuildManager2 manager;


        manager = Substitute.For<IVsSolutionBuildManager2>();

        manager
            .GetProjectDependencies(default, default, default, default)
            .ReturnsForAnyArgs((args) => {
                IVsHierarchy hierarchy;
                uint count;
                IVsHierarchy[] output;
                uint[] size;


                hierarchy = args.ArgAt<IVsHierarchy>(0);
                count = args.ArgAt<uint>(1);
                output = args.ArgAt<IVsHierarchy[]>(2);
                size = args.ArgAt<uint[]>(3);

                if (count == 0) {
                    size[0] = (uint)GetProjectDependencies(root, dependencies, hierarchy, solution).Count;
                } else {
                    GetProjectDependencies(root, dependencies, hierarchy, solution).CopyTo(0, output, 0, (int)count);
                }

                return VSConstants.S_OK;
            });

        return manager;
    }


    private static List<IVsHierarchy> GetProjectDependencies(
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
                    .Where((x) => !x.Data.IsShared)
                    .Select((x) => EnsureHierarchyExists(x, solution))
                    .ToList();
            }
        }

        return new List<IVsHierarchy>();
    }


    private static IVsHierarchy EnsureHierarchyExists(TreeItem item, IVsSolution solution) {
        item.Hierarchy ??= CreateHierarchy(item.Data, solution);

        return item.Hierarchy;
    }

}


internal delegate T TreeItemFactory<T>(XElement element, HierarchyData data) where T : TreeItem;
