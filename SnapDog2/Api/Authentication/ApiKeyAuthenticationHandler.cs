namespace SnapDog2.Api.Authentication;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

/// <summary>
/// Authentication handler for API key authentication.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyAuthenticationHandler"/> class.
    /// </summary>
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration
    )
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    /// <inheritdoc/>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (IsValidApiKey(providedApiKey))
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "ApiKeyUser"),
                new Claim(ClaimTypes.NameIdentifier, providedApiKey),
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));
    }

    private bool IsValidApiKey(string providedApiKey)
    {
        // Check against configured API keys
        var configuredKeys = new List<string>();

        // Load API keys from configuration
        for (int i = 1; i <= 10; i++) // Support up to 10 API keys
        {
            var key = _configuration[$"SNAPDOG_API_APIKEY_{i}"];
            if (!string.IsNullOrEmpty(key))
            {
                configuredKeys.Add(key);
            }
        }

        // Fallback to default key if none configured
        if (configuredKeys.Count == 0)
        {
            var defaultKey = _configuration["SNAPDOG_API_APIKEY"] ?? "snapdog-dev-key";
            configuredKeys.Add(defaultKey);
        }

        return configuredKeys.Contains(providedApiKey);
    }
}

/// <summary>
/// Options for API key authentication scheme.
/// </summary>
public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions { }
