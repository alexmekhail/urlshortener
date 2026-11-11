using UrlShortener.Services;

namespace UrlShortener.Tests.Fakes;

/// <summary>
/// Test double for IUrlSafetyChecker — never calls Google. Defaults to "safe"; set
/// IsSafe = false to simulate a flagged URL.
/// </summary>
public class FakeUrlSafetyChecker : IUrlSafetyChecker
{
    public bool IsSafe { get; set; } = true;

    public Task<bool> IsSafeAsync(string url, CancellationToken cancellationToken = default) =>
        Task.FromResult(IsSafe);
}
