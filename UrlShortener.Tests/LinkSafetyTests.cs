using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UrlShortener.Models;
using UrlShortener.Services;
using UrlShortener.Tests.Fakes;
using Xunit;

namespace UrlShortener.Tests;

/// <summary>
/// Exercises the Google Safe Browsing integration entirely through FakeUrlSafetyChecker —
/// these tests never make a network call to Google.
/// </summary>
public class LinkSafetyTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public LinkSafetyTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    private (WebApplicationFactory<Program> Factory, FakeUrlSafetyChecker Checker) CreateFactory()
    {
        var fakeChecker = new FakeUrlSafetyChecker();

        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:ApiKey"] = "test-key",
                    ["App:SeedDemoData"] = "false",
                });
            });

            builder.ConfigureServices(services =>
            {
                var dbDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<UrlShortenerContext>));
                if (dbDescriptor != null) services.Remove(dbDescriptor);

                services.AddDbContext<UrlShortenerContext>(options =>
                    options.UseNpgsql(_postgres.ConnectionString));

                // Replace the real Google-backed checker with a controllable fake.
                services.RemoveAll<IUrlSafetyChecker>();
                services.AddSingleton<IUrlSafetyChecker>(fakeChecker);
            });
        });

        return (factory, fakeChecker);
    }

    [Fact]
    public async Task Post_SafeUrl_Returns201()
    {
        var (factory, checker) = CreateFactory();
        checker.IsSafe = true;
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/urls", new { url = "https://example.com/safe-url-test" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_UnsafeUrl_Returns400()
    {
        var (factory, checker) = CreateFactory();
        checker.IsSafe = false;
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/urls", new { url = "https://example.com/unsafe-url-test" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("unsafe", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Navigate_LinkBecomesUnsafe_DeactivatesAndRedirectsToWarningPage()
    {
        var (factory, checker) = CreateFactory();
        checker.IsSafe = true;
        var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/urls", new { url = "https://example.com/becomes-unsafe-test" });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var shortUrl = (await createResponse.Content.ReadAsStringAsync()).Trim('"');
        var slug = shortUrl.Split('/').Last();

        // Force the 24h recheck window to have elapsed, then flip the checker to "unsafe".
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UrlShortenerContext>();
            var url = await db.Urls.SingleAsync(u => u.UrlId == slug);
            url.LastSafetyCheckAt = DateTime.UtcNow.AddHours(-25);
            await db.SaveChangesAsync();
        }
        checker.IsSafe = false;

        var redirectClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        var navResponse = await redirectClient.GetAsync($"/{slug}");

        Assert.Equal(HttpStatusCode.Found, navResponse.StatusCode);
        Assert.EndsWith("/link-unsafe", navResponse.Headers.Location?.ToString());

        // The link is now deactivated — a second visit lands on the not-found page.
        var secondNav = await redirectClient.GetAsync($"/{slug}");
        Assert.Equal(HttpStatusCode.Found, secondNav.StatusCode);
        Assert.EndsWith("/link-not-found", secondNav.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Navigate_RecentlyCheckedSafeLink_DoesNotRecheck()
    {
        var (factory, checker) = CreateFactory();
        checker.IsSafe = true;
        var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/urls", new { url = "https://example.com/recently-checked-test" });
        var shortUrl = (await createResponse.Content.ReadAsStringAsync()).Trim('"');
        var slug = shortUrl.Split('/').Last();

        // Flip the checker to "unsafe" — if Navigate incorrectly re-checked a fresh link, this would fire.
        checker.IsSafe = false;

        var redirectClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        var navResponse = await redirectClient.GetAsync($"/{slug}");

        Assert.Equal(HttpStatusCode.Found, navResponse.StatusCode);
        Assert.Equal("https://example.com/recently-checked-test", navResponse.Headers.Location?.ToString());
    }
}
