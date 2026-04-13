using System.ComponentModel.DataAnnotations;

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
}
