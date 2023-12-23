using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Threading;
using ProjectFilter.Services;
using ProjectFilter.UI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;


namespace ProjectFilter.UI;


public sealed class FilterDialogViewModel : ObservableObject, IDisposable {

    private static readonly Predicate<object> CanAlwaysExecute = (x) => true;


    private readonly Func<Task<IEnumerable<IHierarchyNode>>> _hierarchyFactory;
    private readonly TextFilterFactory _textFilterFactory;
    private readonly Dictionary<string, SolutionNodeSettings>? _nodeSettings;
    private readonly IDebouncer _debouncer;
    private HierarchyTreeViewItemCollection _items;
    private string _searchText;
    private bool _loadProjectDependencies;
    private FilterOptions? _result;
    private Visibility _loadingVisibility;
    private Visibility _loadedVisibility;
    private bool _useRegularExpressions;
    private bool _expandLoadedProjects;
    private bool _invalidFilter;


    public FilterDialogViewModel(
        Func<Task<IEnumerable<IHierarchyNode>>> hierarchyFactory,
        Func<TimeSpan, IDebouncer> debouncerFactory,
        TextFilterFactory textFilterFactory,
        Dictionary<string, SolutionNodeSettings>? nodeSettings,
        JoinableTaskFactory joinableTaskFactory
    ) {
        if (debouncerFactory is null) {
            throw new ArgumentNullException(nameof(debouncerFactory));
        }

        _hierarchyFactory = hierarchyFactory ?? throw new ArgumentNullException(nameof(hierarchyFactory));
        _textFilterFactory = textFilterFactory ?? throw new ArgumentNullException(nameof(textFilterFactory));
        _nodeSettings = nodeSettings;

        _items = new HierarchyTreeViewItemCollection(Enumerable.Empty<HierarchyTreeViewItem>());
        _searchText = "";
        _loadingVisibility = Visibility.Visible;
        _loadedVisibility = Visibility.Collapsed;

        FocusSearchBoxSource = new FocusSource();

        ToggleLoadProjectDependenciesCommand = new DelegateCommand(
            (_) => LoadProjectDependencies = !LoadProjectDependencies,
            CanAlwaysExecute,
            joinableTaskFactory
        );

        CollapseAllCommand = new DelegateCommand<HierarchyTreeViewItem>(
            CollapseAll,
            CanAlwaysExecute,
            joinableTaskFactory
        );

        ExpandAllCommand = new DelegateCommand<HierarchyTreeViewItem>(
            ExpandAll,
            CanAlwaysExecute,
            joinableTaskFactory
        );

        CheckAllCommand = new DelegateCommand(
            (_) => SetAllChecked(true),
            CanAlwaysExecute,
            joinableTaskFactory
        );

        UncheckAllCommand = new DelegateCommand(
            (_) => SetAllChecked(false),
            CanAlwaysExecute,
            joinableTaskFactory
        );

        ToggleRegularExpressionModeCommand = new DelegateCommand(
           (_) => UseRegularExpressions = !UseRegularExpressions,
           CanAlwaysExecute,
           joinableTaskFactory
        );

        ToggleExpandLoadedProjectsCommand = new DelegateCommand(
            (_) => ExpandLoadedProjects = !ExpandLoadedProjects,
            CanAlwaysExecute,
            joinableTaskFactory
        );

        FocusSearchBoxCommand = new DelegateCommand(
           (_) => FocusSearchBoxSource.RequestFocus(),
           CanAlwaysExecute,
           joinableTaskFactory
        );

        AcceptCommand = new DelegateCommand(
            (_) => AcceptSelection(),
            CanAlwaysExecute,
            joinableTaskFactory
        );

        // Use a `DispatcherTimer` constructor without a callback
        // so that the timer doesn't start immediately. We only
        // want to start the timer when the search text changes.
        _debouncer = debouncerFactory.Invoke(TimeSpan.FromMilliseconds(500));
        _debouncer.Stable += OnSearchStable;
    }


    public async Task OnLoadedAsync() {
        IEnumerable<IHierarchyNode> hierarchy;


        hierarchy = await _hierarchyFactory.Invoke();

        Items = new HierarchyTreeViewItemCollection(
            hierarchy.Select((x) => CreateItem(x, GetSettingsForNode(x, _nodeSettings)))
        );

        LoadedVisibility = Visibility.Visible;
        LoadingVisibility = Visibility.Collapsed;
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


    public DelegateCommand FocusSearchBoxCommand { get; }


    public DelegateCommand AcceptCommand { get; }


    public FocusSource FocusSearchBoxSource { get; }


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


    public bool UseRegularExpressions {
        get { return _useRegularExpressions; }
        set {
            SetProperty(ref _useRegularExpressions, value);
            Search();
        }
    }


    public bool ExpandLoadedProjects {
        get { return _expandLoadedProjects; }
        set { SetProperty(ref _expandLoadedProjects, value); }
    }


    public bool InvalidFilter {
        get { return _invalidFilter; }
        private set { SetProperty(ref _invalidFilter, value); }
    }


    private void CollapseAll(HierarchyTreeViewItem? root) {
        if (root is not null) {
            // If the given item doesn't have any children to collapse,
            // then step up to the parent and collapse it.
            if (root.Children.Count == 0) {
                root = root.Parent;
            }

            if (root is not null) {
                SetIsExpandedRecursively(new[] { root }, false);
            }

        } else {
            SetIsExpandedRecursively(Items, false);
        }
    }


    private void ExpandAll(HierarchyTreeViewItem? root) {
        if (root is not null) {
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
            LoadProjectDependencies,
            ExpandLoadedProjects
        );
    }


    private void OnSearchStable(object sender, EventArgs e) {
        Search();
    }


    private void Search() {
        string text;


        text = _searchText.Trim();

        if (!string.IsNullOrEmpty(text)) {
            ITextFilter filter;


            try {
                filter = _textFilterFactory(text, UseRegularExpressions);
            } catch (ArgumentException) {
                InvalidFilter = true;
                return;
            }

            Items.Filter(filter);

        } else {
            Items.ClearFilter();
        }

        InvalidFilter = false;
    }


    public void Dispose() {
        _debouncer.Dispose();
        _debouncer.Stable -= OnSearchStable;
    }

}
