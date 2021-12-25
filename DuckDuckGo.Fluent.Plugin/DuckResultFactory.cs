using System.Collections.Generic;
using System.Linq;
using Blast.API.Search;
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

        double score = info.SearchDistanceScore(_searchedText) * 10;
        return CreateDuckResult(info, resultTypeStr, sourceUrl, resultType, score);
    }

    public IEnumerable<DuckResult> GetRelatedTopics()
    {
        if (_apiResult.RelatedTopics == null) yield break;

        foreach (RelatedTopic variableTopic in _apiResult.RelatedTopics.Where(variableTopic =>
                     variableTopic != null))
        {
            if (!string.IsNullOrWhiteSpace(variableTopic.Text))
            {
                string description = variableTopic.Text;
                double score = description.SearchDistanceScore(_searchedText);
                yield return CreateDuckResult(description, "Related", variableTopic.FirstUrl,
                    ResultType.SearchResult, score);
            }

            if (variableTopic.Topics == null) continue;

            foreach (Topic topic in variableTopic.Topics)
            {
                string description = topic.Text;
                double score = 5;
                yield return CreateDuckResult(
                    description,
                    variableTopic.Name,
                    topic.FirstUrl,
                    ResultType.SearchResult, score
                );
            }
        }
    }

    public IEnumerable<DuckResult> GetExternalLinks()
    {
        if (_apiResult.Results == null) yield break;

        foreach (RelatedTopic externalTopic in _apiResult.Results)
        {
            string description = externalTopic.Text;
            double score = 3;
            yield return CreateDuckResult(description, "Links", externalTopic.FirstUrl,
                ResultType.SearchResult, score);
        }
    }

    private DuckResult CreateDuckResult(string info, string resultType, string sourceUrl,
        ResultType searchResultType, double score)
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
            Score = score
        };
    }
}
