namespace SnapDog2.Extensions.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapcastClient;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.Integrations.Snapcast;

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

        // Register a factory for creating SnapcastClient instances
        // This avoids nullable type parameter issues while still handling connection failures gracefully
        services.AddSingleton<Func<SnapcastClient.IClient?>>(serviceProvider =>
        {
            return () =>
            {
                var config = serviceProvider
                    .GetRequiredService<IOptions<SnapDogConfiguration>>()
                    .Value.Services.Snapcast;
                var logger = serviceProvider.GetService<ILogger<Client>>();

                try
                {
                    // Attempt to create TCP connection
                    var connection = new TcpConnection(config.Address, config.JsonRpcPort);
                    return new Client(connection, logger);
                }
                catch (Exception ex)
                {
                    // Log the connection failure but don't fail the entire DI container setup
                    var serviceLogger = serviceProvider.GetService<ILogger<Client>>();
                    serviceLogger?.LogWarning(
                        ex,
                        "Failed to connect to Snapcast server at {Address}:{Port} during startup. Service will retry later.",
                        config.Address,
                        config.JsonRpcPort
                    );

                    // Return null - the SnapcastService will handle this gracefully
                    return null;
                }
            };
        });

        // Register our Snapcast service as singleton with mediator injection
        services.AddSingleton<ISnapcastService>(serviceProvider =>
        {
            var config = serviceProvider.GetRequiredService<IOptions<SnapDogConfiguration>>();
            var stateRepository = serviceProvider.GetRequiredService<ISnapcastStateRepository>();
            var logger = serviceProvider.GetRequiredService<ILogger<SnapcastService>>();
            var clientFactory = serviceProvider.GetRequiredService<Func<SnapcastClient.IClient?>>();

            // Create the client using the factory
            var snapcastClient = clientFactory();

            // Don't resolve IClientManager here - pass IServiceProvider instead
            // SnapcastService will create scopes when it needs to access IClientManager
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
