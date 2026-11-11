namespace UrlShortener.Services;

/// <summary>
/// Maps the "expiresIn" values accepted by POST /api/urls to a concrete duration.
/// </summary>
public static class ExpirationOptions
{
    private static readonly Dictionary<string, TimeSpan> Durations = new()
    {
        ["1h"] = TimeSpan.FromHours(1),
        ["1d"] = TimeSpan.FromDays(1),
        ["7d"] = TimeSpan.FromDays(7),
        ["30d"] = TimeSpan.FromDays(30),
    };

    public static string[] ValidValues => Durations.Keys.ToArray();

    /// <summary>
    /// Resolves an "expiresIn" value to an absolute UTC expiry. Null/empty means
    /// "never expires" and resolves successfully to a null expiry. Returns false
    /// for any non-empty value that isn't one of the known options.
    /// </summary>
    public static bool TryResolve(string? expiresIn, out DateTime? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(expiresIn))
        {
            expiresAt = null;
            return true;
        }

        if (Durations.TryGetValue(expiresIn, out var duration))
        {
            expiresAt = DateTime.UtcNow.Add(duration);
            return true;
        }

        expiresAt = null;
        return false;
    }
}
