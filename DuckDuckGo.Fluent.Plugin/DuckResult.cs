namespace DuckDuckGo.Fluent.Plugin
{
    public class DuckResult
    {
        public string Info { get; set; }
        public string ResultType { get; set; }
        public string SourceUrl { get; set; }
        public double Score { get; set; }
        public string SearchedText { get; set; }
        public ResultType SearchResultType { get; set; }
    }
}
