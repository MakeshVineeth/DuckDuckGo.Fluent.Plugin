using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
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
        private const string Endpoint = "https://api.duckduckgo.com/?q=";
        public const string DuckWebsiteUrl = "https://duckduckgo.com/";

        public static string GetEndpointUrl(string searchedText)
        {
            return Endpoint + WebUtility.UrlEncode(searchedText) + "&format=json&no_html=1";
        }

        public static bool VerifySearchedTerms(string searchedText, string searchedTag)
        {
            if (string.IsNullOrWhiteSpace(searchedText)) return false;

            if (string.IsNullOrWhiteSpace(searchedTag)) return true;

            return searchedTag.Equals(DuckSearchTagName, StringComparison.Ordinal) ||
                   searchedTag.Equals(QrTag, StringComparison.Ordinal);
        }

        public static async Task<DuckDuckGoSearchResult> GetQrImage(string searchedText)
        {
            string url = Endpoint + "qrcode+" + WebUtility.UrlEncode(searchedText) + "&format=json";
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgentString);
            var apiResult = await httpClient.GetFromJsonAsync<DuckDuckGoApiResult>(url, SerializerOptions);

            if (apiResult == null) return null;

            const string resultType = "QR";

            if (!apiResult.AnswerType.Equals("qrcode") || string.IsNullOrWhiteSpace(apiResult.Answer)) return null;

            string info = apiResult.Answer;

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
                HtmlAttribute htmlAttribute = htmlAttributeCollection.FirstOrDefault();
                string base64String = htmlAttribute?.Value;
                base64String = base64String?[(base64String.IndexOf(",", StringComparison.Ordinal) + 1)..];
                byte[] imageBytes = Convert.FromBase64String(base64String!);
                await using var mem = new MemoryStream(imageBytes);
                bitmapImageResult = new BitmapImageResult(mem);
            }

            return new DuckDuckGoSearchResult(searchedText, searchedText, resultType
                , QrOperations, 2)
            {
                Url = searchedText,
                SearchObjectId = new DuckResult { SearchedText = searchedText, SearchResultType = ResultType.QrCode },
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
                    if (duckGoSearchResult.PreviewImage is { IsEmpty: true }) return;
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
