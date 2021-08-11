using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Blast.API.Core.Processes;
using Blast.API.Processes;
using Blast.Core.Interfaces;
using Blast.Core.Objects;
using Blast.Core.Results;
using TextCopy;
using static DuckDuckGo.Fluent.Plugin.DuckDuckGoSearchResult;
using static DuckDuckGo.Fluent.Plugin.JsonResult;
using static DuckDuckGo.Fluent.Plugin.AppFunctions;
using static DuckDuckGo.Fluent.Plugin.QrFunctions;

namespace DuckDuckGo.Fluent.Plugin
{
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
            string searchedTag = searchRequest.SearchedTag;
            string searchedText = searchRequest.SearchedText;

            if (!VerifySearchedTerms(searchedText, searchedTag)) yield break;
            searchedText = searchedText.Trim();

            if (!string.IsNullOrWhiteSpace(searchedTag) && searchedTag.Equals(QrTag))
            {
                DuckDuckGoSearchResult duckGoSearchResult = await GetQrImage(searchedText);
                if (duckGoSearchResult != null)
                    yield return duckGoSearchResult;
            }
            else
            {
                string url = GetEndpointUrl(searchedText);
                DuckDuckGoApiResult apiResult = await HttpCalls.GetApiResult(url);

                if (apiResult == null) yield break;

                var duckResultFactory = DuckResultFactory.Create(apiResult, searchedText);

                // Get Answers
                DuckResult answers = duckResultFactory.GetAnswers();
                if (answers != null) yield return GetISearchResult(answers);

                // Get Definitions if available
                DuckResult dictionary = duckResultFactory.GetDefinition();
                if (dictionary != null) yield return GetISearchResult(dictionary);

                // Get Abstract in Text form
                DuckResult abstractResult = duckResultFactory.GetAbstract();
                if (abstractResult != null)
                    yield return GetISearchResult(abstractResult);

                // External Links associated with search like Official Website etc.
                List<DuckResult> externalLinks = duckResultFactory.GetExternalLinks();
                foreach (DuckResult link in externalLinks) yield return GetISearchResult(link);

                // Internal Links associated with Search.
                List<DuckResult> internalLinks = duckResultFactory.GetRelatedTopics();
                foreach (DuckResult link in internalLinks) yield return GetISearchResult(link);
            }
        }

        public ValueTask LoadSearchApplicationAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "DuckDuckGo.Fluent.Plugin.duck_logo.png";
            var image = new Bitmap(assembly.GetManifestResourceStream(resourceName)!);
            _logoImage = new BitmapImageResult(image) { ScaleX = 1.3, ScaleY = 1.3 };
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
            DuckDuckGoApiResult apiResult = await HttpCalls.GetApiResult(url);

            if (apiResult == null) return default;

            var duckResultFactory = DuckResultFactory.Create(apiResult, duckResult.SearchedText);

            DuckResult result = duckResult.SearchResultType switch
            {
                ResultType.Answer => duckResultFactory.GetAnswers(),
                ResultType.Definition => duckResultFactory.GetDefinition(),
                ResultType.Abstract => duckResultFactory.GetAbstract(),
                ResultType.QrCode => null,
                ResultType.SearchResult => duckResult,
                _ => null
            };

            if (result == null) return default;

            return new DuckDuckGoSearchResult(result.Info, result.SearchedText, result.ResultType, DuckOperations,
                result.Score)
            {
                Url = result.SourceUrl, AdditionalInformation = result.SourceUrl, SearchObjectId = result,
                PreviewImage = _logoImage
            };
        }

        public ValueTask<IHandleResult> HandleSearchResult(ISearchResult searchResult)
        {
            if (searchResult is not DuckDuckGoSearchResult duckGoSearchResult)
                throw new InvalidCastException(nameof(DuckDuckGoSearchResult));

            string url = duckGoSearchResult.Url;
            string searchedTerm = duckGoSearchResult.SearchedText;

            if (string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(searchedTerm))
                url = DuckWebsiteUrl + "?q=" + WebUtility.UrlEncode(searchedTerm);

            if (duckGoSearchResult.SelectedOperation is DuckDuckGoSearchOperations duckGoSearchOperations)
            {
                IProcessManager managerInstance = ProcessUtils.GetManagerInstance();
                switch (duckGoSearchOperations.ActionType)
                {
                    case ActionType.OpenDuckDuckGo:
                        if (!string.IsNullOrWhiteSpace(url))
                            managerInstance.StartNewProcess(url);
                        break;

                    case ActionType.SaveImage:
                        SaveQrImage(duckGoSearchResult);
                        break;

                    default:
                        return default;
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(url))
                    Clipboard.SetText(url);
            }

            return new ValueTask<IHandleResult>(new HandleResult(true, false));
        }

        private DuckDuckGoSearchResult GetISearchResult(DuckResult duckResult)
        {
            return new DuckDuckGoSearchResult(duckResult.Info, duckResult.SearchedText, duckResult.ResultType,
                DuckOperations, duckResult.Score)
            {
                Url = duckResult.SourceUrl, AdditionalInformation = duckResult.SourceUrl,
                SearchObjectId = duckResult, PreviewImage = _logoImage
            };
        }
    }
}
