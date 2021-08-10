using System.Collections.Generic;
using System.Linq;
using static DuckDuckGo.Fluent.Plugin.JsonResult;

namespace DuckDuckGo.Fluent.Plugin
{
    public class DuckResultFactory
    {
        private readonly DuckDuckGoApiResult _apiResult;
        private readonly string _searchedText;

        private DuckResultFactory(DuckDuckGoApiResult apiResult, string searchedText)
        {
            _apiResult = apiResult;
            _searchedText = searchedText;
        }

        public static DuckResultFactory Create(DuckDuckGoApiResult root, string searchedText)
        {
            return new DuckResultFactory(root, searchedText);
        }

        public DuckResult GetAnswers()
        {
            if (string.IsNullOrWhiteSpace(_apiResult.Answer)) return null;
            string resultType = _apiResult.AnswerType ?? "Answer";
            return new DuckResult
            {
                Info = _apiResult.Answer, ResultType = resultType, SearchResultType = ResultType.Answer,
                SearchedText = _searchedText, Score = 10
            };
        }

        public DuckResult GetDefinition()
        {
            if (string.IsNullOrWhiteSpace(_apiResult.Definition)) return null;
            return new DuckResult
            {
                Info = _apiResult.Definition, ResultType = "Define", SourceUrl = _apiResult.DefinitionUrl,
                SearchResultType = ResultType.Definition, SearchedText = _searchedText, Score = 9
            };
        }

        public DuckResult GetAbstract()
        {
            if (string.IsNullOrWhiteSpace(_apiResult.AbstractText)) return null;
            return new DuckResult
            {
                Info = _apiResult.AbstractText, ResultType = "Abstract", SourceUrl = _apiResult.AbstractUrl,
                SearchResultType = ResultType.Abstract, SearchedText = _searchedText,
                Score = 8
            };
        }

        public List<DuckResult> GetRelatedTopics()
        {
            var list = new List<DuckResult>();
            if (_apiResult.RelatedTopics == null) return list;

            foreach (RelatedTopic variableTopic in _apiResult.RelatedTopics.Where(variableTopic =>
                variableTopic != null))
            {
                if (!string.IsNullOrWhiteSpace(variableTopic.Text))
                    list.Add(new DuckResult
                    {
                        Info = variableTopic.Text, SourceUrl = variableTopic.FirstUrl, ResultType = "Related",
                        Score = 3,
                        SearchResultType = ResultType.SearchResult, SearchedText = _searchedText
                    });

                if (variableTopic.Topics == null) continue;

                list.AddRange(variableTopic.Topics.Select(topic => new DuckResult
                {
                    Info = topic.Text,
                    SourceUrl = topic.FirstUrl,
                    ResultType = variableTopic.Name,
                    Score = 2,
                    SearchResultType = ResultType.SearchResult,
                    SearchedText = _searchedText
                }));
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
    }
}