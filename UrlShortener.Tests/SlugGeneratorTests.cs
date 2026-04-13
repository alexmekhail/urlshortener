using UrlShortener.Services;
using Xunit;

namespace UrlShortener.Tests;

public class SlugGeneratorTests
{
    private const string Base62Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    [Fact]
    public void GenerateSlug_HasLengthOfSix()
    {
        var slug = SlugGenerator.GenerateSlug();
        Assert.Equal(6, slug.Length);
    }

    [Fact]
    public void GenerateSlug_OnlyContainsBase62Characters()
    {
        for (int i = 0; i < 200; i++)
        {
            var slug = SlugGenerator.GenerateSlug();
            Assert.All(slug, c => Assert.Contains(c, Base62Chars));
        }
    }

    [Fact]
    public void GenerateSlug_ProducesHighVariety()
    {
        // With 62^6 ≈ 56 billion possibilities, 100 calls should yield many unique slugs
        var slugs = Enumerable.Range(0, 100).Select(_ => SlugGenerator.GenerateSlug()).ToHashSet();
        Assert.True(slugs.Count > 90, $"Expected at least 90 unique slugs out of 100; got {slugs.Count}");
    }

    [Fact]
    public void GenerateSlug_DoesNotContainSpecialCharacters()
    {
        for (int i = 0; i < 100; i++)
        {
            var slug = SlugGenerator.GenerateSlug();
            Assert.False(slug.Any(c => !char.IsLetterOrDigit(c)), $"Slug '{slug}' contained non-alphanumeric characters");
        }
    }
}
