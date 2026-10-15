using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using UrlShortener.Models;

namespace UrlShortener;

// Used by `dotnet ef migrations add` when no live connection string is available.
// Never used at runtime.
public class UrlShortenerContextFactory : IDesignTimeDbContextFactory<UrlShortenerContext>
{
    public UrlShortenerContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<UrlShortenerContext>()
            .UseNpgsql("Host=localhost;Database=urlshortener;Username=postgres;Password=postgres")
            .Options;
        return new UrlShortenerContext(options);
    }
}
