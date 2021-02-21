using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using System.Runtime.CompilerServices;
using System.Threading;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services {

    public class ExtensionSettings : IAsyncInitializable, IExtensionSettings {

        private const string CollectionPath = "ProjectFilter_ed6f0249-446a-4ddf-a8e8-b545113ba58f";


#nullable disable
        private WritableSettingsStore _store;
#nullable restore


        public async Task InitializeAsync(IAsyncServiceProvider provider, CancellationToken cancellationToken) {
            SettingsManager manager;


            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            manager = new ShellSettingsManager(
                await provider.GetServiceAsync<SVsSettingsManager, IVsSettingsManager>()
            );

            _store = manager.GetWritableSettingsStore(SettingsScope.UserSettings);
        }


        public bool LoadProjectDependencies {
            get { return GetBoolean(true); }
            set { SetBoolean(value); }
        }


        private bool GetBoolean(bool defaultValue, [CallerMemberName] string? propertyName = null) {
            return _store.GetBoolean(CollectionPath, propertyName, defaultValue);
        }


        private void SetBoolean(bool value, [CallerMemberName] string? propertyName = null) {
            _store.CreateCollection(CollectionPath);
            _store.SetBoolean(CollectionPath, propertyName, value);
        }

    }

}
