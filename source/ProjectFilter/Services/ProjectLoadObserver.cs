// Ignore Spelling: Unk

using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows.Threading;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services;


public class ProjectLoadObserver : IVsSolutionEvents {

    private static ProjectLoadObserver? _observer;


    private readonly IFilterService _filterService;
    private readonly IVsSolution _solution;
    private readonly ILogger _logger;
    private readonly object _lock;
    private readonly List<Guid> _buffer;
    private DispatcherTimer? _debouncer;


    public static async Task InitializeAsync() {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        _observer = new ProjectLoadObserver(
            await VS.GetRequiredServiceAsync<IFilterService, IFilterService>(),
            await VS.Services.GetSolutionAsync(),
            await VS.GetRequiredServiceAsync<ILogger, ILogger>()
        );

        IVsSolution svc = await VS.GetRequiredServiceAsync<SVsSolution, IVsSolution>();
        svc.AdviseSolutionEvents(_observer, out _);
    }


    private ProjectLoadObserver(
        IFilterService filterService,
        IVsSolution solution,
        ILogger logger
    ) {
        _filterService = filterService;
        _solution = solution;
        _logger = logger;

        _lock = new object();
        _buffer = [];
    }


    public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) {
        ThreadHelper.ThrowIfNotOnUIThread();

        // If "added" is true, it means the project has been loaded after the solution was opened.
        // If it's false, it means the project was added while the solution was being loaded.
        // We are only interested in projects that are loaded after the solution is opened.
        if ((fAdded != 0) && (!_filterService.IsFiltering)) {
            if (ErrorHandler.Succeeded(_solution.GetGuidOfProject(pHierarchy, out Guid identifier))) {
                string? name;


                pHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_Name, out object obj);
                name = (obj as string) ?? identifier.ToString();

                _logger.WriteLineAsync($"Project '{name}' loaded outside of the filter service.").FireAndForget();

                // There might be multiple projects being loaded in quick
                // succession, so we debounce the processing of the new
                // projects to avoid repeated interruptions to the user.
                lock (_lock) {
                    _buffer.Add(identifier);

                    _debouncer?.Stop();

                    if (_debouncer is null) {
                        _debouncer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                        _debouncer.Tick += ProcessBuffer;
                    }

                    _debouncer.Start();
                }
            }
        }

        return VSConstants.S_OK;
    }


    [SuppressMessage(
        "Reliability",
        "VSSDK007:ThreadHelper.JoinableTaskFactory.RunAsync",
        Justification = "Cannot await in an event handler."
    )]
    private void ProcessBuffer(object sender, EventArgs e) {
        Guid[] identifiers;


        lock (_lock) {
            _debouncer?.Stop();
            identifiers = [.. _buffer];
            _buffer.Clear();
        }

        // We can't await in this method because it's an event
        // handler, so we use the thread helper to run asynchronously.
        ThreadHelper.JoinableTaskFactory.RunAsync(async () => {
            try {
                FilterBehavior behavior;


                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                await _logger.WriteLineAsync("Processing projects opened outside of the filter service.");

                behavior = await GetFitlerBehaviorAsync();

                if (!behavior.LoadProjectDependencies) {
                    await _logger.WriteLineAsync(
                        "Project dependencies should not be loaded. No additional projects will be loaded."
                    );
                    return;
                }

                await _logger.WriteLineAsync("Ensuring that dependencies are loaded...");

                await _filterService.ApplyAsync(new FilterOptions(identifiers, [], behavior), true);

                await _logger.WriteLineAsync("Finished loading project dependencies.");

            } catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex)) {
                await _logger.WriteLineAsync($"Error processing projects opened outside of the filter service: {ex}");
            }
        }).FireAndForget();
    }


    private static async Task<FilterBehavior> GetFitlerBehaviorAsync() {
        ISolutionSettingsManager solutionSettingsManager;
        SolutionSettings? solutionSettings;
        IExtensionSettings globalSettings;


        solutionSettingsManager = await VS.GetRequiredServiceAsync<ISolutionSettingsManager, ISolutionSettingsManager>();
        solutionSettings = await solutionSettingsManager.GetSettingsAsync();

        globalSettings = await VS.GetRequiredServiceAsync<IExtensionSettings, IExtensionSettings>();
        await globalSettings.LoadAsync();

        return new FilterBehavior(solutionSettings, globalSettings);
    }


    public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) {
        return VSConstants.S_OK;
    }


    public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) {
        return VSConstants.S_OK;
    }


    public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) {
        return VSConstants.S_OK;
    }


    public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) {
        return VSConstants.S_OK;
    }


    public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) {
        return VSConstants.S_OK;
    }


    public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution) {
        return VSConstants.S_OK;
    }


    public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) {
        return VSConstants.S_OK;
    }


    public int OnBeforeCloseSolution(object pUnkReserved) {
        return VSConstants.S_OK;
    }


    public int OnAfterCloseSolution(object pUnkReserved) {
        return VSConstants.S_OK;

    }

}
