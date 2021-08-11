using System;
using System.Net;
using static DuckDuckGo.Fluent.Plugin.DuckDuckGoSearchResult;

namespace DuckDuckGo.Fluent.Plugin
{
    public class AppFunctions
    {
        public const string DuckWebsiteUrl = "https://duckduckgo.com/";

        public static string GetEndpointUrl(string searchedText, bool isQr = false)
        {
            string encodedSearch = WebUtility.UrlEncode(searchedText);
            const string formatJson = "&format=json";
            const string endpoint = "https://api.duckduckgo.com/?q=";

            return isQr
                ? endpoint + "qrcode+" + encodedSearch + formatJson
                : endpoint + encodedSearch + formatJson + "&no_html=1";
        }

        public static bool VerifySearchedTerms(string searchedText, string searchedTag)
        {
            if (string.IsNullOrWhiteSpace(searchedText)) return false;

            if (string.IsNullOrWhiteSpace(searchedTag)) return true;

            return searchedTag.Equals(DuckSearchTagName, StringComparison.Ordinal) ||
                   searchedTag.Equals(QrTag, StringComparison.Ordinal);
        }
    }
}
