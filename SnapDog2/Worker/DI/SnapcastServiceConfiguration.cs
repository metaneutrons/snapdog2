namespace SnapDog2.Worker.DI;

using Cortex.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapcastClient;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.Services;

/// <summary>
/// Extension methods for configuring Snapcast services.
/// </summary>
public static class SnapcastServiceConfiguration
{
    /// <summary>
    /// Adds Snapcast services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSnapcastServices(this IServiceCollection services)
    {
        // Register the state repository as singleton since it holds shared state
        services.AddSingleton<ISnapcastStateRepository, SnapcastStateRepository>();

        // Register the enterprise SnapcastClient client
        // We need to register it as a factory since we need access to configuration
        services.AddSingleton<SnapcastClient.IClient>(serviceProvider =>
        {
            var config = serviceProvider.GetRequiredService<IOptions<SnapDogConfiguration>>().Value.Services.Snapcast;
            var logger = serviceProvider.GetService<ILogger<Client>>();

            // Create the options with resilience settings
            var options = new SnapcastClientOptions
            {
                EnableAutoReconnect = config.AutoReconnect,
                MaxRetryAttempts = 5,
                ConnectionTimeoutMs = config.Timeout * 1000,
                HealthCheckIntervalMs = 30000,
                ReconnectDelayMs = config.ReconnectInterval * 1000,
            };

            // Create the connection with resilience
            var connectionLogger = serviceProvider.GetService<ILogger<ResilientTcpConnection>>();
            var connection = new ResilientTcpConnection(config.Address, config.JsonRpcPort, options, connectionLogger);

            // Create and return the client
            return new Client(connection, logger);
        });

        // Register our Snapcast service as singleton with mediator injection
        services.AddSingleton<ISnapcastService>(serviceProvider =>
        {
            var config = serviceProvider.GetRequiredService<IOptions<SnapDogConfiguration>>();
            var stateRepository = serviceProvider.GetRequiredService<ISnapcastStateRepository>();
            var logger = serviceProvider.GetRequiredService<ILogger<SnapcastService>>();
            var snapcastClient = serviceProvider.GetRequiredService<SnapcastClient.IClient>();

            return new SnapcastService(config, serviceProvider, stateRepository, logger, snapcastClient);
        });

        return services;
    }

    /// <summary>
    /// Validates Snapcast configuration.
    /// </summary>
    /// <param name="config">The Snapcast configuration to validate.</param>
    /// <returns>True if configuration is valid, false otherwise.</returns>
    public static bool ValidateSnapcastConfiguration(SnapcastConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Address))
        {
            return false;
        }

        if (config.JsonRpcPort <= 0 || config.JsonRpcPort > 65535)
        {
            return false;
        }

        if (config.Timeout <= 0)
        {
            return false;
        }

        if (config.ReconnectInterval <= 0)
        {
            return false;
        }

        return true;
    }
}
