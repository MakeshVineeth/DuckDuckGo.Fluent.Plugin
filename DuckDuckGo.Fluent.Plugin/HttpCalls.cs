using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static DuckDuckGo.Fluent.Plugin.JsonResult;

namespace DuckDuckGo.Fluent.Plugin;

public static class HttpCalls
{
    private const string UserAgentString = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public static Task<DuckDuckGoApiResult> GetApiResult(string url, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgentString);
        return httpClient.GetFromJsonAsync<DuckDuckGoApiResult>(url, SerializerOptions,
            cancellationToken);
    }
}
