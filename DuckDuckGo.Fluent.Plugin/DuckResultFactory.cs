using System.Collections.Generic;
using System.Linq;
using Blast.Core.Results;
using static DuckDuckGo.Fluent.Plugin.JsonResult;
using static DuckDuckGo.Fluent.Plugin.DuckDuckGoSearchResult;

namespace DuckDuckGo.Fluent.Plugin
{
    public class DuckResultFactory
    {
        private readonly DuckDuckGoApiResult _apiResult;
        private readonly BitmapImageResult _logoImage;
        private readonly string _searchedText;

        private DuckResultFactory(DuckDuckGoApiResult apiResult, string searchedText, BitmapImageResult logoImage)
        {
            _apiResult = apiResult;
            _searchedText = searchedText;
            _logoImage = logoImage;
        }

        public static DuckResultFactory Create(DuckDuckGoApiResult root, string searchedText,
            BitmapImageResult logoImage)
        {
            return new DuckResultFactory(root, searchedText, logoImage);
        }

        public DuckResult GetAnswers()
        {
            if (string.IsNullOrWhiteSpace(_apiResult.Answer)) return null;
            string resultType = _apiResult.AnswerType ?? "Answer";
            return CreateDuckResult(_apiResult.Answer, resultType, null, ResultType.Answer, 10);
        }

        public DuckResult GetDefinition()
        {
            return string.IsNullOrWhiteSpace(_apiResult.Definition)
                ? null
                : CreateDuckResult(_apiResult.Definition, "Define", _apiResult.DefinitionUrl, ResultType.Definition, 9);
        }

        public DuckResult GetAbstract()
        {
            return string.IsNullOrWhiteSpace(_apiResult.AbstractText)
                ? null
                : CreateDuckResult(_apiResult.AbstractText, "Abstract", _apiResult.AbstractUrl, ResultType.Abstract, 8);
        }

        public List<DuckResult> GetRelatedTopics()
        {
            var list = new List<DuckResult>();
            if (_apiResult.RelatedTopics == null) return list;

            foreach (RelatedTopic variableTopic in _apiResult.RelatedTopics.Where(variableTopic =>
                variableTopic != null))
            {
                if (!string.IsNullOrWhiteSpace(variableTopic.Text))
                    list.Add(CreateDuckResult(variableTopic.Text, "Related", variableTopic.FirstUrl,
                        ResultType.SearchResult, 3));

                if (variableTopic.Topics == null) continue;

                list.AddRange(variableTopic.Topics.Select(topic => CreateDuckResult(
                    topic.Text,
                    variableTopic.Name,
                    topic.FirstUrl,
                    ResultType.SearchResult,
                    2
                )));
            }

            return list;
        }

        public List<DuckResult> GetExternalLinks()
        {
            var list = new List<DuckResult>();
            if (_apiResult.Results == null) return list;

            list.AddRange(_apiResult.Results.Select(externalTopic => new DuckResult
            {
                Info = externalTopic.Text,
                SourceUrl = externalTopic.FirstUrl,
                ResultType = "Links",
                SearchedText = _searchedText,
                SearchResultType = ResultType.SearchResult,
                Score = 7
            }));

            return list;
        }

        private DuckResult CreateDuckResult(string info, string resultType, string sourceUrl,
            ResultType searchResultType, double score)
        {
            return new DuckResult
            {
                Info = info,
                ResultType = resultType,
                SearchResultType = searchResultType,
                Score = score,
                SourceUrl = sourceUrl,
                SearchedText = _searchedText
            };
        }

        public DuckDuckGoSearchResult GetISearchResult(DuckResult duckResult)
        {
            return new DuckDuckGoSearchResult(duckResult.Info, _searchedText, duckResult.ResultType,
                DuckOperations, duckResult.Score)
            {
                Url = duckResult.SourceUrl, AdditionalInformation = duckResult.SourceUrl,
                SearchObjectId = duckResult, PreviewImage = _logoImage
            };
        }
    }
}
