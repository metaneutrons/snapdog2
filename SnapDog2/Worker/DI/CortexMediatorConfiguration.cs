namespace SnapDog2.Worker.DI;

using System.Reflection;
using Cortex.Mediator.DependencyInjection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SnapDog2.Server.Behaviors;

/// <summary>
/// Extension methods for configuring Cortex.Mediator services.
/// </summary>
public static class CortexMediatorConfiguration
{
    /// <summary>
    /// Adds Cortex.Mediator and related services (handlers, validators, behaviors) to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommandProcessing(this IServiceCollection services)
    {
        // Get the assembly containing the Server layer code
        var serverAssembly = typeof(LoggingCommandBehavior<,>).Assembly;

        // Add Cortex.Mediator using the proper extension method
        services.AddCortexMediator(
            new ConfigurationBuilder().Build().GetSection("Mediator"),
            new[] { typeof(LoggingCommandBehavior<,>) },
            options =>
            {
                // Add default behaviors (logging)
                options.AddDefaultBehaviors();

                // Add custom pipeline behaviors
                options.AddOpenCommandPipelineBehavior(typeof(LoggingCommandBehavior<,>));
                options.AddOpenQueryPipelineBehavior(typeof(LoggingQueryBehavior<,>));
                options.AddOpenCommandPipelineBehavior(typeof(PerformanceCommandBehavior<,>));
                options.AddOpenQueryPipelineBehavior(typeof(PerformanceQueryBehavior<,>));
                options.AddOpenCommandPipelineBehavior(typeof(ValidationCommandBehavior<,>));
                options.AddOpenQueryPipelineBehavior(typeof(ValidationQueryBehavior<,>));
            }
        );

        // Manually register query handlers since auto-discovery isn't working
        services.AddScoped<Server.Features.Global.Handlers.GetSystemStatusQueryHandler>();
        services.AddScoped<Server.Features.Global.Handlers.GetErrorStatusQueryHandler>();
        services.AddScoped<Server.Features.Global.Handlers.GetVersionInfoQueryHandler>();
        services.AddScoped<Server.Features.Global.Handlers.GetServerStatsQueryHandler>();

        // Zone command handlers
        services.AddScoped<Server.Features.Zones.Handlers.PlayCommandHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.PauseCommandHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.StopCommandHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.SetZoneVolumeCommandHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.VolumeUpCommandHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.VolumeDownCommandHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.SetZoneMuteCommandHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.ToggleZoneMuteCommandHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.SetTrackCommandHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.NextTrackCommandHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.PreviousTrackCommandHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.SetPlaylistCommandHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.NextPlaylistCommandHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.PreviousPlaylistCommandHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.SetTrackRepeatCommandHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.ToggleTrackRepeatCommandHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.SetPlaylistShuffleCommandHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.TogglePlaylistShuffleCommandHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.SetPlaylistRepeatCommandHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.TogglePlaylistRepeatCommandHandler>();

        // Zone query handlers
        services.AddScoped<Server.Features.Zones.Handlers.GetAllZonesQueryHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.GetZoneStateQueryHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.GetAllZoneStatesQueryHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.GetZonePlaybackStateQueryHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.GetZoneVolumeQueryHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.GetZoneTrackInfoQueryHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.GetZonePlaylistInfoQueryHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.GetAllPlaylistsQueryHandler>();
        services.AddScoped<Server.Features.Zones.Handlers.GetPlaylistTracksQueryHandler>();

        // Client command handlers
        services.AddScoped<Server.Features.Clients.Handlers.SetClientVolumeCommandHandler>();
        services.AddScoped<Server.Features.Clients.Handlers.SetClientMuteCommandHandler>();
        services.AddScoped<Server.Features.Clients.Handlers.ToggleClientMuteCommandHandler>();
        services.AddScoped<Server.Features.Clients.Handlers.SetClientLatencyCommandHandler>();
        services.AddScoped<Server.Features.Clients.Handlers.AssignClientToZoneCommandHandler>();

        // Client query handlers
        services.AddScoped<Server.Features.Clients.Handlers.GetAllClientsQueryHandler>();
        services.AddScoped<Server.Features.Clients.Handlers.GetClientQueryHandler>();
        services.AddScoped<Server.Features.Clients.Handlers.GetClientsByZoneQueryHandler>();

        // Notification handlers
        services.AddScoped<Server.Features.Shared.Handlers.ZoneStateNotificationHandler>();
        services.AddScoped<Server.Features.Shared.Handlers.ClientStateNotificationHandler>();

        // Automatically register all FluentValidation AbstractValidator<> implementations
        // found in the specified assembly. These are used by the ValidationBehavior.
        services.AddValidatorsFromAssembly(serverAssembly, ServiceLifetime.Transient);

        return services;
    }
}
