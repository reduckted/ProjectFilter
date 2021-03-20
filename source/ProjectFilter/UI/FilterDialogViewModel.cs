using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using ProjectFilter.Services;
using ProjectFilter.UI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;


namespace ProjectFilter.UI {

    public sealed class FilterDialogViewModel : ObservableObject, IDisposable {

        private readonly IDebouncer _debouncer;
        private readonly SearchQueryFactory _searchQueryFactory;
        private string _searchText;
        private bool _loadProjectDependencies;
        private FilterOptions? _result;


        public FilterDialogViewModel(
            IEnumerable<IHierarchyNode> hierarchy,
            Func<TimeSpan, IDebouncer> debouncerFactory,
            SearchQueryFactory searchQueryFactory
        ) {
            if (debouncerFactory is null) {
                throw new ArgumentNullException(nameof(debouncerFactory));
            }

            _searchQueryFactory = searchQueryFactory;

            Items = new HierarchyTreeViewItemCollection(hierarchy.Select(CreateItem));

            ToggleLoadProjectDependenciesCommand = new DelegateCommand(() => LoadProjectDependencies = !LoadProjectDependencies);
            CollapseAllCommand = new DelegateCommand<HierarchyTreeViewItem>(CollapseAll);
            ExpandAllCommand = new DelegateCommand<HierarchyTreeViewItem>(ExpandAll);
            CheckAllCommand = new DelegateCommand(() => SetAllChecked(true));
            UncheckAllCommand = new DelegateCommand(() => SetAllChecked(false));
            AcceptCommand = new DelegateCommand(AcceptSelection);

            _searchText = "";

            // Use a `DispatcherTimer` constructor without a callback
            // so that the timer doesn't start immediately. We only
            // want to start the timer when the search text changes.
            _debouncer = debouncerFactory.Invoke(TimeSpan.FromMilliseconds(500));
            _debouncer.Stable += OnSearchStable;
        }


        private static HierarchyTreeViewItem CreateItem(IHierarchyNode node) {
            HierarchyTreeViewItem item;


            item = new HierarchyTreeViewItem(node, node.Children.Select(CreateItem));

            // Set the checked state of the item. If it has children, then we check 
            // it based on whether all or none of the children are checked. If it 
            // doesn't have children, then we check it if the node is loaded. In either 
            // case, we only set the checked state of the direct item and don't affect 
            // the children or parent because the children have already been initialized, 
            // and the parent will be initialized when we return from this method.
            item.SetIsChecked(
                item.Children.Count > 0 ? item.Children.CalculateCheckedState() : node.IsLoaded,
                false,
                false
            );

            return item;
        }


        public HierarchyTreeViewItemCollection Items { get; }


        public DelegateCommand ToggleLoadProjectDependenciesCommand { get; }


        public DelegateCommand<HierarchyTreeViewItem> CollapseAllCommand { get; }


        public DelegateCommand<HierarchyTreeViewItem> ExpandAllCommand { get; }


        public DelegateCommand CheckAllCommand { get; }


        public DelegateCommand UncheckAllCommand { get; }


        public DelegateCommand AcceptCommand { get; }


        public FilterOptions? Result {
            get { return _result; }
            private set { SetProperty(ref _result, value); }
        }


        public string SearchText {
            get { return _searchText; }
            set {
                SetProperty(ref _searchText, value);

                _debouncer.Cancel();

                // If the text is cleared, bypass the debounce and "search" 
                // immediately so that the original items are restored.
                if (string.IsNullOrWhiteSpace(_searchText)) {
                    Search();
                } else {
                    _debouncer.Start();
                }
            }
        }


        public bool LoadProjectDependencies {
            get { return _loadProjectDependencies; }
            set { SetProperty(ref _loadProjectDependencies, value); }
        }


        private void CollapseAll(HierarchyTreeViewItem? root) {
            if (root != null) {
                // If the given item doesn't have any children to collapse,
                // then step up to the parent and collapse it.
                if (root.Children.Count == 0) {
                    root = root.Parent;
                }

                if (root != null) {
                    SetIsExpandedRecursively(new[] { root }, false);
                }

            } else {
                SetIsExpandedRecursively(Items, false);
            }
        }


        private void ExpandAll(HierarchyTreeViewItem? root) {
            if (root != null) {
                SetIsExpandedRecursively(new[] { root }, true);
            } else {
                SetIsExpandedRecursively(Items, true);
            }
        }


        private static void SetIsExpandedRecursively(IEnumerable<HierarchyTreeViewItem> nodes, bool value) {
            foreach (var node in nodes.SelectMany((x) => x.DescendantsAndSelf())) {
                node.IsExpanded = value;
            }
        }


        private void SetAllChecked(bool value) {
            // Set the value into the top-level nodes and 
            // that will flow down to all of their descendants.
            foreach (var node in Items) {
                node.IsChecked = value;
            }
        }


        private void AcceptSelection() {
            ILookup<bool, Guid> projects;


            // Select all projects from the tree, including projects that 
            // are currently hidden by the search text. The search text 
            // affects what is displayed, but doesn't affect the 
            // results that we return. Split the projects into two 
            // groups based on whether the project should be loaded.
            projects = (
                from node in Items.GetFullHierarchy()
                where !node.IsFolder
                select (node.Identifier, Load: node.IsChecked.GetValueOrDefault())
            ).ToLookup((x) => x.Load, (x) => x.Identifier);

            Result = new FilterOptions(
                projects[true],
                projects[false],
                LoadProjectDependencies
            );
        }


        private void OnSearchStable(object sender, EventArgs e) {
            Search();
        }


        private void Search() {
            string text;
            IVsSearchQuery? query;


            text = _searchText.Trim();

            if (!string.IsNullOrEmpty(text)) {
                query = _searchQueryFactory.Invoke(text);
            } else {
                query = null;
            }

            if (query != null) {
                Items.Filter(new HierarchySearchMatchEvaluator(query));
            } else {
                Items.ClearFilter();
            }
        }


        public void Dispose() {
            _debouncer.Dispose();
            _debouncer.Stable -= OnSearchStable;
        }

    }

}
