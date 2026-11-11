using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Models;
using UrlShortener.Services;

namespace UrlShortener.Controllers;

[ApiController]
[Route("")]   // slugs live at the root: GET /{slug}
public class NavigateController : ControllerBase
{
    private readonly UrlShortenerContext _context;
    private readonly IClickEventQueue _clickQueue;
    private readonly IConfiguration _configuration;

    public NavigateController(UrlShortenerContext context, IClickEventQueue clickQueue, IConfiguration configuration)
    {
        _context = context;
        _clickQueue = clickQueue;
        _configuration = configuration;
    }

    // length(6) constraint ensures known frontend routes (e.g. /dashboard) never match here.
    // All slugs are exactly 6 characters (see SlugGenerator.SlugLength).
    [HttpGet("{slug:length(6)}")]
    public async Task<IActionResult> Navigate(string slug)
    {
        var url = await _context.Urls.SingleOrDefaultAsync(u => u.UrlId == slug);

        // Unknown or deactivated slugs land on the same "not found" page — deactivated
        // links are intentionally indistinguishable from ones that never existed.
        if (url == null || !url.IsActive)
            return Redirect(BuildFrontendUrl("/link-not-found"));

        if (url.ExpiresAt.HasValue && url.ExpiresAt.Value <= DateTime.UtcNow)
            return Redirect(BuildFrontendUrl("/link-expired"));

        EnqueueClick(slug);

        // Every click must reach the API to be recorded, so redirects are not CDN/browser-cached.
        // Tradeoff: higher load per click (no CDN offload) in exchange for accurate analytics.
        // See README "Click Analytics" section.
        Response.Headers["Cache-Control"] = "private, no-store";
        return Redirect(url.OriginalUrl);
    }

    private string BuildFrontendUrl(string path)
    {
        var configuredDomain = _configuration["App:ShortUrlDomain"];
        var domainName = string.IsNullOrWhiteSpace(configuredDomain)
            ? $"{Request.Scheme}://{Request.Host}"
            : configuredDomain.TrimEnd('/');
        return $"{domainName}{path}";
    }

    // Enqueues a best-effort analytics event; never blocks or slows the redirect.
    // Intentionally does not record the client's raw IP address.
    private void EnqueueClick(string slug)
    {
        var referrerHost = TryGetHost(Request.Headers["Referer"].ToString());
        var country = Request.Headers["x-vercel-ip-country"].FirstOrDefault();
        var (deviceType, browser) = UserAgentParser.Parse(Request.Headers["User-Agent"].ToString());

        _clickQueue.Enqueue(new ClickEvent(slug, DateTime.UtcNow, referrerHost, country, deviceType, browser));
    }

    private static string? TryGetHost(string? referrer) =>
        !string.IsNullOrWhiteSpace(referrer) && Uri.TryCreate(referrer, UriKind.Absolute, out var uri)
            ? uri.Host
            : null;
}
