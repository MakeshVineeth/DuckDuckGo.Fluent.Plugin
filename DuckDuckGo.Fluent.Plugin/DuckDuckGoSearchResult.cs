using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Input;
using Blast.API.Search.SearchOperations;
using Blast.Core.Interfaces;
using Blast.Core.Results;
using static DuckDuckGo.Fluent.Plugin.DuckDuckGoSearchOperation;

namespace DuckDuckGo.Fluent.Plugin
{
    public sealed class DuckDuckGoSearchResult : SearchResultBase
    {
        public const string QrTag = "qrcode";
        private const string QrTagDescription = "Display QR Code of Images";
        public const string DuckSearchTagName = "duck";
        public const string DuckTagDescription = "Show DuckDuckGo Instant Answers";
        public const string SearchResultIcon = "\uF78B";

        public static readonly CopySearchOperation CopyUrl = new("Copy URL")
            { Description = "Copies the URL to Clipboard.", KeyGesture = new KeyGesture(Key.None) };

        public static readonly CopySearchOperation CopyContents = new("Copy Contents")
        {
            Description = "Copies the Content of the Result"
        };

        public static readonly ObservableCollection<ISearchOperation> SearchOperations = new()
        {
            OpenDuck,
            CopyUrl,
            SaveImageOperation,
            CopyContents
        };

        public static readonly ObservableCollection<ISearchOperation> DuckOperations = new()
        {
            OpenDuck,
            CopyUrl,
            CopyContents
        };

        public static readonly ObservableCollection<ISearchOperation> QrOperations = new()
        {
            SaveImageOperation
        };

        public static readonly ObservableCollection<SearchTag> SearchTags = new()
        {
            new SearchTag
            {
                Name = DuckSearchTagName,
                IconGlyph = SearchResultIcon,
                Description = DuckTagDescription
            },
            new SearchTag
            {
                Name = QrTag,
                IconGlyph = SearchResultIcon,
                Description = QrTagDescription
            }
        };

        public DuckDuckGoSearchResult(string resultName, string searchedText, string resultType,
            IList<ISearchOperation> supportedOperationCollections, double score, DuckResult duckResult) :
            base(DuckSearchTagName, resultName, searchedText, resultType, score,
                supportedOperationCollections, SearchTags)
        {
            SearchObjectId = duckResult;
        }

        public override string Context => Url;

        public string Url { get; set; }

        protected override void OnSelectedSearchResultChanged()
        {
        }
    }
}
