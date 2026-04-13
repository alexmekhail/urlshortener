using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Models;
using Xunit;

namespace UrlShortener.Tests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    // A predictable key injected into every test host.
    private const string TestApiKey = "test-api-key-for-integration-tests";

    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                // Inject the test API key and allowed CORS origins so the app
                // starts correctly without real user-secrets or env vars.
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:ApiKey"] = TestApiKey,
                    ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
                    ["Cors:AllowedOrigins:1"] = "https://your-app.vercel.app",
                });
            });

            builder.ConfigureServices(services =>
            {
                // Replace SQL Server with an in-memory database.
                // The DB name is captured outside the options lambda so all requests
                // within a single test share the same in-memory store.
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<UrlShortenerContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                var dbName = "IntegrationTestDb_" + Guid.NewGuid();
                services.AddDbContext<UrlShortenerContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            });
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Creates a client that sends the test API key on every request.</summary>
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
        // No auth header at all — POST must be publicly accessible.
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/urls", new { url = "https://example.com/anon-test" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        // Slugs now live at the root — e.g. http://localhost/aBcDeF
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

        // First call creates the entry (201); second call finds it (200).
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
        var client = _factory.CreateClient();   // no X-Api-Key header

        var response = await client.DeleteAsync("/api/urls/someslug");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithApiKey_DeactivatesUrl()
    {
        var client = CreateApiKeyClient();
        const string longUrl = "https://example.com/delete-test";

        // Create a short URL first.
        var postResponse = await client.PostAsJsonAsync("/api/urls", new { url = longUrl });
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var shortUrl = (await postResponse.Content.ReadAsStringAsync()).Trim('"');
        var slug = shortUrl.Split('/').Last();

        // Soft-delete it.
        var deleteResponse = await client.DeleteAsync($"/api/urls/{slug}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        // Navigating to the slug now redirects to the fallback (Google).
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

        // Seed one URL so the list is non-empty.
        var post = await client.PostAsJsonAsync("/api/urls", new { url = "https://example.com/list-test" });
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        var response = await client.GetAsync("/api/urls");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var urls = await response.Content.ReadFromJsonAsync<List<Url>>();
        Assert.NotNull(urls);
        Assert.True(urls.Count >= 1);
        // Every returned record must be active (the endpoint filters IsActive = true).
        Assert.All(urls, u => Assert.True(u.IsActive));
    }

    [Fact]
    public async Task GetUrls_WithoutApiKey_Returns401()
    {
        var client = _factory.CreateClient();   // no X-Api-Key header

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

        // Slugs are now at the root — no /navigate/ prefix.
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

    // ── CORS ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cors_AllowedOrigin_ReturnsAccessControlAllowOriginHeader()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/urls");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        // The CORS middleware must echo back the allowed origin.
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
