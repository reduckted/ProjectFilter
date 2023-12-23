using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;


namespace ProjectFilter.Services;


public class SolutionSettingsManager : ISolutionSettingsManager {

    internal const string OptionsKey = "8a9db320eea24fd6abac372ed68aec4";


    private readonly IVsPersistSolutionOpts _persister;
    private readonly ILogger _logger;
    private SolutionSettings? _settings;
    private bool _hasLoadedForCurrentSolution;


    public SolutionSettingsManager(IVsPersistSolutionOpts persister, ILogger logger) {
        _persister = persister;
        _logger = logger;

        // Clear the settings when the solution closes. The `Load` method is called
        // every time a solution is opened that contains the settings. If a solution
        // is loaded that does not contain the settings, that method is not called,
        // and we'll be left with the settings from the previous solution.
        VS.Events.SolutionEvents.OnAfterCloseSolution += () => {
            _settings = null;
            _hasLoadedForCurrentSolution = false;
        };
    }


    public async Task<SolutionSettings?> GetSettingsAsync() {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        // The settings will be automatically loaded when a solution is opened, but if
        // a solution was already loaded before this package was loaded, then the settings
        // will not have been loaded. So, if we haven't loaded the solution's settings
        // yet, and a solution is open, then we need to manually load the settings.
        if (!_hasLoadedForCurrentSolution && (await VS.Services.GetSolutionAsync()).IsOpen()) {
            (await VS.Services.GetSolutionPersistenceAsync()).LoadPackageUserOpts(_persister, OptionsKey);

            // If the solution does not contain the settings, then the `Load`
            // method won't be called, but we can still flag that we've loaded the
            // current solution's settings so that we don't try to do this again.
            _hasLoadedForCurrentSolution = true;
        }

        return _settings;
    }


    public void SetSettings(SolutionSettings? settings) {
        _settings = settings;
    }


    public void Load(Stream stream) {
        // This method will be called in a few scenarios:
        // 
        //   1. A solution is loaded and contains the settings.
        //   2. We manually load the settings when this package
        //      is loaded after the solution is loaded.
        //   3. When projects are loaded.
        //
        // The third scenario is problematic. When selecting the projects to load,
        // the settings that the user used are stored via `SetSettings`, but they
        // aren't saved to the solution. We then load the projects that were selected,
        // and that causes this method to be called, which will overwrite the new settings
        // with the settings that were originally stored for the solution. So, if we've
        // already loaded the settings for the current solution, then we can ignore this call.
        if (!_hasLoadedForCurrentSolution) {
            using (StreamReader text = new(stream, Encoding.UTF8, false, 1024, true)) {
                using (JsonTextReader reader = new(text)) {
                    try {
                        _settings = JsonSerializer.CreateDefault().Deserialize<SolutionSettings>(reader);

                    } catch (Exception ex) when (!ex.IsCritical()) {
                        _logger.WriteLineAsync($"Failed to load solution settings: ${ex.Message}").FireAndForget();
                        _settings = null;
                    }
                }
            }

            _hasLoadedForCurrentSolution = true;
        }
    }


    public void Save(Stream stream) {
        using (StreamWriter text = new(stream, Encoding.UTF8, 1024, true)) {
            using (JsonTextWriter writer = new(text)) {
                JsonSerializer.CreateDefault().Serialize(writer, _settings);
            }
        }
    }

}
