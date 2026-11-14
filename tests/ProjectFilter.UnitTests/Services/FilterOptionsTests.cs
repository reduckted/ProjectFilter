using NSubstitute;
using System;
using System.Collections.Generic;
using Xunit;


namespace ProjectFilter.Services;


public class FilterOptionsTests {

    private static readonly Guid ProjectA = new("{ab4185c2-aea1-46e9-b0d5-e33f897b4497}");
    private static readonly Guid ProjectB = new("{bbad5d46-fa84-4a78-be3f-b3bf07081f6c}");
    private static readonly Guid ProjectC = new("{cf28d290-8229-4a7f-8791-57f88793d623}");
    private static readonly Guid Project1 = new("{17313ef8-eb1e-4c49-b73a-aca3ba9d0448}");
    private static readonly Guid Project2 = new("{251d301a-6299-4a2a-8ca2-f7c70ecc75d0}");
    private static readonly Guid Project3 = new("{3d16a39b-8bf5-4f35-8d5b-5727e1377d90}");


    [Fact]
    public void StoresCopiesOfTheArguments() {
        FilterOptions options;
        List<Guid> loaded;
        List<Guid> unloaded;
        IExtensionSettings globalSettings;
        FilterBehavior behavior;


        loaded = [ProjectA, ProjectB];
        unloaded = [Project1, Project2];

        globalSettings = Substitute.For<IExtensionSettings>();
        behavior = new FilterBehavior(null, globalSettings);

        options = new FilterOptions(loaded, unloaded, behavior);

        loaded.Add(ProjectC);
        unloaded.Add(Project3);

        Assert.Equal([ProjectA, ProjectB], options.ProjectsToLoad);
        Assert.Equal([Project1, Project2], options.ProjectsToUnload);
        Assert.Same(behavior, options.Behavior);
    }

}
