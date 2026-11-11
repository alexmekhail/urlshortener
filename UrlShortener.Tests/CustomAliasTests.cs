using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Models;
using Xunit;

namespace UrlShortener.Tests;

public class CustomAliasTests : IClassFixture<PostgresFixture>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CustomAliasTests(PostgresFixture postgres)
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

    [Fact]
    public async Task Post_WithValidAlias_UsesAliasAsSlug()
    {
        var client = _factory.CreateClient();
        var alias = $"my-custom-alias-{Guid.NewGuid():N}"[..20];

        var response = await client.PostAsJsonAsync(
            "/api/urls", new { url = "https://example.com/alias-test", alias });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var shortUrl = (await response.Content.ReadAsStringAsync()).Trim('"');
        Assert.EndsWith($"/{alias}", shortUrl);
    }

    [Fact]
    public async Task Post_WithTakenAlias_Returns409()
    {
        var client = _factory.CreateClient();
        var alias = $"taken-alias-{Guid.NewGuid():N}"[..20];

        var first = await client.PostAsJsonAsync(
            "/api/urls", new { url = "https://example.com/alias-conflict-1", alias });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync(
            "/api/urls", new { url = "https://example.com/alias-conflict-2", alias });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Theory]
    [InlineData("ab")] // too short
    [InlineData("this-alias-is-way-too-long-to-be-valid-12345")] // too long
    [InlineData("has a space")]
    [InlineData("has/slash")]
    [InlineData("has.dot")]
    public async Task Post_WithInvalidAliasFormat_Returns400(string alias)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/urls", new { url = "https://example.com/alias-format-test", alias });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("api")]
    [InlineData("dashboard")]
    [InlineData("health")]
    [InlineData("link-expired")]
    [InlineData("link-not-found")]
    [InlineData("ADMIN")] // reserved check is case-insensitive
    public async Task Post_WithReservedAlias_Returns400(string alias)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/urls", new { url = "https://example.com/reserved-alias-test", alias });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithAlias_DoesNotDeduplicate()
    {
        var client = _factory.CreateClient();
        const string longUrl = "https://example.com/alias-duplicate-test";

        var first = await client.PostAsJsonAsync(
            "/api/urls", new { url = longUrl, alias = $"alias-a-{Guid.NewGuid():N}"[..20] });
        var second = await client.PostAsJsonAsync(
            "/api/urls", new { url = longUrl, alias = $"alias-b-{Guid.NewGuid():N}"[..20] });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.NotEqual(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Navigate_CustomAlias_RedirectsToOriginalUrl()
    {
        var client = _factory.CreateClient();
        var alias = $"redirect-alias-{Guid.NewGuid():N}"[..20];
        const string longUrl = "https://example.com/alias-navigate-test";

        var createResponse = await client.PostAsJsonAsync("/api/urls", new { url = longUrl, alias });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var redirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        var navResponse = await redirectClient.GetAsync($"/{alias}");

        Assert.Equal(HttpStatusCode.Found, navResponse.StatusCode);
        Assert.Equal(longUrl, navResponse.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Post_AliasIsCaseSensitive()
    {
        var client = _factory.CreateClient();
        var baseAlias = $"CaseTest{Guid.NewGuid():N}"[..16];

        var first = await client.PostAsJsonAsync(
            "/api/urls", new { url = "https://example.com/case-sensitive-alias-1", alias = baseAlias });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Different case — must be treated as a distinct alias, not a conflict.
        var differentCase = baseAlias.ToLowerInvariant() == baseAlias ? baseAlias.ToUpperInvariant() : baseAlias.ToLowerInvariant();
        var second = await client.PostAsJsonAsync(
            "/api/urls", new { url = "https://example.com/case-sensitive-alias-2", alias = differentCase });

        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }
}
