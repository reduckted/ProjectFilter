using Microsoft.VisualStudio.Text;
using ProjectFilter.Helpers;
using ProjectFilter.Services;
using System.Linq;
using Xunit;


namespace ProjectFilter.UI;


public class PatternTextFilterTests {

    [Fact]
    public void ReturnsMatchesFromPatternMatcherWhenNotUsingRegularExpression() {
        Assert.Equal(
            new[] { Span.FromBounds(1, 3) },
            new PatternTextFilter("foo", Factory.CreatePatternMatcherFactory(new[] { Span.FromBounds(1, 3) })).TryMatch("foo")
        );

        Assert.Empty(
            new PatternTextFilter("foo", Factory.CreatePatternMatcherFactory(Enumerable.Empty<Span>())).TryMatch("bar")
        );
    }

}
