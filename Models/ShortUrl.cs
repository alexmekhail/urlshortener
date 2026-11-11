using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace UrlShortener.Models;

public class ShortUrl
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Url]
    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; set; }

    [JsonPropertyName("hits")]
    public int Hits { get; set; }

    /// <summary>One of "1h", "1d", "7d", "30d", or null/omitted for no expiry.</summary>
    [JsonPropertyName("expiresIn")]
    public string? ExpiresIn { get; set; }

    /// <summary>Optional custom slug, 3-30 chars, [A-Za-z0-9_-], case-sensitive.</summary>
    [JsonPropertyName("alias")]
    public string? Alias { get; set; }
}
