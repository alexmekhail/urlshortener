using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Models;
using UrlShortener.Services;

namespace UrlShortener.Controllers;

[ApiController]
[Route("api/urls")]
public class ShortUrlsController : ControllerBase
{
    private readonly UrlShortenerContext _context;
    private readonly ISlugGenerator _slugGenerator;
    private readonly IConfiguration _configuration;

    public ShortUrlsController(UrlShortenerContext context, ISlugGenerator slugGenerator, IConfiguration configuration)
    {
        _context = context;
        _slugGenerator = slugGenerator;
        _configuration = configuration;
    }

    /// <summary>
    /// Creates a new short URL. No authentication required.
    /// Returns the existing short link (200) if the long URL was already registered,
    /// or the newly created short link (201) otherwise.
    /// Rate-limited to 10 requests per IP per minute.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("link-creation")]
    public async Task<ActionResult<string>> CreateShortUrl([FromBody] ShortUrl request)
    {
        // Validate scheme
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest("URL must be a valid http or https address.");
        }

        // Enforce max length (the DB column is 2000 chars)
        if (request.Url.Length > 2000)
            return BadRequest("URL must not exceed 2000 characters.");

        // Reject self-referral (prevents the shortener being used to obscure its own links)
        var configuredDomain = _configuration["App:ShortUrlDomain"];
        if (!string.IsNullOrWhiteSpace(configuredDomain) &&
            Uri.TryCreate(configuredDomain, UriKind.Absolute, out var shortenerUri) &&
            uri.Host.Equals(shortenerUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("URL must not point to this shortener's own domain.");
        }

        if (!ExpirationOptions.TryResolve(request.ExpiresIn, out var expiresAt))
        {
            return BadRequest(
                $"Invalid expiresIn value. Must be one of: {string.Join(", ", ExpirationOptions.ValidValues)}, or omitted for no expiry.");
        }

        var alias = string.IsNullOrWhiteSpace(request.Alias) ? null : request.Alias;
        if (alias != null)
        {
            if (!AliasValidator.IsValidFormat(alias))
            {
                return BadRequest(
                    $"Alias must be {AliasValidator.MinLength}-{AliasValidator.MaxLength} characters " +
                    "and contain only letters, numbers, hyphens, and underscores.");
            }

            if (ReservedAliases.IsReserved(alias))
                return BadRequest($"'{alias}' is a reserved word and can't be used as an alias.");

            // Case-sensitive, like generated slugs.
            var aliasTaken = await _context.Urls.AnyAsync(u => u.UrlId == alias);
            if (aliasTaken)
                return Conflict($"The alias '{alias}' is already taken.");
        }

        // Duplicate detection: only when no expiry and no custom alias were requested, and only
        // reuses a link that is itself active, not expired, and also has no expiry (same expiry setting).
        if (expiresAt == null && alias == null)
        {
            var existing = await _context.Urls.FirstOrDefaultAsync(u =>
                u.OriginalUrl == request.Url && u.IsActive && u.ExpiresAt == null);
            if (existing != null)
                return Ok(existing.ShortenedUrl);
        }

        var slug = alias ?? await _slugGenerator.GenerateUniqueSlugAsync(_context);
        var domainName = string.IsNullOrWhiteSpace(configuredDomain)
            ? $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}"
            : configuredDomain.TrimEnd('/');

        var url = new Url
        {
            UrlId = slug,
            OriginalUrl = request.Url,
            ShortenedUrl = $"{domainName}/{slug}",
            UserId = 0,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsActive = true,
        };

        _context.Urls.Add(url);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, url.ShortenedUrl);
    }

    /// <summary>
    /// Soft-deletes a short URL by setting IsActive = false.
    /// Requires a valid X-Api-Key header.
    /// </summary>
    [HttpDelete("{slug}")]
    [Authorize(AuthenticationSchemes = "ApiKey")]
    public async Task<ActionResult<string>> DeleteShortUrl(string slug)
    {
        var url = await _context.Urls.FirstOrDefaultAsync(u => u.UrlId == slug);
        if (url == null)
            return NotFound($"Short URL with slug '{slug}' not found.");

        url.IsActive = false;
        await _context.SaveChangesAsync();
        return Ok("Short URL deactivated.");
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<Url>> GetShortUrl(string slug)
    {
        var url = await _context.Urls.SingleOrDefaultAsync(u => u.UrlId == slug);
        return url == null ? NotFound() : Ok(url);
    }

    /// <summary>
    /// Returns all active short URLs ordered newest-first, including a total click count per URL.
    /// Requires a valid X-Api-Key header.
    /// </summary>
    [HttpGet]
    [Authorize(AuthenticationSchemes = "ApiKey")]
    public async Task<ActionResult<List<Url>>> List()
    {
        var urls = await _context.Urls
            .Where(u => u.IsActive)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var slugs = urls.Select(u => u.UrlId).ToList();
        var counts = await _context.Clicks
            .Where(c => slugs.Contains(c.UrlId))
            .GroupBy(c => c.UrlId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        foreach (var url in urls)
            url.ClickCount = counts.GetValueOrDefault(url.UrlId, 0);

        return Ok(urls);
    }

    /// <summary>
    /// Returns click analytics for a single short URL over the trailing <paramref name="days"/> days
    /// (default 30, max 365): total clicks, a daily time series, and referrer/country/device/browser
    /// breakdowns. Requires a valid X-Api-Key header.
    /// </summary>
    [HttpGet("{slug}/stats")]
    [Authorize(AuthenticationSchemes = "ApiKey")]
    public async Task<ActionResult<UrlStatsResponse>> GetStats(string slug, [FromQuery] int days = 30)
    {
        var urlExists = await _context.Urls.AnyAsync(u => u.UrlId == slug);
        if (!urlExists)
            return NotFound($"Short URL with slug '{slug}' not found.");

        var windowDays = Math.Clamp(days <= 0 ? 30 : days, 1, 365);
        var since = DateTime.UtcNow.Date.AddDays(-(windowDays - 1));

        var clicks = await _context.Clicks
            .Where(c => c.UrlId == slug && c.ClickedAt >= since)
            .ToListAsync();

        var clicksPerDay = clicks
            .GroupBy(c => DateOnly.FromDateTime(c.ClickedAt))
            .Select(g => new DailyClickCount(g.Key, g.Count()))
            .OrderBy(d => d.Date)
            .ToList();

        var topReferrers = BreakdownBy(clicks, c => c.ReferrerHost, take: 10);
        var countries = BreakdownBy(clicks, c => c.Country);
        var devices = BreakdownBy(clicks, c => c.DeviceType);
        var browsers = BreakdownBy(clicks, c => c.Browser);

        return Ok(new UrlStatsResponse
        {
            Slug = slug,
            TotalClicks = clicks.Count,
            ClicksPerDay = clicksPerDay,
            TopReferrers = topReferrers,
            Countries = countries,
            Devices = devices,
            Browsers = browsers,
        });
    }

    private static List<CountBreakdown> BreakdownBy(
        List<Click> clicks, Func<Click, string?> keySelector, int? take = null)
    {
        var query = clicks
            .Where(c => !string.IsNullOrEmpty(keySelector(c)))
            .GroupBy(c => keySelector(c)!)
            .Select(g => new CountBreakdown(g.Key, g.Count()))
            .OrderByDescending(b => b.Count);

        return (take.HasValue ? query.Take(take.Value) : query).ToList();
    }
}
