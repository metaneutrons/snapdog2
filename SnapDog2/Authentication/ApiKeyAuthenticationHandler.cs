namespace SnapDog2.Authentication;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Configuration;

/// <summary>
/// API Key authentication handler that validates requests against configured API keys.
/// Supports both header-based and query parameter-based API key authentication.
/// </summary>
public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ApiConfig apiConfig
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private const string ApiKeyQueryParameter = "apikey";

    private readonly ApiConfig _apiConfig = apiConfig;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Try to get API key from header first
        string? apiKey = this.Request.Headers[ApiKeyHeaderName].FirstOrDefault();

        // If not in header, try query parameter
        if (string.IsNullOrEmpty(apiKey))
        {
            apiKey = this.Request.Query[ApiKeyQueryParameter].FirstOrDefault();
        }

        // If no API key provided
        if (string.IsNullOrEmpty(apiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API key not provided"));
        }

        // Validate API key against configured keys
        if (!this._apiConfig.ApiKeys.Contains(apiKey))
        {
            this.Logger.LogWarning(
                "Invalid API key attempted: {ApiKey}",
                string.Concat(apiKey.AsSpan(0, Math.Min(8, apiKey.Length)), "...")
            );
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        // Create authenticated identity
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "ApiKeyUser"),
            new Claim(ClaimTypes.NameIdentifier, $"apikey-{apiKey.GetHashCode():X}"),
            new Claim("scope", "api"),
            new Claim("auth_method", "apikey"),
        };

        var identity = new ClaimsIdentity(claims, this.Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, this.Scheme.Name);

        this.Logger.LogDebug("API key authentication successful");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
