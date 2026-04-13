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

    [HttpGet("{slug}")]
    public async Task<IActionResult> Navigate(string slug)
    {
        var url = await _context.Urls.SingleOrDefaultAsync(u => u.UrlId == slug && u.IsActive);
        return url == null ? Redirect("https://www.google.com") : Redirect(url.OriginalUrl);
    }
}
