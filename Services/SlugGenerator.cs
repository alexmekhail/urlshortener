using Microsoft.EntityFrameworkCore;
using UrlShortener.Models;

namespace UrlShortener.Services;

public interface ISlugGenerator
{
    Task<string> GenerateUniqueSlugAsync(UrlShortenerContext context);
}

public class SlugGenerator : ISlugGenerator
{
    private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int SlugLength = 6;

    public async Task<string> GenerateUniqueSlugAsync(UrlShortenerContext context)
    {
        string slug;
        do
        {
            slug = GenerateSlug();
        } while (await context.Urls.AnyAsync(u => u.UrlId == slug));

        return slug;
    }

    // public so unit tests can call it directly
    public static string GenerateSlug()
    {
        return new string(Enumerable.Range(0, SlugLength)
            .Select(_ => Chars[Random.Shared.Next(Chars.Length)])
            .ToArray());
    }
}
