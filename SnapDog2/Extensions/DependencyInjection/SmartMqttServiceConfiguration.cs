using Microsoft.Extensions.DependencyInjection;
using SnapDog2.Infrastructure.Integrations.Mqtt;

namespace SnapDog2.Extensions.DependencyInjection;

/// <summary>
/// Service collection extensions for smart MQTT publishing configuration.
/// </summary>
public static class SmartMqttServiceConfiguration
{
    /// <summary>
    /// Adds smart MQTT publishing services with hybrid direct/queue approach.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSmartMqttPublishing(this IServiceCollection services)
    {
        // Register the smart MQTT publisher
        services.AddSingleton<ISmartMqttPublisher, SmartMqttPublisher>();

        // Register the unified notification handlers
        services.AddScoped<SnapDog2.Server.Features.Shared.Handlers.SmartMqttNotificationHandlers>();

        return services;
    }
}
