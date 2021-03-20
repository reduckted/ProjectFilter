using Xunit;


namespace ProjectFilter.Helpers {

    [CollectionDefinition(Name, DisableParallelization = true)]
    public static class VisualStudioTests {

        public const string Name = nameof(VisualStudioTests);

    }

}
