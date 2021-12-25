using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Blast.API.Core.Processes;
using Blast.API.Processes;
using Blast.Core.Interfaces;
using Blast.Core.Objects;
using Blast.Core.Results;
using static DuckDuckGo.Fluent.Plugin.DuckDuckGoSearchResult;
using static DuckDuckGo.Fluent.Plugin.JsonResult;
using static DuckDuckGo.Fluent.Plugin.AppFunctions;
using static DuckDuckGo.Fluent.Plugin.QrFunctions;

namespace DuckDuckGo.Fluent.Plugin;

internal class DuckDuckGoSearchApp : ISearchApplication
{
    private const string SearchAppName = "DuckDuckGo Instant Answers";
    private readonly SearchApplicationInfo _applicationInfo;
    private BitmapImageResult _logoImage;

    public DuckDuckGoSearchApp()
    {
        _applicationInfo = new SearchApplicationInfo(SearchAppName,
            DuckTagDescription, SearchOperations)
        {
            MinimumSearchLength = 1,
            IsProcessSearchEnabled = false,
            IsProcessSearchOffline = false,
            SearchTagOnly = true,
            ApplicationIconGlyph = SearchResultIcon,
            SearchAllTime = ApplicationSearchTime.Fast,
            DefaultSearchTags = SearchTags
        };
    }

    public SearchApplicationInfo GetApplicationInfo()
    {
        return _applicationInfo;
    }

    public async IAsyncEnumerable<ISearchResult> SearchAsync(SearchRequest searchRequest,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string searchedTag = searchRequest.SearchedTag?.Trim();
        string searchedText = searchRequest.SearchedText?.Trim();

        SearchAction searchAction = VerifySearchedTerms(searchedText, searchedTag);
        switch (searchAction)
        {
            case SearchAction.Normal:
                string url = GetEndpointUrl(searchedText);
                DuckDuckGoApiResult apiResult = await HttpCalls.GetApiResult(url, cancellationToken);

                if (apiResult == null) yield break;

                var duckResultFactory = DuckResultFactory.Create(apiResult, searchedText);

                // Get an Instant Answer
                DuckResult answers = duckResultFactory.GetInstantAnswer(ResultType.Answer);
                if (answers != null) yield return GetISearchResult(answers);

                // Get Definitions if available
                DuckResult dictionary = duckResultFactory.GetInstantAnswer(ResultType.Definition);
                if (dictionary != null) yield return GetISearchResult(dictionary);

                // Get Abstract in Text form
                DuckResult abstractResult = duckResultFactory.GetInstantAnswer(ResultType.Abstract);
                if (abstractResult != null)
                    yield return GetISearchResult(abstractResult);

                // External Links associated with search like Official Website etc.
                IEnumerable<DuckResult> externalLinks = duckResultFactory.GetExternalLinks();
                foreach (DuckResult link in externalLinks) yield return GetISearchResult(link);

                // Internal Links associated with Search.
                IEnumerable<DuckResult> internalLinks = duckResultFactory.GetRelatedTopics();
                foreach (DuckResult link in internalLinks) yield return GetISearchResult(link);
                break;

            case SearchAction.QrCode:
                DuckDuckGoSearchResult duckGoSearchResult = await GetQrImage(searchedText);
                if (duckGoSearchResult != null)
                    yield return duckGoSearchResult;
                break;

            case SearchAction.Null:
                yield break;

            default:
                yield break;
        }
    }

    public ValueTask LoadSearchApplicationAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "DuckDuckGo.Fluent.Plugin.duck_logo.png";
        var image = new Bitmap(assembly.GetManifestResourceStream(resourceName)!);
        _logoImage = new BitmapImageResult(image);
        return ValueTask.CompletedTask;
    }

    public async ValueTask<ISearchResult> GetSearchResultForId(object searchObjectId)
    {
        DuckResult duckResult = searchObjectId switch
        {
            string json => JsonSerializer.Deserialize<DuckResult>(json),
            DuckResult objectId => objectId,
            _ => null
        };

        switch (duckResult)
        {
            case null:
                return default;
            case { SearchResultType: ResultType.QrCode }:
            {
                DuckDuckGoSearchResult duckGoSearchResult = await GetQrImage(duckResult.SearchedText);
                return duckGoSearchResult;
            }
        }

        string url = GetEndpointUrl(duckResult.SearchedText);
        DuckDuckGoApiResult apiResult = await HttpCalls.GetApiResult(url, default);

        if (apiResult == null) return default;

        var duckResultFactory = DuckResultFactory.Create(apiResult, duckResult.SearchedText);

        DuckResult result = duckResult.SearchResultType switch
        {
            ResultType.Answer => duckResult, // Answers will often have numerical computations and are not suitable for custom tags as they often update very frequently.
            ResultType.Definition => duckResultFactory.GetInstantAnswer(ResultType.Definition),
            ResultType.Abstract => duckResultFactory.GetInstantAnswer(ResultType.Abstract),
            ResultType.QrCode => null,
            ResultType.SearchResult => duckResult,
            _ => null
        };

        return result == null ? default(ISearchResult) : GetISearchResult(result);
    }

    public ValueTask<IHandleResult> HandleSearchResult(ISearchResult searchResult)
    {
        if (searchResult is not DuckDuckGoSearchResult duckGoSearchResult)
            throw new InvalidCastException(nameof(DuckDuckGoSearchResult));

        string url = duckGoSearchResult.Url;

        if (duckGoSearchResult.SelectedOperation is DuckDuckGoSearchOperation duckGoSearchOperations)
        {
            IProcessManager managerInstance = ProcessUtils.GetManagerInstance();
            switch (duckGoSearchOperations.ActionType)
            {
                case ActionType.OpenDuckDuckGo:
                    if (!string.IsNullOrWhiteSpace(url))
                        managerInstance.StartNewProcess(url);
                    break;

                case ActionType.SaveImage:
                    SaveQrImage(duckGoSearchResult, default);
                    break;

                default:
                    return default;
            }
        }

        return new ValueTask<IHandleResult>(new HandleResult(true, false));
    }

    private DuckDuckGoSearchResult GetISearchResult(DuckResult duckResult)
    {
        double score = duckResult.Score;
        return new DuckDuckGoSearchResult(duckResult.Info, duckResult.SearchedText, duckResult.ResultType,
            DuckOperations, score, duckResult)
        {
            Url = duckResult.SourceUrl, AdditionalInformation = duckResult.SourceUrl,
            PreviewImage = _logoImage
        };
    }
}