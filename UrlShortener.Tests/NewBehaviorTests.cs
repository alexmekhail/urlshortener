using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using UrlShortener.Models;
using Xunit;

namespace UrlShortener.Tests;

/// <summary>
/// Tests for new behaviours: URL validation, self-referral rejection, rate limiting.
/// Each class gets its own Postgres container to keep rate-limit counters isolated.
/// </summary>

// ── URL Validation & Self-referral ───────────────────────────────────────────

public class UrlValidationTests : IClassFixture<PostgresFixture>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UrlValidationTests(PostgresFixture postgres)
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:ApiKey"] = "test-key",
                    ["App:SeedDemoData"] = "false",
                    ["App:ShortUrlDomain"] = "https://short.example.com",
                });
            });
            builder.ConfigureServices(services =>
            {
                var d = services.SingleOrDefault(s => s.ServiceType == typeof(DbContextOptions<UrlShortenerContext>));
                if (d != null) services.Remove(d);
                services.AddDbContext<UrlShortenerContext>(o => o.UseNpgsql(postgres.ConnectionString));
            });
        });
    }

    [Fact]
    public async Task Post_UrlExceedingMaxLength_Returns400()
    {
        var client = _factory.CreateClient();
        var longUrl = "https://example.com/" + new string('a', 1985); // total > 2000
        var response = await client.PostAsJsonAsync("/api/urls", new { url = longUrl });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_UrlAtMaxLength_Returns201()
    {
        var client = _factory.CreateClient();
        const string prefix = "https://example.com/";
        // exactly 2000 chars total
        var url = prefix + new string('a', 2000 - prefix.Length);
        Assert.Equal(2000, url.Length);

        var response = await client.PostAsJsonAsync("/api/urls", new { url });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_SelfReferentialUrl_Returns400()
    {
        var client = _factory.CreateClient();
        // App:ShortUrlDomain is https://short.example.com
        var response = await client.PostAsJsonAsync("/api/urls",
            new { url = "https://short.example.com/some/existing/slug" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("shortener", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_NonSelfReferentialUrl_Returns201()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/urls",
            new { url = "https://other.example.com/page" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}

// ── Rate Limiting ─────────────────────────────────────────────────────────────

public class RateLimitFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

public class RateLimitTests : IClassFixture<RateLimitFixture>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RateLimitTests(RateLimitFixture postgres)
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:ApiKey"] = "test-key",
                    ["App:SeedDemoData"] = "false",
                    // Lower the limit so the test finishes quickly
                    ["RateLimit:PermitLimit"] = "3",
                });
            });
            builder.ConfigureServices(services =>
            {
                var d = services.SingleOrDefault(s => s.ServiceType == typeof(DbContextOptions<UrlShortenerContext>));
                if (d != null) services.Remove(d);
                services.AddDbContext<UrlShortenerContext>(o => o.UseNpgsql(postgres.ConnectionString));
            });
        });
    }

    [Fact]
    public async Task Post_ExceedsRateLimit_Returns429()
    {
        var client = _factory.CreateClient();

        // Fire enough requests to exceed the default PermitLimit=10.
        // (Config injection arrives after service registration, so we cannot
        //  lower the limit via in-memory config — we just hit the real limit.)
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 12; i++)
        {
            var res = await client.PostAsJsonAsync("/api/urls",
                new { url = $"https://example.com/rate-limit-test-{i}" });
            statuses.Add(res.StatusCode);
        }

        // At least one request should have been rate-limited.
        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
    }

    [Fact]
    public async Task Get_IsNotRateLimited()
    {
        // GET endpoints (redirect, list) should not be subject to the link-creation limiter.
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        for (var i = 0; i < 5; i++)
        {
            var res = await client.GetAsync("/xxxxxx");
            Assert.NotEqual(HttpStatusCode.TooManyRequests, res.StatusCode);
        }
    }
}
