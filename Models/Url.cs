using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UrlShortener.Models;

public class Url
{
    [Key]
    public string UrlId { get; set; } = string.Empty;

    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(2000)]
    public required string OriginalUrl { get; set; }

    [Required]
    [StringLength(500)]
    public required string ShortenedUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this link's target was last checked against Google Safe Browsing (at
    /// creation, and at most once per 24h thereafter, re-checked lazily on redirect).
    /// </summary>
    public DateTime? LastSafetyCheckAt { get; set; }

    /// <summary>Total recorded clicks. Computed per-query, not a database column.</summary>
    [NotMapped]
    public int ClickCount { get; set; }
}
