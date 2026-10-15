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

        // Duplicate detection: return the existing short link instead of creating a new one.
        var existing = await _context.Urls.FirstOrDefaultAsync(u => u.OriginalUrl == request.Url && u.IsActive);
        if (existing != null)
            return Ok(existing.ShortenedUrl);

        var slug = await _slugGenerator.GenerateUniqueSlugAsync(_context);
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
    /// Returns all active short URLs ordered newest-first.
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
        return Ok(urls);
    }
}
