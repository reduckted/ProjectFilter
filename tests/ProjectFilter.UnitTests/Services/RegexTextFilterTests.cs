using Microsoft.VisualStudio.Text;
using ProjectFilter.Services;
using System.Collections.Immutable;
using System.Linq;
using Xunit;


namespace ProjectFilter.UI;


public class RegexTextFilterTests {

    [Theory]
    [InlineData("Bar", "Foo")]
    [InlineData("Foo", "[Foo]BarFooBar")]
    [InlineData("Bar", "Foo[Bar]FooBar")]
    [InlineData("(?<!^)Foo", "FooBar[Foo]Bar")]
    [InlineData("Bar$", "FooBarFoo[Bar]")]
    [InlineData("B.+B", "Foo[BarFooB]ar")]
    [InlineData("bAR", "Foo[Bar]")]
    public void CanMatchUsingRegularExpression(string pattern, string text) {
        ITextFilter filter;
        ImmutableArray<Span> expectedSpans;
        int startIndex;


        filter = new RegexTextFilter(pattern);

        startIndex = text.IndexOf('[');

        if (startIndex >= 0) {
            int endIndex;


            text = text.Remove(startIndex, 1);
            endIndex = text.IndexOf(']');

            text = text.Remove(endIndex, 1);
            expectedSpans = ImmutableArray.Create(Span.FromBounds(startIndex, endIndex));

        } else {
            expectedSpans = ImmutableArray<Span>.Empty;
        }

        Assert.Equal(expectedSpans.AsEnumerable(), filter.TryMatch(text).AsEnumerable());
    }

}
