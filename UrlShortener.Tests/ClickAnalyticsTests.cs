using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Models;
using Xunit;

namespace UrlShortener.Tests;

/// <summary>
/// Clicks are recorded asynchronously by a background batch-inserter (see
/// ClickIngestionService), so tests that assert on recorded clicks poll briefly
/// instead of asserting immediately after the redirect request completes.
/// </summary>
public class ClickAnalyticsTests : IClassFixture<PostgresFixture>
{
    private const string TestApiKey = "test-key";
    private readonly WebApplicationFactory<Program> _factory;

    public ClickAnalyticsTests(PostgresFixture postgres)
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:ApiKey"] = TestApiKey,
                    ["App:SeedDemoData"] = "false",
                });
            });

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<UrlShortenerContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<UrlShortenerContext>(options =>
                    options.UseNpgsql(postgres.ConnectionString));
            });
        });
    }

    private HttpClient CreateApiKeyClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", TestApiKey);
        return client;
    }

    private async Task<string> CreateShortLinkAsync(HttpClient client, string longUrl)
    {
        var response = await client.PostAsJsonAsync("/api/urls", new { url = longUrl });
        var shortUrl = (await response.Content.ReadAsStringAsync()).Trim('"');
        return shortUrl.Split('/').Last();
    }

    private async Task<UrlStatsResponse?> WaitForStatsAsync(string slug, Func<UrlStatsResponse?, bool> isReady)
    {
        var apiClient = CreateApiKeyClient();
        var deadline = DateTime.UtcNow.AddSeconds(5);
        UrlStatsResponse? stats = null;

        while (DateTime.UtcNow < deadline)
        {
            var response = await apiClient.GetAsync($"/api/urls/{slug}/stats");
            stats = await response.Content.ReadFromJsonAsync<UrlStatsResponse>();
            if (isReady(stats)) return stats;
            await Task.Delay(100);
        }

        return stats;
    }

    [Fact]
    public async Task Navigate_RecordsClick_ReflectedInStats()
    {
        var anonClient = _factory.CreateClient();
        var slug = await CreateShortLinkAsync(anonClient, "https://example.com/click-analytics-test");

        var redirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        redirectClient.DefaultRequestHeaders.Add(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36");
        redirectClient.DefaultRequestHeaders.Referrer = new Uri("https://twitter.com/some/post");

        var navResponse = await redirectClient.GetAsync($"/{slug}");
        Assert.Equal(HttpStatusCode.Found, navResponse.StatusCode);
        Assert.True(navResponse.Headers.CacheControl?.NoStore);
        Assert.True(navResponse.Headers.CacheControl?.Private);

        var stats = await WaitForStatsAsync(slug, s => s is { TotalClicks: >= 1 });

        Assert.NotNull(stats);
        Assert.True(stats!.TotalClicks >= 1);
        Assert.Contains(stats.TopReferrers, r => r.Key == "twitter.com");
        Assert.Contains(stats.Devices, d => d.Key == "desktop");
        Assert.Contains(stats.Browsers, b => b.Key == "Chrome");
        Assert.Single(stats.ClicksPerDay.Where(d => d.Date == DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [Fact]
    public async Task GetStats_UnknownSlug_Returns404()
    {
        var client = CreateApiKeyClient();
        var response = await client.GetAsync("/api/urls/zzzzzz/stats");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStats_WithoutApiKey_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/urls/zzzzzz/stats");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_IncludesClickCountField()
    {
        var client = CreateApiKeyClient();
        var slug = await CreateShortLinkAsync(client, "https://example.com/list-click-count-test");

        var response = await client.GetAsync("/api/urls");
        var urls = await response.Content.ReadFromJsonAsync<List<Url>>();

        Assert.Contains(urls!, u => u.UrlId == slug && u.ClickCount == 0);
    }
}
