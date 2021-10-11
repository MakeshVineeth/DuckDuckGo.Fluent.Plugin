using System;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace DuckDuckGo.Fluent.Plugin
{
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
            return obj is DuckResult duckResult && (duckResult.Info, duckResult.ResultType, duckResult.SourceUrl,
                duckResult.SearchedText, duckResult.SearchResultType, duckResult.IsPinned).Equals((Info, ResultType,
                SourceUrl, SearchedText, SearchResultType, IsPinned));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Info, ResultType, SourceUrl, SearchedText, SearchResultType, IsPinned);
        }
    }
}
