using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Blast.API.Core.Processes;
using Blast.API.Graphics;
using Blast.API.Processes;
using Blast.Core.Results;
using SoftCircuits.HtmlMonkey;
using static DuckDuckGo.Fluent.Plugin.JsonResult;
using static DuckDuckGo.Fluent.Plugin.DuckDuckGoSearchResult;
using static DuckDuckGo.Fluent.Plugin.DuckDuckGoSearchApp;

namespace DuckDuckGo.Fluent.Plugin
{
    public class AppFunctions
    {
        public enum ResultType
        {
            Answer,
            Definition,
            Abstract,
            QrCode,
            SearchResult
        }

        private const string Endpoint = "https://api.duckduckgo.com/?q=";
        public const string DuckWebsiteUrl = "https://duckduckgo.com/";

        public static string GetEndpointUrl(string searchedText)
        {
            return Endpoint + WebUtility.UrlEncode(searchedText) + "&format=json&no_html=1";
        }

        public static bool VerifySearchedTerms(ref string searchedText, ref string searchedTag)
        {
            searchedText = searchedText.Trim();

            if (string.IsNullOrWhiteSpace(searchedText)) return false;

            if (string.IsNullOrWhiteSpace(searchedTag)) return true;

            return searchedTag.Equals(DuckSearchTagName, StringComparison.Ordinal) ||
                   searchedTag.Equals(QrTag, StringComparison.Ordinal);
        }

        public static async Task GetRelatedSearch(Root root, Channel<DuckResult> channel, string searchedText)
        {
            if (root?.RelatedTopics != null)
                foreach (RelatedTopic variableTopic in root.RelatedTopics.Where(variableTopic =>
                    variableTopic != null))
                {
                    if (!string.IsNullOrWhiteSpace(variableTopic.Text))
                        await channel.Writer.WriteAsync(new DuckResult
                        {
                            Info = variableTopic.Text, SourceUrl = variableTopic.FirstUrl, ResultType = "Related",
                            Score = 3,
                            SearchResultType = ResultType.SearchResult, SearchedText = searchedText
                        }, CancellationToken.None);

                    if (variableTopic.Topics == null) continue;

                    foreach (DuckResult duckResult in from topic in variableTopic.Topics
                        where !string.IsNullOrWhiteSpace(topic?.Text)
                        select new DuckResult
                        {
                            Info = topic.Text, SourceUrl = topic.FirstUrl,
                            ResultType = variableTopic.Name, Score = 2, SearchResultType = ResultType.SearchResult,
                            SearchedText = searchedText
                        })
                        await channel.Writer.WriteAsync(duckResult
                            , CancellationToken.None);
                }
        }

        public static DuckResult GetAnswers(Root root, string searchedText)
        {
            if (string.IsNullOrWhiteSpace(root?.Answer)) return null;

            string resultType = root.AnswerType ?? "Answer";
            DuckResult duckResult = new()
            {
                Info = root.Answer, ResultType = resultType, SearchResultType = ResultType.Answer,
                SearchedText = searchedText, Score = 10
            };

            return duckResult;
        }

        public static DuckResult GetDictionary(Root root, string searchedText)
        {
            if (string.IsNullOrWhiteSpace(root?.Definition)) return null;

            DuckResult duckResult = new()
            {
                Info = root.Definition, ResultType = "Define", SourceUrl = root.DefinitionUrl,
                SearchResultType = ResultType.Definition, SearchedText = searchedText, Score = 9
            };

            return duckResult;
        }

        public static DuckResult GetAbstract(Root root, string searchedText)
        {
            if (string.IsNullOrWhiteSpace(root?.AbstractText)) return null;

            DuckResult duckResult = new()
            {
                Info = root.AbstractText, ResultType = "Abstract", SourceUrl = root.AbstractUrl,
                SearchResultType = ResultType.Abstract, SearchedText = searchedText,
                Score = 8
            };

            return duckResult;
        }

        public static async Task<DuckDuckGoSearchResult> GetQrImage(string searchedText)
        {
            string url = Endpoint + "qrcode+" + WebUtility.UrlEncode(searchedText) + "&format=json";
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgentString);
            var root = await httpClient.GetFromJsonAsync<Root>(url, SerializerOptions);

            if (root == null) return null;

            const string resultType = "QR";

            if (!root.AnswerType.Equals("qrcode") || string.IsNullOrWhiteSpace(root.Answer)) return null;

            string info = root.Answer;

            BitmapImageResult bitmapImageResult;
            HtmlDocument document = HtmlDocument.FromHtml(info);
            IEnumerable<HtmlElementNode> nodes = document.Find("img");
            HtmlElementNode htmlElementNode = nodes.ElementAt(0);

            if (htmlElementNode.Attributes.Count == 0)
            {
                bitmapImageResult = null;
            }
            else
            {
                HtmlAttributeCollection htmlAttributeCollection = htmlElementNode.Attributes;
                HtmlAttribute htmlAttribute = htmlAttributeCollection[0];
                string base64String = htmlAttribute.Value;
                base64String = base64String?[(base64String.IndexOf(",", StringComparison.Ordinal) + 1)..];
                byte[] imageBytes = Convert.FromBase64String(base64String!);
                await using var mem = new MemoryStream(imageBytes);
                bitmapImageResult = new BitmapImageResult(mem);
            }

            return new DuckDuckGoSearchResult(searchedText, searchedText, resultType
                , QrOperations, 2)
            {
                Url = searchedText,
                SearchObjectId = new DuckResult {SearchedText = searchedText, SearchResultType = ResultType.QrCode},
                PreviewImage = bitmapImageResult
            };
        }

        public static void SaveQrImage(DuckDuckGoSearchResult duckGoSearchResult)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var saveFileDialog = new SaveFileDialog
                {
                    InitialFileName = "qr_code", DefaultExtension = "png", Title = "Save Image To..."
                };

                saveFileDialog.ShowAsync(new Window()).ContinueWith(task =>
                {
                    if (!task.IsCompletedSuccessfully)
                        return;

                    string path = task.Result;
                    if (string.IsNullOrWhiteSpace(path)) return;
                    if (duckGoSearchResult.PreviewImage is {IsEmpty: true}) return;
                    Bitmap bitmap = duckGoSearchResult.PreviewImage.ConvertToNormalBitmap();
                    bitmap.Save(path);
                    if (!File.Exists(path)) return;
                    IProcessManager managerInstance = ProcessUtils.GetManagerInstance();
                    managerInstance.StartNewProcess(path);
                });
            }, DispatcherPriority.Input);
        }
    }
}
