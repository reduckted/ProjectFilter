using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using ProjectFilter.Helpers;
using System;
using System.Collections.Generic;
using Xunit;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services {

    public static class ExtensionSettingsTests {

        public class LoadProjectDependenciesProperty : ServiceTest<ExtensionSettings> {

            readonly SettingsManager _manager;


            public LoadProjectDependenciesProperty() {
                _manager = new SettingsManager();
                AddService<SVsSettingsManager, IVsSettingsManager>(_manager);
            }


            [Fact]
            public async Task ReturnsTrueWhenSettingIsNotDefined() {
                Assert.True((await CreateAsync()).LoadProjectDependencies);
            }


            [Fact]
            public async Task ReturnsStoredValueWhenValueIsDefined() {
                ExtensionSettings settings;


                settings = await CreateAsync();

                settings.LoadProjectDependencies = false;
                Assert.False(settings.LoadProjectDependencies);

                settings.LoadProjectDependencies = true;
                Assert.True(settings.LoadProjectDependencies);
            }

        }


        private class SettingsManager : IVsSettingsManager {

            private readonly Mock<IVsWritableSettingsStore> _store;
            private readonly Dictionary<string, Dictionary<string, object>> _collections;


            private delegate void GetBoolOrDefaultCallback(string collection, string name, int defaultValue, out int value);


            public SettingsManager() {
                _collections = new Dictionary<string, Dictionary<string, object>>();
                _store = new Mock<IVsWritableSettingsStore>();

                _store.Setup((x) => x.CreateCollection(It.IsAny<string>())).Callback(
                    (string name) => {
                        if (!_collections.ContainsKey(name)) {
                            _collections[name] = new Dictionary<string, object>();
                        }
                    }
                ).Returns(VSConstants.S_OK);

                _store.As<IVsSettingsStore>().Setup((x) => x.GetBoolOrDefault(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), out It.Ref<int>.IsAny)).Callback(
                    new GetBoolOrDefaultCallback((string collectionPath, string name, int defaultValue, out int value) => {
                        if (_collections.TryGetValue(collectionPath, out Dictionary<string, object> collection)) {
                            if (collection.TryGetValue(name, out object obj)) {
                                value = (int)obj;
                                return;
                            }
                        }

                        value = defaultValue;
                    })
                ).Returns(VSConstants.S_OK);

                _store.Setup((x) => x.SetBool(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).Callback(
                    (string collectionPath, string name, int value) => {
                        _collections[collectionPath][name] = value;
                    }
                ).Returns(VSConstants.S_OK);
            }


            public int GetReadOnlySettingsStore(uint scope, out IVsSettingsStore store) {
                store = _store.Object;
                return VSConstants.S_OK;
            }


            public int GetWritableSettingsStore(uint scope, out IVsWritableSettingsStore writableStore) {
                writableStore = _store.Object;
                return VSConstants.S_OK;
            }


            public int GetCollectionScopes(string collectionPath, out uint scopes) {
                throw new NotSupportedException();
            }


            public int GetPropertyScopes(string collectionPath, string propertyName, out uint scopes) {
                throw new NotSupportedException();
            }


            public int GetApplicationDataFolder(uint folder, out string folderPath) {
                throw new NotSupportedException();
            }


            public int GetCommonExtensionsSearchPaths(uint paths, string[] commonExtensionsPaths, out uint actualPaths) {
                throw new NotSupportedException();
            }

        }

    }

}
