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
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private const string ApiKeyQueryParameter = "apikey";

    private readonly ApiConfig _apiConfig;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ApiConfig apiConfig
    )
        : base(options, logger, encoder)
    {
        _apiConfig = apiConfig;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Try to get API key from header first
        string? apiKey = Request.Headers[ApiKeyHeaderName].FirstOrDefault();

        // If not in header, try query parameter
        if (string.IsNullOrEmpty(apiKey))
        {
            apiKey = Request.Query[ApiKeyQueryParameter].FirstOrDefault();
        }

        // If no API key provided
        if (string.IsNullOrEmpty(apiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API key not provided"));
        }

        // Validate API key against configured keys
        if (!_apiConfig.ApiKeys.Contains(apiKey))
        {
            Logger.LogWarning(
                "Invalid API key attempted: {ApiKey}",
                apiKey.Substring(0, Math.Min(8, apiKey.Length)) + "..."
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

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        Logger.LogDebug("API key authentication successful");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
