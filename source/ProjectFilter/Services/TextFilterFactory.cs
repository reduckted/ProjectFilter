namespace ProjectFilter.Services;


public delegate ITextFilter TextFilterFactory(string pattern, bool isRegularExpression);
