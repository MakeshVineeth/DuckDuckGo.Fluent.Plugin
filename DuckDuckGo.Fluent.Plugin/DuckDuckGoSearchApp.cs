﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
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

namespace DuckDuckGo.Fluent.Plugin
{
    internal class DuckDuckGoSearchApp : ISearchApplication
    {
        private const string SearchAppName = "DuckDuckGo Instant Answers";
        public const string UserAgentString = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
        public static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };
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

            if (!VerifySearchedTerms(ref searchedText, ref searchedTag)) yield break;

            if (searchedTag.Equals(QrTag))
            {
                DuckDuckGoSearchResult duckGoSearchResult = await GetQrImage(searchedText);
                if (duckGoSearchResult != null)
                    yield return duckGoSearchResult;
            }
            else
            {
                string url = GetEndpointUrl(searchedText);
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgentString);

                var root = await httpClient.GetFromJsonAsync<Root>(url, SerializerOptions, cancellationToken);
                var duckResultFactory = DuckResultFactory.Create(root, searchedText);

                // Get Answers
                DuckResult answers = duckResultFactory.GetAnswers();
                if (answers != null) yield return GetISearchResult(answers);

                // Get Definitions if available
                DuckResult dictionary = duckResultFactory.GetDictionary();
                if (dictionary != null) yield return GetISearchResult(dictionary);

                // Get Abstract in Text form
                DuckResult abstractResult = duckResultFactory.GetAbstract();
                if (abstractResult != null)
                    yield return GetISearchResult(abstractResult);

                // External Links associated with search like Official Website etc.
                List<DuckResult> externalLinks = duckResultFactory.GetExternalLinks();
                foreach (DuckResult externalLink in externalLinks) yield return GetISearchResult(externalLink);

                // Internal Links associated with Search.
                List<DuckResult> internalLinks = duckResultFactory.GetRelatedSearch();
                foreach (DuckResult internalLink in internalLinks) yield return GetISearchResult(internalLink);
            }
        }

        public ValueTask LoadSearchApplicationAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "DuckDuckGo.Fluent.Plugin.duck_logo.png";
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            var image = new Bitmap(stream!);
            _logoImage = new BitmapImageResult(image) { ScaleX = 1.3, ScaleY = 1.3 };
            return ValueTask.CompletedTask;
        }

        public async ValueTask<ISearchResult> GetSearchResultForId(object searchObjectId)
        {
            DuckResult duckResult;
            switch (searchObjectId)
            {
                case string json:
                    duckResult = JsonSerializer.Deserialize<DuckResult>(json);
                    break;
                case DuckResult objectId:
                    duckResult = objectId;
                    break;
                default:
                    return default;
            }

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

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgentString);
            string url = GetEndpointUrl(duckResult.SearchedText);
            var root = await httpClient.GetFromJsonAsync<Root>(url, SerializerOptions);
            var duckResultFactory = DuckResultFactory.Create(root, duckResult.SearchedText);

            DuckResult result = duckResult.SearchResultType switch
            {
                ResultType.Answer => duckResultFactory.GetAnswers(),
                ResultType.Definition => duckResultFactory.GetDictionary(),
                ResultType.Abstract => duckResultFactory.GetAbstract(),
                ResultType.QrCode => null,
                ResultType.SearchResult => duckResult,
                _ => null
            };

            if (result == null) return default;

            DuckDuckGoSearchResult duckDuckGoSearchResult =
                new(result.Info, result.SearchedText, result.ResultType, DuckOperations, result.Score)
                {
                    Url = result.SourceUrl, AdditionalInformation = result.SourceUrl, SearchObjectId = result,
                    PreviewImage = _logoImage
                };

            return duckDuckGoSearchResult;
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