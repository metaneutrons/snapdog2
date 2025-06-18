using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.Data;

namespace SnapDog2.Infrastructure.HealthChecks;

/// <summary>
/// Extension methods for configuring SnapDog health checks.
/// Provides centralized registration and configuration of all system health checks.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds all SnapDog health checks to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add health checks to.</param>
    /// <param name="healthCheckConfig">The health check configuration.</param>
    /// <returns>The health checks builder for further configuration.</returns>
    public static IHealthChecksBuilder AddSnapDogHealthChecks(
        this IServiceCollection services,
        HealthCheckConfiguration healthCheckConfig
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(healthCheckConfig);

        if (!healthCheckConfig.Enabled)
        {
            // Return an empty builder if health checks are disabled
            return services.AddHealthChecks();
        }

        var builder = services.AddHealthChecks();

        // Add database health check
        builder.AddCheck<DatabaseHealthCheck>(
            name: "database",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "ready", "live", "database" },
            timeout: healthCheckConfig.DatabaseTimeout
        );

        // Add Snapcast service health check
        builder.AddCheck<SnapcastServiceHealthCheck>(
            name: "snapcast",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "ready", "external", "snapcast" },
            timeout: healthCheckConfig.ExternalServiceTimeout
        );

        // Add MQTT service health check
        builder.AddCheck<MqttServiceHealthCheck>(
            name: "mqtt",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "ready", "external", "mqtt" },
            timeout: healthCheckConfig.ExternalServiceTimeout
        );

        // Add KNX service health check
        builder.AddCheck<KnxServiceHealthCheck>(
            name: "knx",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "ready", "external", "knx" },
            timeout: healthCheckConfig.ExternalServiceTimeout
        );

        // Note: Entity Framework Core health check would be added here if needed
        // This requires the Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore package
        // which provides the AddDbContextCheck extension method

        return builder;
    }

    /// <summary>
    /// Adds readiness health checks (checks required for the service to be ready to receive traffic).
    /// </summary>
    /// <param name="services">The service collection to add health checks to.</param>
    /// <param name="healthCheckConfig">The health check configuration.</param>
    /// <returns>The health checks builder for further configuration.</returns>
    public static IHealthChecksBuilder AddSnapDogReadinessChecks(
        this IServiceCollection services,
        HealthCheckConfiguration healthCheckConfig
    )
    {
        return services
            .AddSnapDogHealthChecks(healthCheckConfig)
            .AddTypeActivatedCheck<DatabaseHealthCheck>(
                name: "database-readiness",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "ready" },
                timeout: healthCheckConfig.DatabaseTimeout
            );
    }

    /// <summary>
    /// Adds liveness health checks (checks that verify the service is alive and not stuck).
    /// </summary>
    /// <param name="services">The service collection to add health checks to.</param>
    /// <param name="healthCheckConfig">The health check configuration.</param>
    /// <returns>The health checks builder for further configuration.</returns>
    public static IHealthChecksBuilder AddSnapDogLivenessChecks(
        this IServiceCollection services,
        HealthCheckConfiguration healthCheckConfig
    )
    {
        return services
            .AddHealthChecks()
            .AddTypeActivatedCheck<DatabaseHealthCheck>(
                name: "database-liveness",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "live" },
                timeout: TimeSpan.FromSeconds(5)
            ); // Shorter timeout for liveness
    }

    /// <summary>
    /// Configures health check policies and failure behaviors.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="healthCheckConfig">The health check configuration.</param>
    /// <returns>The health checks builder for further configuration.</returns>
    public static IHealthChecksBuilder ConfigureHealthCheckPolicies(
        this IHealthChecksBuilder builder,
        HealthCheckConfiguration healthCheckConfig
    )
    {
        // Configuration would be applied during service registration
        // The actual policy configuration is handled by the health check service
        return builder;
    }

    /// <summary>
    /// Adds network connectivity health checks for external dependencies.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="healthCheckConfig">The health check configuration.</param>
    /// <returns>The health checks builder for further configuration.</returns>
    public static IHealthChecksBuilder AddNetworkHealthChecks(
        this IHealthChecksBuilder builder,
        HealthCheckConfiguration healthCheckConfig
    )
    {
        // These would typically be configured with actual network endpoints
        // For now, we'll provide the structure for future implementation

        // Example: Add ping health check for critical network dependencies
        // builder.AddPingHealthCheck(options =>
        // {
        //     options.AddHost("snapcast-server", 1000);
        //     options.AddHost("mqtt-broker", 1000);
        //     options.AddHost("knx-gateway", 1000);
        // }, name: "network-connectivity", tags: new[] { "network", "external" });

        return builder;
    }
}
