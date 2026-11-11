using UrlShortener.Services;
using Xunit;

namespace UrlShortener.Tests;

/// <summary>
/// Fails if a new top-level frontend route is added under frontend/src/app without a
/// matching entry in ReservedAliases — otherwise a custom alias could shadow it
/// (vercel.json would route requests for that path to the frontend, making the alias
/// unreachable) or collide with it in confusing ways.
/// </summary>
public class ReservedAliasesTests
{
    [Fact]
    public void AllTopLevelFrontendRoutes_AreReserved()
    {
        var appDir = FindFrontendAppDirectory();

        var topLevelRouteNames = Directory.GetDirectories(appDir)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            // Route groups "(group)" and dynamic segments "[param]" aren't literal,
            // reservable path segments — only named folders are.
            .Where(name => !name!.StartsWith('(') && !name!.StartsWith('['))
            .Select(name => name!)
            .ToList();

        // Sanity check: if this comes back empty, the scan is broken (wrong path, moved
        // directory, etc.) and the test would otherwise pass vacuously.
        Assert.NotEmpty(topLevelRouteNames);

        foreach (var route in topLevelRouteNames)
        {
            Assert.True(
                ReservedAliases.IsReserved(route),
                $"Frontend route '/{route}' exists under frontend/src/app but is not in " +
                $"ReservedAliases.Values — a custom alias with that name would collide with it. " +
                $"Add \"{route}\" to Services/ReservedAliases.cs.");
        }
    }

    private static string FindFrontendAppDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "urlshortener.sln")))
            dir = dir.Parent;

        if (dir == null)
        {
            throw new InvalidOperationException(
                "Could not locate the repository root (urlshortener.sln) from the test output directory.");
        }

        var appDir = Path.Combine(dir.FullName, "frontend", "src", "app");
        if (!Directory.Exists(appDir))
            throw new InvalidOperationException($"Expected frontend app directory not found: {appDir}");

        return appDir;
    }
}
