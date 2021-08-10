using System.Collections.Generic;
using System.Linq;
using static DuckDuckGo.Fluent.Plugin.JsonResult;
using static DuckDuckGo.Fluent.Plugin.AppFunctions;

namespace DuckDuckGo.Fluent.Plugin
{
    public class DuckResultFactory
    {
        private readonly Root _root;
        private readonly string _searchedText;

        private DuckResultFactory(Root root, string searchedText)
        {
            _root = root;
            _searchedText = searchedText;
        }

        public static DuckResultFactory Create(Root root, string searchedText)
        {
            return new DuckResultFactory(root, searchedText);
        }

        public DuckResult GetAnswers()
        {
            if (string.IsNullOrWhiteSpace(_root.Answer)) return null;
            string resultType = _root.AnswerType ?? "Answer";
            return new DuckResult
            {
                Info = _root.Answer, ResultType = resultType, SearchResultType = ResultType.Answer,
                SearchedText = _searchedText, Score = 10
            };
        }

        public DuckResult GetDictionary()
        {
            if (string.IsNullOrWhiteSpace(_root.Definition)) return null;
            return new DuckResult
            {
                Info = _root.Definition, ResultType = "Define", SourceUrl = _root.DefinitionUrl,
                SearchResultType = ResultType.Definition, SearchedText = _searchedText, Score = 9
            };
        }

        public DuckResult GetAbstract()
        {
            if (string.IsNullOrWhiteSpace(_root.AbstractText)) return null;
            return new DuckResult
            {
                Info = _root.AbstractText, ResultType = "Abstract", SourceUrl = _root.AbstractUrl,
                SearchResultType = ResultType.Abstract, SearchedText = _searchedText,
                Score = 8
            };
        }

        public List<DuckResult> GetRelatedSearch()
        {
            var list = new List<DuckResult>();
            if (_root.RelatedTopics == null) return list;

            foreach (RelatedTopic variableTopic in _root.RelatedTopics.Where(variableTopic =>
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
            if (_root.Results == null) return list;

            list.AddRange(_root.Results.Select(externalTopic => new DuckResult
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
