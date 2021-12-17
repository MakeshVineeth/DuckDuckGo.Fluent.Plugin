using System.Collections.Generic;
using System.Linq;
using static DuckDuckGo.Fluent.Plugin.JsonResult;
using static DuckDuckGo.Fluent.Plugin.AppFunctions;

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

        public DuckResult GetAnswer()
        {
            if (string.IsNullOrWhiteSpace(_apiResult.Answer)) return null;
            string resultType = _apiResult.AnswerType ?? "Answer";
            return CreateDuckResult(_apiResult.Answer, resultType, null, ResultType.Answer, true);
        }

        public DuckResult GetDefinition()
        {
            return string.IsNullOrWhiteSpace(_apiResult.Definition)
                ? null
                : CreateDuckResult(_apiResult.Definition, "Define", _apiResult.DefinitionUrl, ResultType.Definition,
                    true);
        }

        public DuckResult GetAbstract()
        {
            return string.IsNullOrWhiteSpace(_apiResult.AbstractText)
                ? null
                : CreateDuckResult(_apiResult.AbstractText, "Abstract", _apiResult.AbstractUrl, ResultType.Abstract,
                    true);
        }

        public IEnumerable<DuckResult> GetRelatedTopics()
        {
            if (_apiResult.RelatedTopics == null) yield break;

            foreach (RelatedTopic variableTopic in _apiResult.RelatedTopics.Where(variableTopic =>
                         variableTopic != null))
            {
                if (!string.IsNullOrWhiteSpace(variableTopic.Text))
                    yield return CreateDuckResult(variableTopic.Text, "Related", variableTopic.FirstUrl,
                        ResultType.SearchResult);

                if (variableTopic.Topics == null) continue;

                foreach (Topic topic in variableTopic.Topics)
                {
                    yield return CreateDuckResult(
                        topic.Text,
                        variableTopic.Name,
                        topic.FirstUrl,
                        ResultType.SearchResult
                    );
                }
            }
        }

        public List<DuckResult> GetExternalLinks()
        {
            var list = new List<DuckResult>();
            if (_apiResult.Results == null) return list;

            list.AddRange(_apiResult.Results.Select(externalTopic =>
                CreateDuckResult(externalTopic.Text, "Links", externalTopic.FirstUrl, ResultType.SearchResult)
            ));

            return list;
        }

        private DuckResult CreateDuckResult(string info, string resultType, string sourceUrl,
            ResultType searchResultType, bool isPinned = false)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl) && !string.IsNullOrWhiteSpace(_searchedText))
                sourceUrl = GetGeneralizedUrl(_searchedText);

            return new DuckResult
            {
                Info = info,
                ResultType = resultType,
                SearchResultType = searchResultType,
                SourceUrl = sourceUrl,
                SearchedText = _searchedText,
                IsPinned = isPinned
            };
        }
    }
}
