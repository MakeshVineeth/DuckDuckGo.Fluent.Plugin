using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Blast.API.Core.Processes;
using Blast.API.Graphics;
using Blast.API.Processes;
using Blast.Core.Results;
using SoftCircuits.HtmlMonkey;
using static DuckDuckGo.Fluent.Plugin.JsonResult;
using static DuckDuckGo.Fluent.Plugin.AppFunctions;
using static DuckDuckGo.Fluent.Plugin.DuckDuckGoSearchResult;

namespace DuckDuckGo.Fluent.Plugin
{
    public class QrFunctions
    {
        public static async Task<DuckDuckGoSearchResult> GetQrImage(string searchedText)
        {
            string url = GetEndpointUrl(searchedText, true);
            DuckDuckGoApiResult apiResult = await HttpCalls.GetApiResult(url);

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

                if (htmlAttribute == null) return null;

                string base64String = htmlAttribute.Value;
                base64String = base64String?[(base64String.IndexOf(",", StringComparison.Ordinal) + 1)..];
                byte[] imageBytes = Convert.FromBase64String(base64String!);
                bitmapImageResult = new BitmapImageResult(new MemoryStream(imageBytes));
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
            if (duckGoSearchResult.PreviewImage is { IsEmpty: true }) return;

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