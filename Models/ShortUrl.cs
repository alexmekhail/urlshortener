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
}
