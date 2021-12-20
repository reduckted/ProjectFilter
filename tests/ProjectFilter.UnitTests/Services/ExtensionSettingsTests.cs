using System.Threading.Tasks;
using Xunit;


namespace ProjectFilter.Services {

    public static class ExtensionSettingsTests {

        public class LoadProjectDependenciesProperty {

            [Fact]
            public void DefaultsToTrue() {
                Assert.True(new ExtensionSettings().LoadProjectDependencies);
            }

        }

    }

}
