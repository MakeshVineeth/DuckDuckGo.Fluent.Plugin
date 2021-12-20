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
        if (obj == null || GetType() != obj.GetType()) return false;

        var duckResult = (DuckResult)obj;
        return Info.Equals(duckResult.Info) && ResultType.Equals(duckResult.ResultType) &&
               SourceUrl.Equals(duckResult.SourceUrl)
               && SearchedText.Equals(duckResult.SearchedText) && SearchResultType == duckResult.SearchResultType;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Info, ResultType, SourceUrl, SearchedText, SearchResultType, IsPinned);
    }
}
