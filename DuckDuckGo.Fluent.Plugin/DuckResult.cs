using System;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace DuckDuckGo.Fluent.Plugin;

public class DuckResult
{
    public string Info { get; set; }
    public string ResultType { get; set; }
    public string SourceUrl { get; set; }
    public string SearchedText { get; set; }
    public ResultType SearchResultType { get; set; }
    public bool IsPinned { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is not DuckResult duckResult) return false;

        var t1 = (duckResult.Info, duckResult.ResultType, duckResult.SourceUrl,
                duckResult.SearchedText, duckResult.SearchResultType, duckResult.IsPinned);

        var t2 = (Info, ResultType,
                SourceUrl, SearchedText, SearchResultType, IsPinned);

        return t1 == t2;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Info, ResultType, SourceUrl, SearchedText, SearchResultType, IsPinned);
    }
}
