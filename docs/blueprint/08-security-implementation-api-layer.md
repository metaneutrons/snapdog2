# Security Implementation (API Layer)

## 8.1 Security Philosophy & Threat Model

SnapDog2 is designed primarily for operation within a **trusted private local area network (LAN)**, such as a typical home network. The primary security goal for the exposed REST API (defined in Section 11) is **authentication and basic authorization** to prevent accidental or unauthorized control by other devices or users on the same network, rather than defending against sophisticated external threats common to internet-facing applications.

The threat model assumes the network itself is reasonably secure and focuses on ensuring that only explicitly permitted clients (e.g., home automation hubs, specific user interface applications, authorized scripts) can interact with the SnapDog2 API. Sensitive user data is not directly managed or stored by SnapDog2 beyond credentials for external services like Subsonic, which are handled via configuration (Section 10).

Given this context, a simple but effective **API Key authentication** mechanism is employed as the primary security measure for the API layer (`/Api`).

## 8.2 API Key Authentication

Access to the SnapDog2 REST API is controlled via mandatory API Keys. Any client attempting to communicate with protected API endpoints **must** include a valid, pre-configured API key in the `X-API-Key` HTTP request header.

### 8.2.1 API Key Configuration

API keys are managed securely outside the application code, loaded at startup from environment variables. This allows keys to be easily provisioned, rotated, or revoked without code changes.

* **Enabling:** Authentication is enabled by default but can be explicitly disabled via `SNAPDOG_API_AUTH_ENABLED=false` (See Section 10). If disabled, requests to `[Authorize]` endpoints will likely fail unless the default authorization policy is explicitly changed to allow anonymous access (not recommended for typical deployments).
* **Key Definition:** One or more keys are defined using indexed environment variables:

    ```bash
    # Example Environment Variables
    SNAPDOG_API_AUTH_ENABLED=true
    SNAPDOG_API_APIKEY_1="sd-key-for-homeassistant-integration-alksjdhfgqwer"
    SNAPDOG_API_APIKEY_2="sd-key-for-mobile-app-zxcvbnm12345"
    SNAPDOG_API_APIKEY_3="..."
    # Add as many keys as needed for different clients
    ```

* **Loading:** The `/Core/Configuration/ApiAuthConfiguration.cs` class is responsible for loading these keys from the environment variables into a list used for validation. It should also load the `Enabled` flag.

```csharp
// Example: /Core/Configuration/ApiAuthConfiguration.cs
namespace SnapDog2.Core.Configuration;

using System.Collections.Generic;
using System.Linq;
using SnapDog2.Infrastructure; // For EnvConfigHelper

/// <summary>
/// Configuration for API Authentication.
/// </summary>
public class ApiAuthConfiguration
{
    /// <summary>
    /// Gets a value indicating whether API Key authentication is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets the list of valid API keys loaded from the environment.
    /// </summary>
    public List<string> ApiKeys { get; init; } = new();

    /// <summary>
    /// Loads API authentication configuration from environment variables.
    /// </summary>
    /// <returns>An instance of ApiAuthConfiguration.</returns>
    public static ApiAuthConfiguration LoadFromEnvironment()
    {
        var enabled = EnvConfigHelper.GetBool("SNAPDOG_API_AUTH_ENABLED", true); // Default to enabled
        var keys = new List<string>();

        if (enabled) // Only load keys if enabled
        {
            int index = 1;
            while (true)
            {
                var key = Environment.GetEnvironmentVariable($"SNAPDOG_API_APIKEY_{index}");
                if (string.IsNullOrWhiteSpace(key))
                {
                    break; // Stop searching when an indexed key is missing
                }
                keys.Add(key);
                index++;
            }
        }

        return new ApiAuthConfiguration { Enabled = enabled, ApiKeys = keys };
    }
}
```

### 8.2.2 API Key Authentication Implementation (ASP.NET Core)

Authentication is handled by a custom `AuthenticationHandler` registered with the ASP.NET Core authentication middleware. This handler intercepts incoming requests, checks for the `X-API-Key` header, and validates the provided key against the loaded configuration.

```csharp
// Located in /Api/Auth/ApiKeyAuthenticationHandler.cs
namespace SnapDog2.Api.Auth; // Example namespace

using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Configuration; // For ApiAuthConfiguration

/// <summary>
/// Handles API Key authentication for ASP.NET Core.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly ApiAuthConfiguration _apiAuthConfig;
    private readonly ISecurityLogger _securityLogger; // Use dedicated logger interface
    private readonly ILogger<ApiKeyAuthenticationHandler> _logger; // General logger

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyAuthenticationHandler"/> class.
    /// </summary>
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory loggerFactory, // Inject factory
        UrlEncoder encoder,
        ApiAuthConfiguration apiAuthConfig, // Inject loaded config
        ISecurityLogger securityLogger)     // Inject security logger
        : base(options, loggerFactory, encoder)
    {
        _apiAuthConfig = apiAuthConfig ?? throw new ArgumentNullException(nameof(apiAuthConfig));
        _securityLogger = securityLogger ?? throw new ArgumentNullException(nameof(securityLogger));
        _logger = loggerFactory.CreateLogger<ApiKeyAuthenticationHandler>(); // Create specific logger
    }

    /// <summary>
    /// Handles the authentication process for incoming requests.
    /// </summary>
    /// <returns>The authentication result.</returns>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 1. Check if Authentication is enabled globally
        if (!_apiAuthConfig.Enabled)
        {
            _logger.LogTrace("API Key authentication is disabled. Skipping authentication.");
            // If disabled, treat as successful authentication with a system identity
            // to allow [Authorize] attributes to pass without requiring a real key.
            // Alternatively, configure the default policy differently.
             var systemIdentity = new ClaimsIdentity("SystemInternal");
             var systemPrincipal = new ClaimsPrincipal(systemIdentity);
             var systemTicket = new AuthenticationTicket(systemPrincipal, Scheme.Name);
             return Task.FromResult(AuthenticateResult.Success(systemTicket));
        }

        // 2. Check if endpoint allows anonymous access
        var endpoint = Context.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
        {
             _logger.LogTrace("Endpoint allows anonymous access. Skipping authentication.");
            return Task.FromResult(AuthenticateResult.NoResult()); // No auth needed
        }

        // 3. Try to extract API Key from header
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues))
        {
            var reason = $"Missing API Key header: {ApiKeyHeaderName}";
            _securityLogger.LogApiKeyAuthFailure(GetRemoteIp(), reason); // Log security event
            return Task.FromResult(AuthenticateResult.Fail(reason));
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
             var reason = "Provided API Key is empty.";
            _securityLogger.LogApiKeyAuthFailure(GetRemoteIp(), reason);
            return Task.FromResult(AuthenticateResult.Fail(reason));
        }

        // 4. Validate the key against the configured list
        // Note: Use constant-time comparison if protecting against timing attacks,
        // but simple comparison is sufficient for typical LAN environments.
        if (!_apiAuthConfig.ApiKeys.Contains(providedApiKey))
        {
            var reason = "Invalid API Key provided.";
            _securityLogger.LogApiKeyAuthFailure(GetRemoteIp(), reason);
            return Task.FromResult(AuthenticateResult.Fail(reason));
        }

        // 5. Authentication Successful: Create principal and ticket
        _securityLogger.LogApiKeyAuthSuccess(GetRemoteIp());
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, "ApiKeyUser"), // Generic user identifier
            new Claim(ClaimTypes.Name, providedApiKey),         // Include the key itself as a claim (optional)
            new Claim(ClaimTypes.AuthenticationMethod, Scheme.Name)
         };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// Handles challenge response (typically 401 Unauthorized).
    /// </summary>
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        // Optional: Add WWW-Authenticate header if needed for specific clients, usually not for API keys.
        // Response.Headers.Append("WWW-Authenticate", $"{Scheme.Name} realm=\"SnapDog2 API\"");
        _logger.LogDebug("Responding with 401 Unauthorized for API Key challenge.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles forbidden response (typically 403 Forbidden).
    /// </summary>
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        _logger.LogDebug("Responding with 403 Forbidden for API Key authorization failure.");
        // This is usually triggered by authorization policies, not the authentication handler itself.
        return Task.CompletedTask;
    }

    private string GetRemoteIp() => Context.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
}

/// <summary>
/// Options for API Key Authentication Scheme.
/// </summary>
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Default scheme name ("ApiKey").
    /// </summary>
    public const string DefaultScheme = "ApiKey";
}
```

### 8.2.3 API Security Registration (ASP.NET Core)

Authentication and Authorization services are configured in `Program.cs` via DI extension methods (`/Worker/DI/ApiAuthExtensions.cs`).

```csharp
// In /Worker/DI/ApiAuthExtensions.cs
namespace SnapDog2.Worker.DI;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; // For LogInformation/Warning
using SnapDog2.Api.Auth; // Location of handler/options
using SnapDog2.Core.Configuration;

public static class ApiAuthExtensions
{
    public static IServiceCollection AddSnapDogApiKeyAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Load config (assumes already loaded/bound elsewhere, or load here)
        var apiAuthConfig = ApiAuthConfiguration.LoadFromEnvironment();
        services.AddSingleton(apiAuthConfig); // Register for injection into handler

        // Get logger for startup messages
        var serviceProvider = services.BuildServiceProvider(); // Temp provider for logger
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ApiAuthExtensions");

        if (apiAuthConfig.Enabled && apiAuthConfig.ApiKeys.Any())
        {
            logger.LogInformation("API Key Authentication ENABLED with {KeyCount} key(s). Setting up Authentication and Authorization...", apiAuthConfig.ApiKeys.Count);

            services.AddAuthentication(ApiKeyAuthenticationOptions.DefaultScheme)
                .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                    ApiKeyAuthenticationOptions.DefaultScheme, // Scheme name
                    options => { /* No options needed here */ });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiKeyPolicy", policy =>
                {
                    policy.AddAuthenticationSchemes(ApiKeyAuthenticationOptions.DefaultScheme);
                    policy.RequireAuthenticatedUser(); // Core requirement
                    // Example: Require a specific claim if needed later
                    // policy.RequireClaim(ClaimTypes.Name, "SpecificKeyValue");
                });
                // Set the default policy for [Authorize] attributes
                options.DefaultPolicy = options.GetPolicy("ApiKeyPolicy");
                 options.FallbackPolicy = options.GetPolicy("ApiKeyPolicy"); // Require auth by default for all endpoints unless [AllowAnonymous]
            });
        }
        else if(apiAuthConfig.Enabled && !apiAuthConfig.ApiKeys.Any())
        {
             logger.LogWarning("API Key Authentication is ENABLED but NO API keys were configured (SNAPDOG_API_APIKEY_n). API will be inaccessible.");
             // Keep default restrictive policy
              services.AddAuthorization(options => {
                 options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build(); // Requires auth, but no keys means failure
                  options.FallbackPolicy = options.DefaultPolicy;
             });
        }
         else
        {
            logger.LogWarning("API Key Authentication is DISABLED via configuration (SNAPDOG_API_AUTH_ENABLED=false). API endpoints may be unsecured unless other auth is added.");
            // If disabled, configure authorization to allow anonymous by default, otherwise [Authorize] will fail.
            // Or rely on endpoint-specific [AllowAnonymous].
             services.AddAuthorization(options => {
                  options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build(); // Allows all requests globally
                  options.FallbackPolicy = options.DefaultPolicy;
             });
        }

        // Register Security Logger
        services.AddSingleton<ISecurityLogger, SecurityLogger>();

        return services;
    }
}

// In /Worker/Program.cs
// var builder = WebApplication.CreateBuilder(args);
// ...
// builder.Services.AddSnapDogApiKeyAuthentication(builder.Configuration); // Add this call
// ...
// var app = builder.Build();
// ...
// app.UseAuthentication(); // MUST be called before UseAuthorization
// app.UseAuthorization();
// ...
// app.MapControllers().RequireAuthorization(); // Apply policy (can be default or specific)
// ...
```

Controllers requiring protection use the `[Authorize]` attribute.```csharp
// In /Api/Controllers/ZonesController.cs
namespace SnapDog2.Api.Controllers;

using Microsoft.AspNetCore.Authorization; // Required namespace
using Microsoft.AspNetCore.Mvc;
// ... other usings ...

[ApiController]
[Route("api/v1/zones")]
[Authorize] // Apply the default authorization policy (which requires ApiKey if enabled)
public class ZonesController : ControllerBase
{
    // ... Controller methods (GetZone, PlayZone, etc.) ...
}

[ApiController]
[Route("api/v1/system/status")]
[AllowAnonymous] // Example: Allow public access to status endpoint
public class StatusController : ControllerBase
{
     [HttpGet]
     public IActionResult GetStatus() { /*...*/ return Ok(); }
}```

## 8.3 Security Best Practices Applied

In addition to API Key Authentication, SnapDog2 incorporates:

1. **HTTPS Enforcement:** Production deployments **must** be configured to serve the API over HTTPS using a reverse proxy (like Nginx or Traefik) or Kestrel HTTPS configuration with valid certificates. Traffic within the Docker network might be HTTP, but external access must be encrypted.
2. **Input Validation:** All API request models and command payloads are validated using FluentValidation (Section 5.4) to prevent injection attacks, ensure data integrity, and handle malformed requests gracefully.
3. **Rate Limiting:** Implementing rate limiting middleware (e.g., `AspNetCoreRateLimit` NuGet package) is recommended for production deployments to mitigate denial-of-service and brute-force attacks against the API. Configuration should be done in `Program.cs`.
4. **Security Headers:** Standard security headers (`X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Content-Security-Policy`, `Referrer-Policy`, `Permissions-Policy`, etc.) should be configured in the ASP.NET Core middleware pipeline (`Program.cs`) to enhance browser-level security for potential future web interfaces or direct API interactions.
5. **Secrets Management:** API Keys and any other sensitive credentials (MQTT passwords, Subsonic passwords) are loaded exclusively from environment variables or a secure configuration provider, keeping secrets out of source control (Section 10).
6. **Minimal Exposure:** Only necessary API endpoints are exposed publicly. Internal application services and infrastructure components are not directly accessible from outside the application process.
7. **Dependency Scanning:** The CI/CD pipeline includes automated checks for known vulnerabilities in NuGet dependencies using tools like `dotnet list package --vulnerable` or GitHub Dependabot.
8. **Logging:** Security-relevant events (authentication success/failure, authorization failures) are explicitly logged using the `ISecurityLogger` interface (Section 8.4) for auditing and monitoring purposes.

## 8.4 Security Logging (`ISecurityLogger` / `SecurityLogger`)

Provides dedicated logging for security-related events.

```csharp
// Located in /Core/Abstractions/ISecurityLogger.cs
namespace SnapDog2.Core.Abstractions;

/// <summary>
/// Interface for logging security-relevant events.
/// </summary>
public interface ISecurityLogger
{
    /// <summary> Logs successful API Key authentication. </summary>
    void LogApiKeyAuthSuccess(string remoteIpAddress);
    /// <summary> Logs failed API Key authentication. </summary>
    void LogApiKeyAuthFailure(string remoteIpAddress, string reason);
    /// <summary> Logs authorization failure for an authenticated user. </summary>
    void LogAuthorizationFailure(string? identityName, string resource, string action);
    // Add other specific security event logging methods as needed
}

// Located in /Infrastructure/Logging/SecurityLogger.cs
namespace SnapDog2.Infrastructure.Logging; // Example namespace

using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using System;

/// <summary>
/// Implementation of ISecurityLogger using ILogger.
/// Must be partial for LoggerMessage generation.
/// </summary>
public partial class SecurityLogger : ISecurityLogger
{
    private readonly ILogger<SecurityLogger> _logger;

    // Logger Messages
    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "API Key authentication SUCCEEDED for RemoteIP: {RemoteIpAddress}")]
    private partial void LogAuthSuccess(string remoteIpAddress);

    [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "API Key authentication FAILED for RemoteIP: {RemoteIpAddress}. Reason: {FailureReason}")]
    private partial void LogAuthFailure(string remoteIpAddress, string failureReason);

     [LoggerMessage(EventId = 12, Level = LogLevel.Warning, Message = "Authorization FAILED for User '{IdentityName}' attempting Action '{Action}' on Resource '{Resource}'.")]
    private partial void LogAuthZFailure(string identityName, string action, string resource);


    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityLogger"/> class.
    /// </summary>
    public SecurityLogger(ILogger<SecurityLogger> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void LogApiKeyAuthSuccess(string remoteIpAddress)
    {
        LogAuthSuccess(remoteIpAddress ?? "Unknown");
    }

    /// <inheritdoc />
    public void LogApiKeyAuthFailure(string remoteIpAddress, string reason)
    {
        LogAuthFailure(remoteIpAddress ?? "Unknown", reason);
    }

    /// <inheritdoc />
    public void LogAuthorizationFailure(string? identityName, string resource, string action)
    {
         LogAuthZFailure(identityName ?? "Anonymous/Unknown", action, resource);
    }
}
```

Register in DI: `services.AddSingleton<ISecurityLogger, SecurityLogger>();`

This security implementation provides essential API protection suitable for the intended trusted network environment, incorporating standard security practices and allowing for future enhancements if needed.
