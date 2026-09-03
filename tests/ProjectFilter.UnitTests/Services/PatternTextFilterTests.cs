using Microsoft.VisualStudio.Text;
using ProjectFilter.Helpers;
using Xunit;


namespace ProjectFilter.Services;


public class PatternTextFilterTests {

    [Fact]
    public void ReturnsMatchesFromPatternMatcherWhenNotUsingRegularExpression() {
        Assert.Equal(
            new[] { Span.FromBounds(1, 3) },
            new PatternTextFilter("foo", Factory.CreatePatternMatcherFactory([Span.FromBounds(1, 3)])).TryMatch("foo")
        );

        Assert.Empty(
            new PatternTextFilter("foo", Factory.CreatePatternMatcherFactory([])).TryMatch("bar")
        );
    }

}
