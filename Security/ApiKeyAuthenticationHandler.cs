using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace UrlShortener.Security;

/// <summary>
/// Authenticates requests by reading the X-Api-Key request header and comparing it
/// against the value stored at Auth:ApiKey in configuration (user-secrets or env var).
/// </summary>
public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyValues))
            return Task.FromResult(AuthenticateResult.Fail("API key header not present."));

        var providedKey = apiKeyValues.ToString();
        var expectedKey = configuration["Auth:ApiKey"];

        if (string.IsNullOrWhiteSpace(expectedKey))
            return Task.FromResult(AuthenticateResult.Fail("API key is not configured on the server."));

        if (!string.Equals(providedKey, expectedKey, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));

        var claims = new[] { new Claim(ClaimTypes.Name, "ApiKeyUser") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
