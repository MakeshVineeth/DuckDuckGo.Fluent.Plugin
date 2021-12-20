using System.Collections.Generic;
using System.Linq;
using static DuckDuckGo.Fluent.Plugin.JsonResult;
using static DuckDuckGo.Fluent.Plugin.AppFunctions;

namespace DuckDuckGo.Fluent.Plugin;

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

    public DuckResult GetInstantAnswer(ResultType resultType)
    {
        string info = string.Empty;
        string resultTypeStr = string.Empty;
        string sourceUrl = string.Empty;

        switch (resultType)
        {
            case ResultType.Answer:
                info = _apiResult.Answer;
                resultTypeStr = _apiResult.AnswerType ?? "Answer";
                sourceUrl = null;
                break;
            case ResultType.Definition:
                info = _apiResult.Definition;
                resultTypeStr = "Define";
                sourceUrl = _apiResult.DefinitionUrl;
                break;
            case ResultType.Abstract:
                info = _apiResult.AbstractText;
                resultTypeStr = "Abstract";
                sourceUrl = _apiResult.AbstractUrl;
                break;
        }

        if (string.IsNullOrWhiteSpace(info)) return null;

        return CreateDuckResult(info, resultTypeStr, sourceUrl, resultType,
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
                yield return CreateDuckResult(
                    topic.Text,
                    variableTopic.Name,
                    topic.FirstUrl,
                    ResultType.SearchResult
                );
        }
    }

    public IEnumerable<DuckResult> GetExternalLinks()
    {
        if (_apiResult.Results == null) yield break;

        foreach (RelatedTopic externalTopic in _apiResult.Results)
            yield return CreateDuckResult(externalTopic.Text, "Links", externalTopic.FirstUrl,
                ResultType.SearchResult);
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
