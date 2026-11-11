using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace UrlShortener.Services;

/// <summary>
/// Checks a URL against the Google Safe Browsing Lookup API v4. Fails open (treats
/// the URL as safe) whenever GOOGLE_SAFE_BROWSING_API_KEY isn't configured, or the
/// API errors or times out — never blocks link creation on an infrastructure problem.
/// Registered with a short-timeout typed HttpClient (see Program.cs).
/// </summary>
public class GoogleSafeBrowsingChecker(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<GoogleSafeBrowsingChecker> logger) : IUrlSafetyChecker
{
    private static readonly string[] ThreatTypes =
        ["MALWARE", "SOCIAL_ENGINEERING", "UNWANTED_SOFTWARE", "POTENTIALLY_HARMFUL_APPLICATION"];

    public async Task<bool> IsSafeAsync(string url, CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["GOOGLE_SAFE_BROWSING_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning(
                "GOOGLE_SAFE_BROWSING_API_KEY is not set; skipping the Safe Browsing check (failing open).");
            return true;
        }

        try
        {
            var requestBody = new SafeBrowsingRequest(
                new ClientInfo("urly-url-shortener", "1.0.0"),
                new ThreatInfo(ThreatTypes, ["ANY_PLATFORM"], ["URL"], [new ThreatEntry(url)]));

            using var response = await httpClient.PostAsJsonAsync(
                $"v4/threatMatches:find?key={Uri.EscapeDataString(apiKey)}", requestBody, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Google Safe Browsing API returned {StatusCode}; failing open.", response.StatusCode);
                return true;
            }

            var result = await response.Content.ReadFromJsonAsync<SafeBrowsingResponse>(cancellationToken);
            return result?.Matches is not { Count: > 0 };
        }
        catch (Exception ex)
        {
            // Network error, timeout, or malformed response — never block link creation on this.
            logger.LogWarning(ex, "Google Safe Browsing check failed; failing open.");
            return true;
        }
    }

    private record SafeBrowsingRequest(
        [property: JsonPropertyName("client")] ClientInfo Client,
        [property: JsonPropertyName("threatInfo")] ThreatInfo ThreatInfo);

    private record ClientInfo(
        [property: JsonPropertyName("clientId")] string ClientId,
        [property: JsonPropertyName("clientVersion")] string ClientVersion);

    private record ThreatInfo(
        [property: JsonPropertyName("threatTypes")] string[] ThreatTypes,
        [property: JsonPropertyName("platformTypes")] string[] PlatformTypes,
        [property: JsonPropertyName("threatEntryTypes")] string[] ThreatEntryTypes,
        [property: JsonPropertyName("threatEntries")] ThreatEntry[] ThreatEntries);

    private record ThreatEntry([property: JsonPropertyName("url")] string Url);

    private class SafeBrowsingResponse
    {
        [JsonPropertyName("matches")]
        public List<object>? Matches { get; set; }
    }
}
