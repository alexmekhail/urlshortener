using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Models;

namespace UrlShortener.Controllers;

[ApiController]
[Route("")]   // slugs live at the root: GET /{slug}
public class NavigateController : ControllerBase
{
    private readonly UrlShortenerContext _context;

    public NavigateController(UrlShortenerContext context)
    {
        _context = context;
    }

    // length(6) constraint ensures known frontend routes (e.g. /dashboard) never match here.
    // All slugs are exactly 6 characters (see SlugGenerator.SlugLength).
    [HttpGet("{slug:length(6)}")]
    public async Task<IActionResult> Navigate(string slug)
    {
        var url = await _context.Urls.SingleOrDefaultAsync(u => u.UrlId == slug && u.IsActive);
        return url == null ? Redirect("https://www.google.com") : Redirect(url.OriginalUrl);
    }
}
