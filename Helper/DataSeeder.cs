using UrlShortener.Models;

namespace UrlShortener.Helper;

public static class DataSeeder
{
    public static void Seed(UrlShortenerContext db, IConfiguration config)
    {
        if (db.Urls.Any()) return;

        var domain = (config["App:ShortUrlDomain"] ?? "").TrimEnd('/');
        if (string.IsNullOrWhiteSpace(domain))
            domain = "https://demo.short";

        var demos = new (string Slug, string Url)[]
        {
            ("gh1234", "https://github.com"),
            ("li5678", "https://linkedin.com"),
            ("doc9ab", "https://github.com/topics/url-shortener"),
        };

        foreach (var (slug, url) in demos)
        {
            db.Urls.Add(new Url
            {
                UrlId = slug,
                OriginalUrl = url,
                ShortenedUrl = $"{domain}/{slug}",
                UserId = 0,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            });
        }
        db.SaveChanges();
    }
}
