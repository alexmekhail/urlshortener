using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Models;
using Xunit;

namespace UrlShortener.Tests;

public class ExpirationTests : IClassFixture<PostgresFixture>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExpirationTests(PostgresFixture postgres)
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
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
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<UrlShortenerContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<UrlShortenerContext>(options =>
                    options.UseNpgsql(postgres.ConnectionString));
            });
        });
    }

    private static async Task<string> ExtractSlugAsync(HttpResponseMessage response)
    {
        var shortUrl = (await response.Content.ReadAsStringAsync()).Trim('"');
        return shortUrl.Split('/').Last();
    }

    [Theory]
    [InlineData("1h")]
    [InlineData("1d")]
    [InlineData("7d")]
    [InlineData("30d")]
    public async Task Post_WithValidExpiresIn_SetsExpiryInTheFuture(string expiresIn)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/urls", new { url = $"https://example.com/expiry-test-{expiresIn}", expiresIn });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var slug = await ExtractSlugAsync(response);

        var getResponse = await client.GetAsync($"/api/urls/{slug}");
        var url = await getResponse.Content.ReadFromJsonAsync<Url>();

        Assert.NotNull(url!.ExpiresAt);
        Assert.True(url.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Post_WithoutExpiresIn_NeverExpires()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/urls", new { url = "https://example.com/no-expiry-test" });
        var slug = await ExtractSlugAsync(response);

        var getResponse = await client.GetAsync($"/api/urls/{slug}");
        var url = await getResponse.Content.ReadFromJsonAsync<Url>();

        Assert.Null(url!.ExpiresAt);
    }

    [Fact]
    public async Task Post_WithInvalidExpiresIn_Returns400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/urls", new { url = "https://example.com/invalid-expiry-test", expiresIn = "2h" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithExpiresIn_DoesNotDeduplicate()
    {
        var client = _factory.CreateClient();
        const string longUrl = "https://example.com/expiring-duplicate-test";

        var first = await client.PostAsJsonAsync("/api/urls", new { url = longUrl, expiresIn = "1h" });
        var second = await client.PostAsJsonAsync("/api/urls", new { url = longUrl, expiresIn = "1h" });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.NotEqual(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Navigate_ExpiredLink_RedirectsToLinkExpiredPage()
    {
        var client = _factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync(
            "/api/urls", new { url = "https://example.com/already-expired-test", expiresIn = "1h" });
        var slug = await ExtractSlugAsync(createResponse);

        // Force the expiry into the past — we don't want this test to depend on real time passing.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UrlShortenerContext>();
            var url = await db.Urls.SingleAsync(u => u.UrlId == slug);
            url.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        var redirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        var navResponse = await redirectClient.GetAsync($"/{slug}");

        Assert.Equal(HttpStatusCode.Found, navResponse.StatusCode);
        Assert.EndsWith("/link-expired", navResponse.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Navigate_NotYetExpiredLink_RedirectsNormally()
    {
        var client = _factory.CreateClient();
        const string longUrl = "https://example.com/not-yet-expired-test";
        var createResponse = await client.PostAsJsonAsync("/api/urls", new { url = longUrl, expiresIn = "1h" });
        var slug = await ExtractSlugAsync(createResponse);

        var redirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        var navResponse = await redirectClient.GetAsync($"/{slug}");

        Assert.Equal(HttpStatusCode.Found, navResponse.StatusCode);
        Assert.Equal(longUrl, navResponse.Headers.Location?.ToString());
    }
}
