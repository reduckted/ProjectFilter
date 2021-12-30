using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.PatternMatching;
using System;
using System.Collections.Immutable;
using System.Globalization;


namespace ProjectFilter.Services;


public class PatternTextFilter : ITextFilter {

    private readonly IPatternMatcher _matcher;


    public PatternTextFilter(string pattern, IPatternMatcherFactory factory) {
        if (factory is null) {
            throw new ArgumentNullException(nameof(factory));
        }

        _matcher = factory.CreatePatternMatcher(
            pattern,
            new PatternMatcherCreationOptions(
                CultureInfo.CurrentCulture,
                PatternMatcherCreationFlags.AllowSimpleSubstringMatching | PatternMatcherCreationFlags.IncludeMatchedSpans
            )
        );
    }


    public ImmutableArray<Span> TryMatch(string text) {
        return _matcher.TryMatch(text)?.MatchedSpans ?? ImmutableArray<Span>.Empty;
    }

}
