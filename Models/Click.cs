using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Models;

/// <summary>
/// A single recorded click/redirect event for a short URL.
/// Never stores the client's raw IP address.
/// </summary>
public class Click
{
    [Key]
    public long Id { get; set; }

    [Required]
    public required string UrlId { get; set; }

    public DateTime ClickedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Host portion of the Referer header, e.g. "twitter.com". Null if absent/unparsable.</summary>
    [StringLength(255)]
    public string? ReferrerHost { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code from the x-vercel-ip-country header, if present.</summary>
    [StringLength(2)]
    public string? Country { get; set; }

    [Required]
    [StringLength(20)]
    public string DeviceType { get; set; } = Models.DeviceTypes.Unknown;

    [StringLength(50)]
    public string? Browser { get; set; }
}

public static class DeviceTypes
{
    public const string Desktop = "desktop";
    public const string Mobile = "mobile";
    public const string Tablet = "tablet";
    public const string Bot = "bot";
    public const string Unknown = "unknown";
}
