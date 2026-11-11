namespace UrlShortener.Services;

/// <summary>
/// Single source of truth for words that can never be used as a custom alias or
/// collide with a generated slug, because they'd be unreachable (vercel.json routes
/// them to the frontend before the request ever reaches the API) or because they're
/// reserved by the platform itself.
///
/// Keep this in sync with every top-level route under frontend/src/app — this is
/// enforced by UrlShortener.Tests/ReservedAliasesTests.cs, which scans that directory
/// and fails if a route is added here without a matching entry.
/// </summary>
public static class ReservedAliases
{
    public static readonly HashSet<string> Values = new(StringComparer.OrdinalIgnoreCase)
    {
        // Platform / API paths
        "api",
        "health",

        // Frontend routes (frontend/src/app/*)
        "dashboard",
        "link-expired",
        "link-not-found",

        // Reserved for future use, named explicitly in the product spec
        "admin",
        "stats",

        // Next.js internals and well-known static files
        "_next",
        "favicon.ico",
        "robots.txt",
        "sitemap.xml",
    };

    public static bool IsReserved(string value) => Values.Contains(value);
}
