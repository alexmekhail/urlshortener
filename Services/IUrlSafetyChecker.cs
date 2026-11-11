namespace UrlShortener.Services;

/// <summary>
/// Checks whether a target URL is safe to shorten. Implementations fail open:
/// when the check can't be completed (not configured, network error, timeout),
/// they return true rather than block link creation.
/// </summary>
public interface IUrlSafetyChecker
{
    Task<bool> IsSafeAsync(string url, CancellationToken cancellationToken = default);
}
