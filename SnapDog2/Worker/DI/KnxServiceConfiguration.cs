namespace SnapDog2.Worker.DI;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Infrastructure.Services;

/// <summary>
/// Dependency injection configuration for KNX service.
/// </summary>
public static class KnxServiceConfiguration
{
    /// <summary>
    /// Adds KNX service to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The SnapDog configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKnxService(this IServiceCollection services, SnapDogConfiguration configuration)
    {
        var knxConfig = configuration.Services.Knx;
        var logger = services
            .BuildServiceProvider()
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("KnxServiceConfiguration");

        if (!knxConfig.Enabled)
        {
            logger.LogInformation("KNX service is disabled via configuration");

            // Register a no-op implementation when disabled
            services.AddSingleton<IKnxService, NoOpKnxService>();
            return services;
        }

        // Validate configuration
        var validationResult = ValidateKnxConfiguration(knxConfig, configuration.Zones, configuration.Clients);
        if (!validationResult.IsValid)
        {
            logger.LogError(
                "KNX configuration validation failed: {Errors}",
                string.Join(", ", validationResult.Errors)
            );

            // Register no-op service on validation failure
            services.AddSingleton<IKnxService, NoOpKnxService>();
            return services;
        }

        logger.LogInformation(
            "Registering KNX service with {ConnectionType} connection",
            knxConfig.ConnectionType switch
            {
                KnxConnectionType.Tunnel => "IP Tunneling",
                KnxConnectionType.Router => "IP Routing",
                KnxConnectionType.Usb => "USB",
                _ => "Unknown",
            }
        );

        if (!string.IsNullOrEmpty(knxConfig.Gateway))
        {
            var connectionTypeText = knxConfig.ConnectionType switch
            {
                KnxConnectionType.Tunnel => "IP Tunneling",
                KnxConnectionType.Router => "IP Routing",
                _ => "IP Connection",
            };
            logger.LogInformation(
                "KNX {ConnectionType}: {Gateway}:{Port}",
                connectionTypeText,
                knxConfig.Gateway,
                knxConfig.Port
            );
        }

        // Count configured KNX zones and clients
        var knxZoneCount = configuration.Zones.Count(z => z.Knx.Enabled);
        var knxClientCount = configuration.Clients.Count(c => c.Knx.Enabled);

        logger.LogInformation(
            "KNX integration configured for {ZoneCount} zones and {ClientCount} clients",
            knxZoneCount,
            knxClientCount
        );

        // Register the actual KNX service
        services.AddSingleton<IKnxService, KnxService>();

        return services;
    }

    private static ValidationResult ValidateKnxConfiguration(
        KnxConfig knxConfig,
        List<ZoneConfig> zones,
        List<ClientConfig> clients
    )
    {
        var errors = new List<string>();

        // Validate connection configuration
        if (string.IsNullOrEmpty(knxConfig.Gateway))
        {
            // USB connection - no additional validation needed
            // The service will check for available USB devices at runtime
        }
        else
        {
            // IP connection validation
            if (knxConfig.Port <= 0 || knxConfig.Port > 65535)
            {
                errors.Add($"Invalid KNX port: {knxConfig.Port}. Must be between 1 and 65535.");
            }
        }

        // Validate timeout
        if (knxConfig.Timeout <= 0)
        {
            errors.Add($"Invalid KNX timeout: {knxConfig.Timeout}. Must be greater than 0.");
        }

        // Validate zone KNX configurations
        for (int i = 0; i < zones.Count; i++)
        {
            var zone = zones[i];
            if (zone.Knx.Enabled)
            {
                ValidateGroupAddresses(zone.Knx, $"Zone {i + 1}", errors);
            }
        }

        // Validate client KNX configurations
        for (int i = 0; i < clients.Count; i++)
        {
            var client = clients[i];
            if (client.Knx.Enabled)
            {
                ValidateGroupAddresses(client.Knx, $"Client {i + 1}", errors);
            }
        }

        return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
    }

    private static void ValidateGroupAddresses(ZoneKnxConfig knxConfig, string context, List<string> errors)
    {
        ValidateGroupAddress(knxConfig.Volume, $"{context} Volume", errors);
        ValidateGroupAddress(knxConfig.VolumeStatus, $"{context} VolumeStatus", errors);
        ValidateGroupAddress(knxConfig.Mute, $"{context} Mute", errors);
        ValidateGroupAddress(knxConfig.MuteStatus, $"{context} MuteStatus", errors);
        ValidateGroupAddress(knxConfig.Play, $"{context} Play", errors);
        ValidateGroupAddress(knxConfig.Pause, $"{context} Pause", errors);
        ValidateGroupAddress(knxConfig.Stop, $"{context} Stop", errors);
        ValidateGroupAddress(knxConfig.TrackNext, $"{context} TrackNext", errors);
        ValidateGroupAddress(knxConfig.TrackPrevious, $"{context} TrackPrevious", errors);
        ValidateGroupAddress(knxConfig.ControlStatus, $"{context} ControlStatus", errors);
    }

    private static void ValidateGroupAddresses(ClientKnxConfig knxConfig, string context, List<string> errors)
    {
        ValidateGroupAddress(knxConfig.Volume, $"{context} Volume", errors);
        ValidateGroupAddress(knxConfig.VolumeStatus, $"{context} VolumeStatus", errors);
        ValidateGroupAddress(knxConfig.Mute, $"{context} Mute", errors);
        ValidateGroupAddress(knxConfig.MuteStatus, $"{context} MuteStatus", errors);
        ValidateGroupAddress(knxConfig.ConnectedStatus, $"{context} ConnectedStatus", errors);
    }

    private static void ValidateGroupAddress(string? groupAddress, string context, List<string> errors)
    {
        if (string.IsNullOrEmpty(groupAddress))
        {
            return; // Optional addresses are allowed to be empty
        }

        // Basic KNX group address format validation (x/y/z where x,y,z are numbers)
        var parts = groupAddress.Split('/');
        if (parts.Length != 3)
        {
            errors.Add($"Invalid group address format for {context}: '{groupAddress}'. Expected format: 'x/y/z'");
            return;
        }

        for (int i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out var value) || value < 0)
            {
                errors.Add(
                    $"Invalid group address part {i + 1} for {context}: '{parts[i]}'. Must be a non-negative integer."
                );
                return;
            }

            // KNX group address range validation
            var maxValue = i switch
            {
                0 => 31, // Main group: 0-31
                1 => 7, // Middle group: 0-7
                2 => 255, // Sub group: 0-255
                _ => 0,
            };

            if (value > maxValue)
            {
                errors.Add(
                    $"Group address part {i + 1} out of range for {context}: {value}. Maximum allowed: {maxValue}"
                );
                return;
            }
        }
    }

    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}

/// <summary>
/// No-operation KNX service implementation used when KNX is disabled or configuration is invalid.
/// </summary>
internal class NoOpKnxService : IKnxService
{
    public bool IsConnected => false;
    public ServiceStatus Status => ServiceStatus.Disabled;

    public Task<Result> InitializeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success());

    public Task<Result> StopAsync(CancellationToken cancellationToken = default) => Task.FromResult(Result.Success());

    public Task<Result> SendStatusAsync(
        string statusId,
        int targetId,
        object value,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(Result.Success());

    public Task<Result> WriteGroupValueAsync(
        string groupAddress,
        object value,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(Result.Success());

    public Task<Result<object>> ReadGroupValueAsync(
        string groupAddress,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(Result<object>.Failure("KNX service is disabled"));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
