using Microsoft.VisualStudio.Text;
using System.Collections.Immutable;
using System.Text.RegularExpressions;


namespace ProjectFilter.Services;


public class RegexTextFilter : ITextFilter {

    private readonly Regex _regex;


    public RegexTextFilter(string pattern) {
        _regex = new Regex(pattern, RegexOptions.IgnoreCase);
    }


    public ImmutableArray<Span> TryMatch(string text) {
        Match match;


        match = _regex.Match(text);

        return match.Success
            ? ImmutableArray.Create(Span.FromBounds(match.Index, match.Index + match.Length))
            : ImmutableArray<Span>.Empty;
    }

}
