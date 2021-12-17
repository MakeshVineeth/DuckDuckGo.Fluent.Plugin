using System;
using System.Net;
using static DuckDuckGo.Fluent.Plugin.DuckDuckGoSearchResult;

namespace DuckDuckGo.Fluent.Plugin
{
    public class AppFunctions
    {
        private const string DuckWebsiteUrl = "https://duckduckgo.com/";

        public static string GetEndpointUrl(string searchedText, bool isQr = false)
        {
            string encodedSearch = WebUtility.UrlEncode(searchedText);
            const string formatJson = "&format=json";
            const string endpoint = "https://api.duckduckgo.com/?q=";

            return isQr
                ? endpoint + "qrcode+" + encodedSearch + formatJson
                : endpoint + encodedSearch + formatJson + "&no_html=1";
        }

        public static SearchAction VerifySearchedTerms(string searchedText, string searchedTag)
        {
            if (string.IsNullOrWhiteSpace(searchedText)) return SearchAction.Null;
            if (string.IsNullOrWhiteSpace(searchedTag)) return SearchAction.Null;

            if (searchedTag.Equals(DuckSearchTagName, StringComparison.Ordinal))
                return SearchAction.Normal;

            return searchedTag.Equals(QrTag, StringComparison.Ordinal) ? SearchAction.QrCode : SearchAction.Null;
        }

        public static string GetGeneralizedUrl(string searchedText)
        {
            return DuckWebsiteUrl + "?q=" + WebUtility.UrlEncode(searchedText);
        }
    }
}
