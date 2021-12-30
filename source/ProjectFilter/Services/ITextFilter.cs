using Microsoft.VisualStudio.Text;
using System.Collections.Immutable;


namespace ProjectFilter.Services;


public interface ITextFilter {

    ImmutableArray<Span> TryMatch(string text);

}
