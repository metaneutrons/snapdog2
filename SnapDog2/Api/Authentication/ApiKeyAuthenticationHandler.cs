using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.Http.Headers;
using SnapDog2.Core.Configuration;

namespace SnapDog2.Api.Authentication;

/// <summary>
/// Custom authentication handler for API Key authentication.
/// Validates the X-API-Key header against configured API keys.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
{
    private const string ApiKeyHeaderName = "X-API-Key";
    public const string SchemeName = "ApiKey"; // public for external reference

    private readonly ApiConfiguration.ApiAuthSettings _authConfig;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ApiConfiguration.ApiAuthSettings authConfig
    )
        : base(options, logger, encoder)
    {
        _authConfig = authConfig ?? throw new ArgumentNullException(nameof(authConfig));
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? providedApiKey = null;
        // Check X-API-Key header first
        if (Request.Headers.TryGetValue(ApiKeyHeaderName, out var headerValues))
        {
            providedApiKey = headerValues.FirstOrDefault();
        }
        // Fallback to Authorization header with ApiKey scheme
        else if (Request.Headers.ContainsKey("Authorization") &&
                 AuthenticationHeaderValue.TryParse(Request.Headers["Authorization"], out var authHeader) &&
                 string.Equals(authHeader.Scheme, SchemeName, StringComparison.OrdinalIgnoreCase))
        {
            providedApiKey = authHeader.Parameter;
        }
        else
        {
            return Task.FromResult(AuthenticateResult.Fail($"Missing {ApiKeyHeaderName} header or Authorization scheme '{SchemeName}'"));
        }

        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Empty API key"));
        }

        // Validate the API key
        if (_authConfig.ApiKeys == null || !_authConfig.ApiKeys.Contains(providedApiKey))
        {
            Logger.LogWarning("Invalid API key provided: {ApiKey}", providedApiKey);
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        // Create claims for the authenticated user
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "ApiKeyUser"),
            new Claim(ClaimTypes.NameIdentifier, providedApiKey),
            new Claim("ApiKey", providedApiKey),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        Logger.LogDebug("API key authentication successful");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }


    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.Headers["WWW-Authenticate"] = $"{SchemeName} realm=\"SnapDog2 API\"";
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Options for API Key authentication scheme.
/// </summary>
public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    // Additional options can be added here if needed
}
