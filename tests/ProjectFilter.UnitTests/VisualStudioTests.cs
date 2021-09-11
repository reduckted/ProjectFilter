using Microsoft.VisualStudio.Sdk.TestFramework;
using Xunit;


namespace Community.VisualStudio.Toolkit.Testing
{

    [CollectionDefinition(Collection, DisableParallelization = true)]
    public class xVisualStudioTests : ICollectionFixture<GlobalServiceProvider>, ICollectionFixture<MefHostingFixture>
    {

        public const string Collection = nameof(xVisualStudioTests);

    }

}
