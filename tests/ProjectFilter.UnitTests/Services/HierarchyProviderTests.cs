using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NSubstitute;
using ProjectFilter.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Xunit;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services;


public static class HierarchyProviderTests {

    public class GetHierarchyMethod : ServiceTest<HierarchyProvider> {

        [SuppressMessage("Usage", "xUnit1041:Fixture arguments to test classes must have fixture sources", Justification = "False Positive. Fixed in xunit.analyzers@1.8.0")]
        public GetHierarchyMethod(GlobalServiceProvider serviceProvider) : base(serviceProvider) { }


        [Fact]
        public async Task UsesCorrectIdentifier() {
            IHierarchyNode node;
            HierarchyItem root;


            root = Setup(
                @"
                    <solution>
                        <folder name='foo'>
                            <project name='bar'/>
                        </folder>
                    </solution>
                    "
            );

            node = (await CreateService().GetHierarchyAsync()).First().Children[0];

            Assert.Equal(
                root.DescendantsAndSelf().First((x) => string.Equals(x.Data.Name, "bar", StringComparison.Ordinal)).Data.Identifier,
                node.Identifier
            );
        }


        [Fact]
        public async Task GetsCorrectIconsForNodes() {
            List<IHierarchyNode> nodes;
            IHierarchyNode node;


            Setup(
                @"
                    <solution>
                        <folder name='foo' expanded='FolderOpened' collapsed='FolderClosed'>
                            <project name='bar' expanded='Accelerator' collapsed='Zoom'/>
                        </folder>
                    </solution>
                    "
            );

            nodes = (await CreateService().GetHierarchyAsync()).ToList();
            node = Flatten(nodes).First((x) => x.Name == "foo");

            Assert.Equal(GetImageMoniker("FolderOpened"), node.ExpandedIcon);
            Assert.Equal(GetImageMoniker("FolderClosed"), node.CollapsedIcon);
        }


        [Fact]
        public async Task SkipsEmptyFolders() {
            Setup(
                @"
                    <solution>
                        <folder name='empty'/>
                        <folder name='foo'>
                            <project name='bar'/>
                            <folder name='also_empty'/>
                        </folder>
                    </solution>
                    "
            );

            await VerifyAsync(
                @"
                    <root>
                        <folder name='foo'>
                            <project name='bar'/>
                        </folder>
                    </root>
                    "
            );
        }


        [Fact]
        public async Task SkipsMiscellaneousFilesProject() {
            Setup(
                @"
                    <solution>
                        <project name='foo'/>
                        <misc name='misc'/>
                    </solution>
                    "
            );

            await VerifyAsync(
                @"
                    <root>
                        <project name='foo'/>
                    </root>
                    "
            );
        }


        [Fact]
        public async Task SortsNodesByFoldersThenByNames() {
            Setup(
                @"
                    <solution>
                        <folder name='z'>
                            <project name='b'/>
                            <project name='a'/>
                            <folder name='m'>
                                <unloaded name='c'/>
                                <project name='d'/>
                            </folder>
                        </folder>
                        <folder name='y'>
                            <unloaded name='f'/>
                            <project name='e'/>
                        </folder>
                    </solution>
                    "
            );

            await VerifyAsync(
                @"
                    <root>
                        <folder name='y'>
                            <project name='e'/>
                            <unloaded name='f'/>
                        </folder>
                        <folder name='z'>
                            <folder name='m'>
                                <unloaded name='c'/>
                                <project name='d'/>
                            </folder>
                            <project name='a'/>
                            <project name='b'/>
                        </folder>
                    </root>
                    "
            );
        }


        private HierarchyItem Setup(string data) {
            IVsSolution solution;
            HierarchyItem root;


            root = Factory.ParseHierarchies<HierarchyItem>(data, CreateNode);
            solution = Factory.CreateSolution(root);

            AddService<SVsSolution>(solution);
            AddService<SVsImageService>(CreateImageService(root));
            AddMefService(CreateHierarchyItemManager(root));

            return root;
        }


        private HierarchyItem CreateNode(XElement element, HierarchyData data) {
            HierarchyItem item;


            item = new HierarchyItem(data);

            item.Icons[(VSConstants.VSITEMID.Root, __VSHIERARCHYIMAGEASPECT.HIA_OpenFolderIcon)] = GetImageMoniker(element, "expanded");
            item.Icons[(VSConstants.VSITEMID.Root, __VSHIERARCHYIMAGEASPECT.HIA_Icon)] = GetImageMoniker(element, "collapsed");

            return item;
        }


        private static ImageMoniker GetImageMoniker(XElement element, string type) {
            return GetImageMoniker(element.Attribute(type)?.Value ?? nameof(KnownMonikers.None));
        }


        private static ImageMoniker GetImageMoniker(string name) {
            return (ImageMoniker)typeof(KnownMonikers).InvokeMember(
                name,
                BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty,
                null,
                null,
                null,
                CultureInfo.InvariantCulture
            );
        }


        private static IVsImageService2 CreateImageService(HierarchyItem root) {
            IVsImageService2 service;


            service = Substitute.For<IVsImageService2>();

            service.GetImageMonikerForHierarchyItem(default, default, default).ReturnsForAnyArgs(
                (args) => {
                    HierarchyItem? data;
                    IVsHierarchy hierarchy;
                    uint itemID;
                    int aspect;

                    hierarchy = args.ArgAt<IVsHierarchy>(0);
                    itemID = args.ArgAt<uint>(1);
                    aspect = args.ArgAt<int>(2);

                    data = root
                        .DescendantsAndSelf()
                        .Where((x) => x.Hierarchy == hierarchy)
                        .FirstOrDefault();

                    if (data is not null) {
                        if (data.Icons.TryGetValue(((VSConstants.VSITEMID)itemID, (__VSHIERARCHYIMAGEASPECT)aspect), out ImageMoniker icon)) {
                            return icon;
                        }
                    }

                    return default;
                });

            return service;
        }


        private static IVsHierarchyItemManager CreateHierarchyItemManager(HierarchyItem root) {
            IVsHierarchyItemManager manager;

            manager = Substitute.For<IVsHierarchyItemManager>();

            manager.GetHierarchyItem(default, default).ReturnsForAnyArgs((args) => {
                HierarchyItem? data;
                IVsHierarchy hierarchy;
                uint itemID;


                hierarchy = args.ArgAt<IVsHierarchy>(0);
                itemID = args.ArgAt<uint>(1);

                data = root
                        .DescendantsAndSelf()
                        .Where((x) => x.Hierarchy == hierarchy)
                        .FirstOrDefault();

                if (data is not null) {
                    IVsHierarchyItem item;
                    IVsHierarchyItemIdentity identity;


                    identity = Substitute.For<IVsHierarchyItemIdentity>();
                    identity.NestedHierarchy.Returns(hierarchy);
                    identity.NestedItemID.Returns(itemID);

                    item = Substitute.For<IVsHierarchyItem>();
                    item.HierarchyIdentity.Returns(identity);

                    return item;
                }

                return null!;
            });

            return manager;
        }


        private async Task VerifyAsync(string expected) {
            XElement solution;


            solution = XElement.Parse("<root/>");
            solution.Add(ConvertToElements(await CreateService().GetHierarchyAsync()).ToArray());

            Assert.Equal(
                XDocument.Parse(expected).Root.ToString(),
                solution.ToString()
            );
        }


        private static IEnumerable<XElement> ConvertToElements(IEnumerable<IHierarchyNode> nodes) {
            foreach (var node in nodes) {
                XElement element;


                element = new XElement(XName.Get(node.IsFolder ? "folder" : node.IsLoaded ? "project" : "unloaded"));
                element.SetAttributeValue("name", node.Name);

                element.Add(ConvertToElements(node.Children).ToArray());

                yield return element;
            }
        }


        private static IEnumerable<IHierarchyNode> Flatten(IEnumerable<IHierarchyNode> nodes) {
            foreach (var node in nodes) {
                yield return node;

                foreach (var child in Flatten(node.Children)) {
                    yield return child;
                }
            }
        }


        private class HierarchyItem : TreeItem {

            public HierarchyItem(HierarchyData data) : base(data) {
                Icons = new Dictionary<(VSConstants.VSITEMID, __VSHIERARCHYIMAGEASPECT), ImageMoniker>();
            }


            public Dictionary<(VSConstants.VSITEMID, __VSHIERARCHYIMAGEASPECT), ImageMoniker> Icons { get; }

        }

    }

}
