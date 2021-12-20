using Microsoft.VisualStudio.Sdk.TestFramework;
using Xunit;


namespace ProjectFilter;


[CollectionDefinition(CollectionName, DisableParallelization = true)]
public class VisualStudioTests : ICollectionFixture<GlobalServiceProvider>, ICollectionFixture<MefHostingFixture> {

    public const string CollectionName = nameof(VisualStudioTests);

}
