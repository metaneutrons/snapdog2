namespace SnapDog2.Api.Authentication;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

/// <summary>
/// Authentication handler for API key authentication.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ApiKeyAuthenticationHandler"/> class.
/// </remarks>
public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration
) : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly IConfiguration _configuration = configuration;

    /// <inheritdoc/>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!this.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (this.IsValidApiKey(providedApiKey))
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "ApiKeyUser"),
                new Claim(ClaimTypes.NameIdentifier, providedApiKey),
            };

            var identity = new ClaimsIdentity(claims, this.Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, this.Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));
    }

    private bool IsValidApiKey(string providedApiKey)
    {
        // Check against configured API keys
        var configuredKeys = new List<string>();

        // Load API keys from configuration
        for (var i = 1; i <= 10; i++) // Support up to 10 API keys
        {
            var key = this._configuration[$"SNAPDOG_API_APIKEY_{i}"];
            if (!string.IsNullOrEmpty(key))
            {
                configuredKeys.Add(key);
            }
        }

        // Fallback to default key if none configured
        if (configuredKeys.Count == 0)
        {
            var defaultKey = this._configuration["SNAPDOG_API_APIKEY"] ?? "snapdog-dev-key";
            configuredKeys.Add(defaultKey);
        }

        return configuredKeys.Contains(providedApiKey);
    }
}

/// <summary>
/// Options for API key authentication scheme.
/// </summary>
public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions { }
