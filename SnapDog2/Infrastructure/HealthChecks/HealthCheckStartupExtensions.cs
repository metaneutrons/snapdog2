using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.HealthChecks.Models;

namespace SnapDog2.Infrastructure.HealthChecks;

/// <summary>
/// Extension methods for configuring health check endpoints and startup behavior.
/// Provides methods to setup readiness and liveness probes with proper JSON formatting.
/// </summary>
public static class HealthCheckStartupExtensions
{
    /// <summary>
    /// Maps health check endpoints for readiness and liveness probes.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    /// <param name="healthCheckConfig">The health check configuration.</param>
    /// <returns>The web application for method chaining.</returns>
    public static WebApplication MapSnapDogHealthChecks(
        this WebApplication app,
        HealthCheckConfiguration healthCheckConfig
    )
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(healthCheckConfig);

        if (!healthCheckConfig.Enabled)
        {
            return app;
        }

        // Map readiness endpoint - checks if the service is ready to receive traffic
        app.MapHealthChecks(
            "/health/ready",
            new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = WriteHealthCheckResponse,
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status200OK,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
                },
            }
        );

        // Map liveness endpoint - checks if the service is alive and not stuck
        app.MapHealthChecks(
            "/health/live",
            new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("live"),
                ResponseWriter = WriteHealthCheckResponse,
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status200OK,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
                },
            }
        );

        // Map comprehensive health endpoint - checks all health checks
        app.MapHealthChecks(
            "/health",
            new HealthCheckOptions
            {
                ResponseWriter = WriteDetailedHealthCheckResponse,
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status200OK,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
                },
            }
        );

        return app;
    }

    /// <summary>
    /// Writes a simple health check response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="report">The health report.</param>
    /// <returns>A task representing the write operation.</returns>
    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow,
            results = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    duration = entry.Value.Duration.TotalMilliseconds,
                    description = entry.Value.Description,
                    tags = entry.Value.Tags,
                }
            ),
        };

        var json = JsonSerializer.Serialize(
            response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true }
        );

        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Writes a detailed health check response with system information.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="report">The health report.</param>
    /// <returns>A task representing the write operation.</returns>
    private static async Task WriteDetailedHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var systemHealth = new SystemHealthStatus
        {
            Status = report.Status,
            TotalDurationMs = (long)report.TotalDuration.TotalMilliseconds,
            Timestamp = DateTime.UtcNow,
            Results = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new HealthCheckResponse
                {
                    Name = entry.Key,
                    Status = entry.Value.Status.ToString(),
                    ResponseTimeMs = (long)entry.Value.Duration.TotalMilliseconds,
                    Description = entry.Value.Description,
                    Error = entry.Value.Exception?.Message,
                    Data =
                        entry.Value.Data.Count > 0
                            ? entry.Value.Data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                            : null,
                    Timestamp = DateTime.UtcNow,
                    Tags = entry.Value.Tags,
                }
            ),
            SystemInfo = GetSystemInfo(),
        };

        var json = JsonSerializer.Serialize(
            systemHealth,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true }
        );

        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Gets system information for health check responses.
    /// </summary>
    /// <returns>A dictionary containing system information.</returns>
    private static Dictionary<string, object> GetSystemInfo()
    {
        return new Dictionary<string, object>
        {
            ["machineName"] = Environment.MachineName,
            ["osVersion"] = Environment.OSVersion.ToString(),
            ["processorCount"] = Environment.ProcessorCount,
            ["workingSet"] = Environment.WorkingSet,
            ["gcTotalMemory"] = GC.GetTotalMemory(false),
            ["uptime"] = Environment.TickCount64,
            ["frameworkVersion"] = Environment.Version.ToString(),
            ["timestamp"] = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Configures health check options with custom filters and predicates.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="healthCheckConfig">The health check configuration.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection ConfigureHealthCheckOptions(
        this IServiceCollection services,
        HealthCheckConfiguration healthCheckConfig
    )
    {
        services.Configure<HealthCheckOptions>(options =>
        {
            options.AllowCachingResponses = false;
            options.ResponseWriter = WriteDetailedHealthCheckResponse;
        });

        return services;
    }

    /// <summary>
    /// Adds health check filters for different endpoint types.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddHealthCheckFilters(this IServiceCollection services)
    {
        // Register named health check options for different endpoints
        services.Configure<HealthCheckOptions>(
            "readiness",
            options =>
            {
                options.Predicate = check => check.Tags.Contains("ready");
                options.ResponseWriter = WriteHealthCheckResponse;
            }
        );

        services.Configure<HealthCheckOptions>(
            "liveness",
            options =>
            {
                options.Predicate = check => check.Tags.Contains("live");
                options.ResponseWriter = WriteHealthCheckResponse;
            }
        );

        return services;
    }
}
