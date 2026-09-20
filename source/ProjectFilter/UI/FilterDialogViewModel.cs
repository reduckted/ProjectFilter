using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ProjectFilter.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;


namespace ProjectFilter.UI;


public sealed class FilterDialogViewModel : ObservableObject, IVsWindowSearch {

    private static readonly Predicate<object> CanAlwaysExecute = (x) => true;


    private readonly Func<Task<IEnumerable<IHierarchyNode>>> _hierarchyFactory;
    private readonly TextFilterFactory _textFilterFactory;
    private readonly Dictionary<string, SolutionNodeSettings>? _nodeSettings;
    private IVsWindowSearchHost? _searchHost;
    private HierarchyTreeViewItemCollection _items;
    private bool _loadProjectDependencies;
    private FilterOptions? _result;
    private Visibility _loadingVisibility;
    private Visibility _loadedVisibility;
    private bool _useRegularExpressions;
    private bool _expandLoadedProjects;
    private bool _hideSolutionFoldersWithoutLoadedProjects;


    public FilterDialogViewModel(
        Func<Task<IEnumerable<IHierarchyNode>>> hierarchyFactory,
        TextFilterFactory textFilterFactory,
        Dictionary<string, SolutionNodeSettings>? nodeSettings
    ) {
        _hierarchyFactory = hierarchyFactory ?? throw new ArgumentNullException(nameof(hierarchyFactory));
        _textFilterFactory = textFilterFactory ?? throw new ArgumentNullException(nameof(textFilterFactory));
        _nodeSettings = nodeSettings;
        _items = new HierarchyTreeViewItemCollection([]);
        _loadingVisibility = Visibility.Visible;
        _loadedVisibility = Visibility.Collapsed;

        ToggleLoadProjectDependenciesCommand = new DelegateCommand(
            (_) => LoadProjectDependencies = !LoadProjectDependencies,
            CanAlwaysExecute,
            ThreadHelper.JoinableTaskFactory
        );

        CollapseAllCommand = new DelegateCommand<HierarchyTreeViewItem>(
            CollapseAll,
            CanAlwaysExecute,
            ThreadHelper.JoinableTaskFactory
        );

        ExpandAllCommand = new DelegateCommand<HierarchyTreeViewItem>(
            ExpandAll,
            CanAlwaysExecute,
            ThreadHelper.JoinableTaskFactory
        );

        CheckAllCommand = new DelegateCommand(
            (_) => SetAllChecked(true),
            CanAlwaysExecute,
            ThreadHelper.JoinableTaskFactory
        );

        UncheckAllCommand = new DelegateCommand(
            (_) => SetAllChecked(false),
            CanAlwaysExecute,
            ThreadHelper.JoinableTaskFactory
        );

        ToggleRegularExpressionModeCommand = new DelegateCommand(
           (_) => UseRegularExpressions = !UseRegularExpressions,
           CanAlwaysExecute,
           ThreadHelper.JoinableTaskFactory
        );

        ToggleExpandLoadedProjectsCommand = new DelegateCommand(
            (_) => ExpandLoadedProjects = !ExpandLoadedProjects,
            CanAlwaysExecute,
            ThreadHelper.JoinableTaskFactory
        );

        ToggleHideSolutionFoldersWithoutLoadedProjectsCommand = new DelegateCommand(
            (_) => HideSolutionFoldersWithoutLoadedProjects = !HideSolutionFoldersWithoutLoadedProjects,
            CanAlwaysExecute,
            ThreadHelper.JoinableTaskFactory
        );

        FocusSearchBoxCommand = new DelegateCommand(
           (_) => {
               ThreadHelper.ThrowIfNotOnUIThread();
               _searchHost?.Activate();
           },
           CanAlwaysExecute,
           ThreadHelper.JoinableTaskFactory
        );

        AcceptCommand = new DelegateCommand(
            (_) => AcceptSelection(),
            CanAlwaysExecute,
            ThreadHelper.JoinableTaskFactory
        );

        SearchFiltersEnum = new WindowSearchFilterEnumerator([]);
        SearchOptionsEnum = new WindowSearchOptionEnumerator([]);
    }


    public async Task OnLoadedAsync(IVsWindowSearchHost searchHost) {
        IEnumerable<IHierarchyNode> hierarchy;


        _searchHost = searchHost;

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        hierarchy = await _hierarchyFactory.Invoke();

        Items = new HierarchyTreeViewItemCollection(
            hierarchy.Select((x) => CreateItem(x, GetSettingsForNode(x, _nodeSettings)))
        );

        LoadedVisibility = Visibility.Visible;
        LoadingVisibility = Visibility.Collapsed;

        searchHost.SetupSearch(this);
        searchHost.Activate();
    }


    private static HierarchyTreeViewItem CreateItem(IHierarchyNode node, SolutionNodeSettings? nodeSettings) {
        HierarchyTreeViewItem item;


        item = new HierarchyTreeViewItem(
            node,
            nodeSettings?.IsExpanded ?? true,
            node.Children.Select((x) => CreateItem(x, GetSettingsForNode(x, nodeSettings?.Children)))
        );

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


    private static SolutionNodeSettings? GetSettingsForNode(IHierarchyNode node, Dictionary<string, SolutionNodeSettings>? nodeSettings) {
        if (nodeSettings is not null) {
            nodeSettings.TryGetValue(node.Name, out SolutionNodeSettings settings);
            return settings;
        }

        return null;
    }


    public Visibility LoadingVisibility {
        get { return _loadingVisibility; }
        private set { SetProperty(ref _loadingVisibility, value); }
    }


    public Visibility LoadedVisibility {
        get { return _loadedVisibility; }
        private set { SetProperty(ref _loadedVisibility, value); }
    }


    public HierarchyTreeViewItemCollection Items {
        get { return _items; }
        private set { SetProperty(ref _items, value); }
    }


    public DelegateCommand ToggleLoadProjectDependenciesCommand { get; }


    public DelegateCommand<HierarchyTreeViewItem> CollapseAllCommand { get; }


    public DelegateCommand<HierarchyTreeViewItem> ExpandAllCommand { get; }


    public DelegateCommand CheckAllCommand { get; }


    public DelegateCommand UncheckAllCommand { get; }


    public DelegateCommand ToggleRegularExpressionModeCommand { get; }


    public DelegateCommand ToggleExpandLoadedProjectsCommand { get; }


    public DelegateCommand ToggleHideSolutionFoldersWithoutLoadedProjectsCommand { get; }


    public DelegateCommand FocusSearchBoxCommand { get; }


    public DelegateCommand AcceptCommand { get; }


    public FilterOptions? Result {
        get { return _result; }
        private set { SetProperty(ref _result, value); }
    }


    public bool LoadProjectDependencies {
        get { return _loadProjectDependencies; }
        set { SetProperty(ref _loadProjectDependencies, value); }
    }


    public bool UseRegularExpressions {
        get { return _useRegularExpressions; }
        set {
            ThreadHelper.ThrowIfNotOnUIThread();
            SetProperty(ref _useRegularExpressions, value);
            _searchHost?.SearchAsync(_searchHost.SearchQuery);
        }
    }


    public bool ExpandLoadedProjects {
        get { return _expandLoadedProjects; }
        set { SetProperty(ref _expandLoadedProjects, value); }
    }


    public bool HideSolutionFoldersWithoutLoadedProjects {
        get { return _hideSolutionFoldersWithoutLoadedProjects; }
        set { SetProperty(ref _hideSolutionFoldersWithoutLoadedProjects, value); }
    }


    private void CollapseAll(HierarchyTreeViewItem? root) {
        if (root is not null) {
            // If the given item doesn't have any children to collapse,
            // then step up to the parent and collapse it.
            if (root.Children.Count == 0) {
                root = root.Parent;
            }

            if (root is not null) {
                SetIsExpandedRecursively([root], false);
            }

        } else {
            SetIsExpandedRecursively(Items, false);
        }
    }


    private void ExpandAll(HierarchyTreeViewItem? root) {
        if (root is not null) {
            SetIsExpandedRecursively([root], true);
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
            LoadProjectDependencies,
            ExpandLoadedProjects,
            HideSolutionFoldersWithoutLoadedProjects
        );
    }


    private void ApplyFilter(string text) {
        if (!string.IsNullOrEmpty(text)) {
            Items.Filter(_textFilterFactory(text, UseRegularExpressions));
        } else {
            Items.ClearFilter();
        }
    }


    public void ClearSearch() {
        Items.ClearFilter();
    }


    public IVsSearchTask CreateSearch(uint cookie, IVsSearchQuery query, IVsSearchCallback callback) {
        return new SearchTask(cookie, query, callback, ApplyFilter, ClearSearch);
    }


    public void ProvideSearchSettings(IVsUIDataSource pSearchSettings) {
        ThreadHelper.ThrowIfNotOnUIThread();

        SetString(SearchSettingsDataSource.PropertyNames.SearchWatermark, "Filter projects... (Ctrl+F)");
        SetString(SearchSettingsDataSource.PropertyNames.SearchWatermarkWhenFocused, "Filter projects... (Ctrl+F)");
        SetString(SearchSettingsDataSource.PropertyNames.SearchTooltip, "Type words to filter the projects");
        SetString(SearchSettingsDataSource.PropertyNames.SearchStartTooltip, "Filter");
        SetString(SearchSettingsDataSource.PropertyNames.SearchStopTooltip, "Cancel");
        SetString(SearchSettingsDataSource.PropertyNames.SearchClearTooltip, "Clear");

        SetUint(SearchSettingsDataSource.PropertyNames.ControlMaxWidth, 10000u);

        SetUint(SearchSettingsDataSource.PropertyNames.SearchStartType, (uint)VSSEARCHSTARTTYPE.SST_DELAYED);
        SetUint(SearchSettingsDataSource.PropertyNames.SearchStartDelay, 500u);
        SetUint(SearchSettingsDataSource.PropertyNames.SearchProgressType, (uint)VSSEARCHPROGRESSTYPE.SPT_INDETERMINATE);

        SetBool(SearchSettingsDataSource.PropertyNames.SearchUseMRU, false);

        void SetString(string prop, string value) {
            pSearchSettings.SetValue(prop, BuiltInPropertyValue.Create(value));
        }

        void SetUint(string prop, uint value) {
            pSearchSettings.SetValue(prop, BuiltInPropertyValue.Create(value));
        }

        void SetBool(string prop, bool value) {
            pSearchSettings.SetValue(prop, BuiltInPropertyValue.Create(value));
        }
    }


    public bool OnNavigationKeyDown(uint dwNavigationKey, uint dwModifiers) {
        return false;
    }


    public bool SearchEnabled => true;


    public Guid Category => Guid.Empty;


    public IVsEnumWindowSearchFilters SearchFiltersEnum { get; }


    public IVsEnumWindowSearchOptions SearchOptionsEnum { get; }


    private sealed class SearchTask : VsSearchTask {

        private readonly Action<string> _search;
        private readonly Action _clear;


        public SearchTask(
            uint dwCookie,
            IVsSearchQuery pSearchQuery,
            IVsSearchCallback pSearchCallback,
            Action<string> search,
            Action clear
        ) : base(dwCookie, pSearchQuery, pSearchCallback) {
            _search = search;
            _clear = clear;
        }


        [SuppressMessage(
            "Reliability",
            "VSSDK007:ThreadHelper.JoinableTaskFactory.RunAsync",
            Justification = "Cannot await or join here."
        )]
        protected override void OnStartSearch() {
            ErrorCode = VSConstants.S_OK;

            // We are always called on a background thread, but
            // because filtering changes properties that the view
            // binds to, we need to switch to the main thread.
            ThreadHelper.JoinableTaskFactory.RunAsync(async () => {
                try {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    if (TaskStatus != VSConstants.VsSearchTaskStatus.Stopped) {
                        _search(SearchQuery.SearchString);
                    } else {
                        _clear();
                    }

                    // The number of search results are not shown anywhere,
                    // but if it's zero, then the search control gets an
                    // "error" border. Set it to one to prevent that.
                    SearchResults = 1;

                } catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex)) {
                    ErrorCode = VSConstants.E_FAIL;
                }

                // Confusingly, calling the base `OnStartSearch`
                // method actually completes the search.
                base.OnStartSearch();
            }).FireAndForget();
        }

    }

}
