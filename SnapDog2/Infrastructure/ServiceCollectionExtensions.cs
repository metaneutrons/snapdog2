using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.HealthChecks;
using SnapDog2.Infrastructure.Services;

namespace SnapDog2.Infrastructure;

/// <summary>
/// Extension methods for configuring all infrastructure services in the dependency injection container.
/// Provides centralized service registration for repositories, external services, health checks, and other infrastructure components.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all SnapDog infrastructure services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="systemConfig">The system configuration.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddSnapDogInfrastructure(
        this IServiceCollection services,
        SystemConfiguration systemConfig
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(systemConfig);

        // Add health checks if enabled
        if (systemConfig.HealthChecks.Enabled)
        {
            services.AddSnapDogHealthChecks(systemConfig.HealthChecks);
            services.ConfigureHealthCheckOptions(systemConfig.HealthChecks);
            services.AddHealthCheckFilters();
        }

        // Add logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(Enum.Parse<LogLevel>(systemConfig.LogLevel));
        });

        return services;
    }

    /// <summary>
    /// Adds health check services with readiness and liveness configurations.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="healthCheckConfig">The health check configuration.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddSnapDogHealthCheckServices(
        this IServiceCollection services,
        HealthCheckConfiguration healthCheckConfig
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(healthCheckConfig);

        if (!healthCheckConfig.Enabled)
        {
            return services;
        }

        // Register health check implementations
        services.AddTransient<SnapcastServiceHealthCheck>();
        services.AddTransient<MqttServiceHealthCheck>();
        services.AddTransient<KnxServiceHealthCheck>();

        // Add health checks with different configurations
        services.AddSnapDogHealthChecks(healthCheckConfig);
        services.AddSnapDogReadinessChecks(healthCheckConfig);
        services.AddSnapDogLivenessChecks(healthCheckConfig);

        return services;
    }

    /// <summary>
    /// Adds repository services to the service collection.
    /// This method is a placeholder for future repository service registration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddSnapDogRepositories(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Repository services would be registered here
        // Example:
        // services.AddScoped<IAudioStreamRepository, AudioStreamRepository>();
        // services.AddScoped<IClientRepository, ClientRepository>();
        // services.AddScoped<IZoneRepository, ZoneRepository>();
        // services.AddScoped<IPlaylistRepository, PlaylistRepository>();
        // services.AddScoped<ITrackRepository, TrackRepository>();
        // services.AddScoped<IRadioStationRepository, RadioStationRepository>();

        return services;
    }

    /// <summary>
    /// Adds external service abstractions to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="servicesConfig">The services configuration.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddSnapDogExternalServices(
        this IServiceCollection services,
        ServicesConfiguration servicesConfig
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(servicesConfig);

        // Configure Snapcast service
        services.Configure<SnapcastConfiguration>(config =>
        {
            config.Host = servicesConfig.Snapcast.Host;
            config.Port = servicesConfig.Snapcast.Port;
            config.TimeoutSeconds = servicesConfig.Snapcast.TimeoutSeconds;
            config.ReconnectIntervalSeconds = servicesConfig.Snapcast.ReconnectIntervalSeconds;
            config.AutoReconnect = servicesConfig.Snapcast.AutoReconnect;
        });

        // Configure MQTT service
        services.Configure<ServicesMqttConfiguration>(config =>
        {
            config.Broker = servicesConfig.Mqtt.Broker;
            config.Port = servicesConfig.Mqtt.Port;
            config.Username = servicesConfig.Mqtt.Username;
            config.Password = servicesConfig.Mqtt.Password;
            config.ClientId = servicesConfig.Mqtt.ClientId;
            config.SslEnabled = servicesConfig.Mqtt.SslEnabled;
            config.KeepAliveSeconds = servicesConfig.Mqtt.KeepAliveSeconds;
        });

        // Configure KNX service
        services.Configure<KnxConfiguration>(config =>
        {
            config.Enabled = servicesConfig.Knx.Enabled;
            config.Gateway = servicesConfig.Knx.Gateway;
            config.Port = servicesConfig.Knx.Port;
            config.TimeoutSeconds = servicesConfig.Knx.TimeoutSeconds;
            config.AutoReconnect = servicesConfig.Knx.AutoReconnect;
        });

        // Register external services
        services.AddScoped<ISnapcastService, SnapcastService>();
        services.AddScoped<IMqttService, MqttService>();
        services.AddScoped<MqttDomainEventPublisher>();
        services.AddScoped<IKnxService, KnxService>();

        return services;
    }

    /// <summary>
    /// Adds resilience policies to the service collection.
    /// This method is a placeholder for future resilience policy registration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddSnapDogResiliencePolicies(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Resilience policies would be registered here
        // Example:
        // services.AddSingleton<PolicyFactory>();
        // services.AddHttpClient().AddPolicyHandler(PolicyFactory.GetRetryPolicy());

        return services;
    }

    /// <summary>
    /// Adds all SnapDog infrastructure services with comprehensive configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="systemConfig">The system configuration.</param>
    /// <param name="servicesConfig">The services configuration.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddSnapDogInfrastructureServices(
        this IServiceCollection services,
        SystemConfiguration systemConfig,
        ServicesConfiguration servicesConfig
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(systemConfig);
        ArgumentNullException.ThrowIfNull(servicesConfig);

        // Add core infrastructure
        services.AddSnapDogInfrastructure(systemConfig);

        // Add health checks
        services.AddSnapDogHealthCheckServices(systemConfig.HealthChecks);

        // Add repositories
        services.AddSnapDogRepositories();

        // Add external services
        services.AddSnapDogExternalServices(servicesConfig);

        // Add resilience policies
        services.AddSnapDogResiliencePolicies();

        return services;
    }

    /// <summary>
    /// Validates the service configuration and ensures all required services are registered.
    /// </summary>
    /// <param name="services">The service collection to validate.</param>
    /// <param name="systemConfig">The system configuration.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection ValidateSnapDogServices(
        this IServiceCollection services,
        SystemConfiguration systemConfig
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(systemConfig);

        // Validation logic would go here
        // This could check that required services are registered
        // and that configuration is valid

        return services;
    }
}
