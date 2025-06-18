using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using SnapDog2.Api.Authentication;
using SnapDog2.Api.Configuration;

namespace SnapDog2.Api.Extensions;

/// <summary>
/// Extension methods for configuring API security features including authentication, authorization, CORS, and security headers.
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// Adds API Key authentication to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddApiSecurity(this IServiceCollection services)
    {
        // Load API authentication configuration
        var authConfig = ApiAuthConfiguration.LoadFromEnvironment();
        services.AddSingleton(authConfig);

        // Add authentication with API Key scheme
        services
            .AddAuthentication("ApiKey")
            .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", options => { });

        // Add authorization
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes("ApiKey")
                .Build();
        });

        return services;
    }

    /// <summary>
    /// Adds CORS configuration for the API.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddApiCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });

            // Add a more restrictive policy for production
            options.AddPolicy(
                "Production",
                builder =>
                {
                    builder
                        .WithOrigins("https://snapdog.local", "https://admin.snapdog.local")
                        .WithMethods("GET", "POST", "PUT", "DELETE")
                        .WithHeaders("Content-Type", "Authorization", "X-API-Key")
                        .AllowCredentials();
                }
            );
        });

        return services;
    }

    /// <summary>
    /// Adds security headers middleware configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddSecurityHeaders(this IServiceCollection services)
    {
        // Security headers will be configured in the middleware pipeline
        return services;
    }

    /// <summary>
    /// Configures the security middleware pipeline.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for method chaining.</returns>
    public static WebApplication UseApiSecurity(this WebApplication app)
    {
        // Add comprehensive security headers
        app.Use(
            async (context, next) =>
            {
                var response = context.Response;
                var isDevelopment = app.Environment.IsDevelopment();

                // Basic security headers
                response.Headers.Append("X-Content-Type-Options", "nosniff");
                response.Headers.Append("X-Frame-Options", "DENY");
                response.Headers.Append("X-XSS-Protection", "1; mode=block");
                response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

                // Strict Transport Security (only for HTTPS)
                if (context.Request.IsHttps)
                {
                    response.Headers.Append(
                        "Strict-Transport-Security",
                        "max-age=31536000; includeSubDomains; preload"
                    );
                }

                // Content Security Policy - more permissive for development
                var csp = isDevelopment
                    ? "default-src 'self' 'unsafe-inline' 'unsafe-eval'; "
                        + "script-src 'self' 'unsafe-inline' 'unsafe-eval'; "
                        + "style-src 'self' 'unsafe-inline'; "
                        + "img-src 'self' data: blob:; "
                        + "font-src 'self' data:; "
                        + "connect-src 'self'"
                    : "default-src 'self'; "
                        + "script-src 'self'; "
                        + "style-src 'self' 'unsafe-inline'; "
                        + "img-src 'self' data:; "
                        + "font-src 'self'; "
                        + "connect-src 'self'; "
                        + "object-src 'none'; "
                        + "base-uri 'self'; "
                        + "form-action 'self'";

                response.Headers.Append("Content-Security-Policy", csp);

                // Permissions Policy (formerly Feature Policy)
                response.Headers.Append(
                    "Permissions-Policy",
                    "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()"
                );

                // Cross-Origin policies
                response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
                response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
                response.Headers.Append("Cross-Origin-Resource-Policy", "cross-origin");

                // Remove potentially revealing headers
                response.Headers.Remove("Server");
                response.Headers.Remove("X-Powered-By");
                response.Headers.Remove("X-AspNet-Version");
                response.Headers.Remove("X-AspNetMvc-Version");

                // Add custom security headers for API
                response.Headers.Append("X-API-Version", "1.0");
                response.Headers.Append("X-Rate-Limit-Policy", "enabled");

                await next();
            }
        );

        // Add CORS with environment-specific configuration
        if (app.Environment.IsDevelopment())
        {
            app.UseCors(); // Use default policy in development
        }
        else
        {
            app.UseCors("Production"); // Use production policy
        }

        // Add authentication and authorization
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
