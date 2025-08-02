namespace SnapDog2.Worker.DI;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.Services;

/// <summary>
/// Dependency injection configuration for enterprise MQTT services.
/// </summary>
public static class MqttServiceConfiguration
{
    /// <summary>
    /// Adds MQTT services to the dependency injection container.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddMqttServices(this IServiceCollection services)
    {
        // Register the MQTT service as singleton with proper DI lifetime management
        services.AddSingleton<IMqttService, MqttService>();

        return services;
    }

    /// <summary>
    /// Validates MQTT configuration and dependencies.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection ValidateMqttConfiguration(this IServiceCollection services)
    {
        // Add configuration validation
        services
            .AddOptions<ServicesConfig>()
            .Validate(
                config =>
                {
                    if (!config.Mqtt.Enabled)
                        return true; // Skip validation if MQTT is disabled

                    if (string.IsNullOrWhiteSpace(config.Mqtt.BrokerAddress))
                        return false;

                    if (config.Mqtt.Port <= 0 || config.Mqtt.Port > 65535)
                        return false;

                    if (string.IsNullOrWhiteSpace(config.Mqtt.ClientId))
                        return false;

                    return true;
                },
                "Invalid MQTT configuration"
            );

        return services;
    }
}
