using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
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
        public static readonly JsonSerializerOptions SerializerOptions = new() {PropertyNameCaseInsensitive = true};
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
                var channel = Channel.CreateUnbounded<DuckResult>();
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgentString);

                _ = httpClient.GetFromJsonAsync<Root>(url, SerializerOptions, cancellationToken).ContinueWith(
                    async task =>
                    {
                        if (task.IsCompletedSuccessfully && task.Result != null)
                        {
                            Root root = task.Result;

                            // Get Answers
                            DuckResult answers = GetAnswers(root, searchedText);
                            if (answers != null) await channel.Writer.WriteAsync(answers, cancellationToken);

                            // Get Definitions if available
                            DuckResult dictionary = GetDictionary(root, searchedText);
                            if (dictionary != null) await channel.Writer.WriteAsync(dictionary, cancellationToken);

                            // Get Abstract in Text form
                            DuckResult abstractResult = GetAbstract(root, searchedText);
                            if (abstractResult != null)
                                await channel.Writer.WriteAsync(abstractResult, cancellationToken);

                            // External Links associated with search like Official Website etc.
                            if (root?.Results != null)
                                foreach (DuckResult duckResult in root.Results.Select(variableTopic => new DuckResult
                                {
                                    Info = variableTopic.Text, SourceUrl = variableTopic.FirstUrl,
                                    ResultType = "Links",
                                    SearchedText = searchedText, SearchResultType = ResultType.SearchResult, Score = 7
                                }))
                                    await channel.Writer.WriteAsync(duckResult, CancellationToken.None);

                            // Internal Links associated with Search.
                            await GetRelatedSearch(root, channel, searchedText);
                        }

                        // Finish channel if task is null.
                        else
                        {
                            channel.Writer.Complete();
                        }
                    }, cancellationToken).ContinueWith(_ => channel.Writer.Complete(), CancellationToken.None);

                await foreach (DuckResult duckResult in channel.Reader.ReadAllAsync(cancellationToken))
                    yield return new DuckDuckGoSearchResult(duckResult.Info, searchedText, duckResult.ResultType,
                        DuckOperations, duckResult.Score)
                    {
                        Url = duckResult.SourceUrl, AdditionalInformation = duckResult.SourceUrl,
                        SearchObjectId = duckResult, PreviewImage = _logoImage
                    };
            }
        }

        public ValueTask LoadSearchApplicationAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "DuckDuckGo.Fluent.Plugin.duck_logo.png";
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            var image = new Bitmap(stream!);
            _logoImage = new BitmapImageResult(image) {ScaleX = 1.3, ScaleY = 1.3};
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
                case {SearchResultType: ResultType.QrCode}:
                {
                    DuckDuckGoSearchResult duckGoSearchResult = await GetQrImage(duckResult.SearchedText);
                    return duckGoSearchResult;
                }
            }

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgentString);
            string url = GetEndpointUrl(duckResult.SearchedText);
            var root = await httpClient.GetFromJsonAsync<Root>(url, SerializerOptions);

            DuckResult result = duckResult.SearchResultType switch
            {
                ResultType.Answer => GetAnswers(root, duckResult.SearchedText),
                ResultType.Definition => GetDictionary(root, duckResult.SearchedText),
                ResultType.Abstract => GetAbstract(root, duckResult.SearchedText),
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
    }
}
