using System.Net;
using System.Net.Http.Json;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using UrlShortener.Models;
using Xunit;

namespace UrlShortener.Tests;

// One container shared across all tests in this class.
public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

public class ApiIntegrationTests : IClassFixture<PostgresFixture>
{
    private const string TestApiKey = "test-api-key-for-integration-tests";

    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests(PostgresFixture postgres)
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:ApiKey"] = TestApiKey,
                    ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
                    // Disable seeding so demo slugs don't affect test assertions
                    ["App:SeedDemoData"] = "false",
                });
            });

            builder.ConfigureServices(services =>
            {
                // Replace the app's DbContext registration with one pointing at the
                // Testcontainers Postgres instance started above.
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<UrlShortenerContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<UrlShortenerContext>(options =>
                    options.UseNpgsql(postgres.ConnectionString));
            });
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private HttpClient CreateApiKeyClient(WebApplicationFactoryClientOptions? opts = null)
    {
        var client = opts is null ? _factory.CreateClient() : _factory.CreateClient(opts);
        client.DefaultRequestHeaders.Add("X-Api-Key", TestApiKey);
        return client;
    }

    // ── POST /api/urls ────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_WithoutAuth_Returns201()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/urls", new { url = "https://example.com/anon-test" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("/navigate/", body);
        Assert.Matches(@"https?://[^/]+/[A-Za-z0-9]{6}", body);
    }

    [Fact]
    public async Task Post_ValidUrl_Returns201WithShortLink()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/urls", new { url = "https://example.com/some/long/path" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("/navigate/", body);
        Assert.Matches(@"https?://[^/]+/[A-Za-z0-9]{6}", body);
    }

    [Fact]
    public async Task Post_InvalidUrl_Returns400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/urls", new { url = "not-a-url" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_FtpUrl_Returns400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/urls", new { url = "ftp://files.example.com/file.zip" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_DuplicateUrl_Returns200WithSameShortLink()
    {
        var client = _factory.CreateClient();
        const string longUrl = "https://example.com/duplicate-test";

        var first = await client.PostAsJsonAsync("/api/urls", new { url = longUrl });
        var second = await client.PostAsJsonAsync("/api/urls", new { url = longUrl });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(
            await first.Content.ReadAsStringAsync(),
            await second.Content.ReadAsStringAsync());
    }

    // ── DELETE /api/urls/{slug} ───────────────────────────────────────────────

    [Fact]
    public async Task Delete_WithoutApiKey_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.DeleteAsync("/api/urls/someslug");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithApiKey_DeactivatesUrl()
    {
        var client = CreateApiKeyClient();
        const string longUrl = "https://example.com/delete-test";

        var postResponse = await client.PostAsJsonAsync("/api/urls", new { url = longUrl });
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var shortUrl = (await postResponse.Content.ReadAsStringAsync()).Trim('"');
        var slug = shortUrl.Split('/').Last();

        var deleteResponse = await client.DeleteAsync($"/api/urls/{slug}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var redirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var navResponse = await redirectClient.GetAsync($"/{slug}");
        Assert.Equal(HttpStatusCode.Found, navResponse.StatusCode);
        Assert.StartsWith("https://www.google.com", navResponse.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Delete_WithWrongApiKey_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "this-is-the-wrong-key");
        var response = await client.DeleteAsync("/api/urls/someslug");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── GET /api/urls ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUrls_WithValidApiKey_Returns200WithArray()
    {
        var client = CreateApiKeyClient();

        var post = await client.PostAsJsonAsync("/api/urls", new { url = "https://example.com/list-test" });
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        var response = await client.GetAsync("/api/urls");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var urls = await response.Content.ReadFromJsonAsync<List<Url>>();
        Assert.NotNull(urls);
        Assert.True(urls.Count >= 1);
        Assert.All(urls, u => Assert.True(u.IsActive));
    }

    [Fact]
    public async Task GetUrls_WithoutApiKey_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/urls");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUrls_WithWrongApiKey_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "totally-wrong-key");
        var response = await client.GetAsync("/api/urls");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── GET /{slug} (redirect) ────────────────────────────────────────────────

    [Fact]
    public async Task Navigate_UnknownSlug_RedirectsToGoogle()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/xxxxxx");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.StartsWith("https://www.google.com", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Navigate_KnownSlug_RedirectsToOriginalUrl()
    {
        var client = _factory.CreateClient();
        const string longUrl = "https://example.com/navigate-test";

        var postResponse = await client.PostAsJsonAsync("/api/urls", new { url = longUrl });
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var shortUrl = (await postResponse.Content.ReadAsStringAsync()).Trim('"');
        var slug = shortUrl.Split('/').Last();

        var redirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var navResponse = await redirectClient.GetAsync($"/{slug}");

        Assert.Equal(HttpStatusCode.Found, navResponse.StatusCode);
        Assert.Equal(longUrl, navResponse.Headers.Location?.ToString());
    }

    // ── Case-sensitivity (Postgres is case-sensitive; SQL Server was not) ─────

    [Fact]
    public async Task Navigate_WrongCaseSlug_RedirectsToGoogle()
    {
        var client = _factory.CreateClient();
        const string longUrl = "https://example.com/case-sensitivity-test";

        var postResponse = await client.PostAsJsonAsync("/api/urls", new { url = longUrl });
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var shortUrl = (await postResponse.Content.ReadAsStringAsync()).Trim('"');
        var slug = shortUrl.Split('/').Last();

        // Flip the case of every character
        var wrongCaseSlug = new string(slug.Select(c =>
            char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c)).ToArray());

        // If the slug happens to be all digits, flipping case produces the same string —
        // skip this test to avoid a false positive (extremely rare with 6 base-62 chars).
        if (wrongCaseSlug == slug) return;

        var redirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var navResponse = await redirectClient.GetAsync($"/{wrongCaseSlug}");

        Assert.Equal(HttpStatusCode.Found, navResponse.StatusCode);
        Assert.StartsWith("https://www.google.com", navResponse.Headers.Location?.ToString());
    }

    // ── CORS ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cors_AllowedOrigin_ReturnsAccessControlAllowOriginHeader()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/urls");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        var acao = response.Headers.TryGetValues("Access-Control-Allow-Origin", out var vals)
            ? vals.FirstOrDefault()
            : null;
        Assert.Equal("http://localhost:3000", acao);
    }

    [Fact]
    public async Task Cors_DisallowedOrigin_DoesNotReturnAccessControlHeader()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/urls");
        request.Headers.Add("Origin", "https://evil-site.example.com");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        var hasCorsHeader = response.Headers.Contains("Access-Control-Allow-Origin");
        Assert.False(hasCorsHeader);
    }
}
