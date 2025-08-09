namespace SnapDog2.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Queries;
using SnapDog2.Server.Features.Global.Queries;
using SnapDog2.Server.Features.Shared.Notifications;
using SnapDog2.Server.Features.Zones.Queries;

/// <summary>
/// Service responsible for publishing initial state to all integrations at application startup.
/// According to the command framework blueprint (Section 15), all states with direction "publish"
/// should be published at startup to establish a defined state across all integrations.
/// From then on, only state changes are published.
///
/// This service uses the Cortex.Mediator pattern to publish notifications, which are then
/// handled by the existing notification handlers that publish to integration services (MQTT, KNX).
/// </summary>
public partial class StatePublishingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StatePublishingService> _logger;
    private readonly SnapDogConfiguration _configuration;

    public StatePublishingService(
        IServiceProvider serviceProvider,
        ILogger<StatePublishingService> logger,
        SnapDogConfiguration configuration
    )
    {
        this._serviceProvider = serviceProvider;
        this._logger = logger;
        this._configuration = configuration;
    }

    #region Logging

    [LoggerMessage(8001, LogLevel.Information, "🚀 Starting initial state publishing for all integrations...")]
    private partial void LogStatePublishingStarted();

    [LoggerMessage(8002, LogLevel.Information, "📊 Publishing global system state...")]
    private partial void LogPublishingGlobalState();

    [LoggerMessage(8003, LogLevel.Information, "🎵 Publishing initial state for {ZoneCount} zones...")]
    private partial void LogPublishingZoneStates(int zoneCount);

    [LoggerMessage(8004, LogLevel.Information, "📱 Publishing initial state for {ClientCount} clients...")]
    private partial void LogPublishingClientStates(int clientCount);

    [LoggerMessage(8005, LogLevel.Information, "✅ Zone {ZoneId} initial state published successfully")]
    private partial void LogZoneStatePublished(int zoneId);

    [LoggerMessage(8006, LogLevel.Information, "✅ Client {ClientId} initial state published successfully")]
    private partial void LogClientStatePublished(int clientId);

    [LoggerMessage(8007, LogLevel.Information, "✅ Global system state published successfully")]
    private partial void LogGlobalStatePublished();

    [LoggerMessage(8008, LogLevel.Warning, "⚠️  Failed to publish zone {ZoneId} state: {ErrorMessage}")]
    private partial void LogZoneStatePublishFailed(int zoneId, string errorMessage);

    [LoggerMessage(8009, LogLevel.Warning, "⚠️  Failed to publish client {ClientId} state: {ErrorMessage}")]
    private partial void LogClientStatePublishFailed(int clientId, string errorMessage);

    [LoggerMessage(8010, LogLevel.Warning, "⚠️  Failed to publish global state: {ErrorMessage}")]
    private partial void LogGlobalStatePublishFailed(string errorMessage);

    [LoggerMessage(8011, LogLevel.Information, "✅ Initial state publishing completed successfully")]
    private partial void LogStatePublishingCompleted();

    [LoggerMessage(8012, LogLevel.Warning, "⚠️  State publishing completed with some failures")]
    private partial void LogStatePublishingCompletedWithFailures();

    [LoggerMessage(8013, LogLevel.Information, "📴 No integration services available - skipping state publishing")]
    private partial void LogNoIntegrationServices();

    [LoggerMessage(8014, LogLevel.Error, "❌ Critical error during state publishing: {ErrorMessage}")]
    private partial void LogCriticalError(string errorMessage, Exception ex);

    #endregion

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Wait a short time to ensure all integration services are initialized
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

            this.LogStatePublishingStarted();

            // Check if any integration services are available
            var mqttService = this._serviceProvider.GetService<IMqttService>();
            var knxService = this._serviceProvider.GetService<IKnxService>();

            if (mqttService == null && knxService == null)
            {
                this.LogNoIntegrationServices();
                return;
            }

            var publishingTasks = new List<Task>();
            var successCount = 0;
            var failureCount = 0;

            // Get mediator for publishing notifications
            var mediator = this._serviceProvider.GetRequiredService<IMediator>();

            // 1. Publish global system state (SYSTEM_STATUS, VERSION_INFO, SERVER_STATS)
            this.LogPublishingGlobalState();
            publishingTasks.Add(
                this.PublishGlobalStateAsync(mediator, stoppingToken)
                    .ContinueWith(
                        t =>
                        {
                            if (t.IsCompletedSuccessfully && t.Result)
                            {
                                this.LogGlobalStatePublished();
                                Interlocked.Increment(ref successCount);
                            }
                            else
                            {
                                var error = t.Exception?.GetBaseException()?.Message ?? "Unknown error";
                                this.LogGlobalStatePublishFailed(error);
                                Interlocked.Increment(ref failureCount);
                            }
                        },
                        stoppingToken
                    )
            );

            // 2. Publish zone states for all configured zones
            if (this._configuration.Zones?.Any() == true)
            {
                this.LogPublishingZoneStates(this._configuration.Zones.Count);

                for (var i = 0; i < this._configuration.Zones.Count; i++)
                {
                    var zoneId = i + 1; // 1-based indexing as per blueprint
                    publishingTasks.Add(
                        this.PublishZoneStateAsync(mediator, zoneId, stoppingToken)
                            .ContinueWith(
                                t =>
                                {
                                    if (t.IsCompletedSuccessfully && t.Result)
                                    {
                                        this.LogZoneStatePublished(zoneId);
                                        Interlocked.Increment(ref successCount);
                                    }
                                    else
                                    {
                                        var error = t.Exception?.GetBaseException()?.Message ?? "Unknown error";
                                        this.LogZoneStatePublishFailed(zoneId, error);
                                        Interlocked.Increment(ref failureCount);
                                    }
                                },
                                stoppingToken
                            )
                    );
                }
            }

            // 3. Publish client states for all configured clients
            if (this._configuration.Clients?.Any() == true)
            {
                this.LogPublishingClientStates(this._configuration.Clients.Count);

                for (var i = 0; i < this._configuration.Clients.Count; i++)
                {
                    var clientId = i + 1; // 1-based indexing as per blueprint
                    publishingTasks.Add(
                        this.PublishClientStateAsync(mediator, clientId, stoppingToken)
                            .ContinueWith(
                                t =>
                                {
                                    if (t.IsCompletedSuccessfully && t.Result)
                                    {
                                        this.LogClientStatePublished(clientId);
                                        Interlocked.Increment(ref successCount);
                                    }
                                    else
                                    {
                                        var error = t.Exception?.GetBaseException()?.Message ?? "Unknown error";
                                        this.LogClientStatePublishFailed(clientId, error);
                                        Interlocked.Increment(ref failureCount);
                                    }
                                },
                                stoppingToken
                            )
                    );
                }
            }

            // Wait for all publishing tasks to complete
            await Task.WhenAll(publishingTasks);

            // Log completion status
            if (failureCount == 0)
            {
                this.LogStatePublishingCompleted();
            }
            else
            {
                this.LogStatePublishingCompletedWithFailures();
            }
        }
        catch (OperationCanceledException)
        {
            this._logger.LogInformation("State publishing cancelled during shutdown");
        }
        catch (Exception ex)
        {
            this.LogCriticalError(ex.Message, ex);
        }

        // This service runs once at startup and then exits
    }

    /// <summary>
    /// Publishes global system state including SYSTEM_STATUS, VERSION_INFO, and SERVER_STATS.
    /// These are all "publish" direction states according to the blueprint.
    /// Uses Mediator to publish notifications that are handled by existing notification handlers.
    /// </summary>
    private async Task<bool> PublishGlobalStateAsync(IMediator mediator, CancellationToken cancellationToken)
    {
        try
        {
            // Get current global state using Mediator queries
            var systemStatusResult = await mediator.SendQueryAsync<GetSystemStatusQuery, Result<SystemStatus>>(
                new GetSystemStatusQuery(),
                cancellationToken
            );
            var versionInfoResult = await mediator.SendQueryAsync<GetVersionInfoQuery, Result<VersionDetails>>(
                new GetVersionInfoQuery(),
                cancellationToken
            );
            var serverStatsResult = await mediator.SendQueryAsync<GetServerStatsQuery, Result<ServerStats>>(
                new GetServerStatsQuery(),
                cancellationToken
            );

            if (!systemStatusResult.IsSuccess || !versionInfoResult.IsSuccess || !serverStatsResult.IsSuccess)
            {
                return false;
            }

            // Publish notifications through Mediator - existing handlers will publish to integrations
            await mediator.PublishAsync(
                new SystemStatusChangedNotification { Status = systemStatusResult.Value },
                cancellationToken
            );

            await mediator.PublishAsync(
                new VersionInfoChangedNotification { VersionInfo = versionInfoResult.Value },
                cancellationToken
            );

            await mediator.PublishAsync(
                new ServerStatsChangedNotification { Stats = serverStatsResult.Value },
                cancellationToken
            );

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Publishes complete zone state including all "publish" direction states.
    /// Uses Mediator to publish notifications that are handled by existing notification handlers.
    /// </summary>
    private async Task<bool> PublishZoneStateAsync(IMediator mediator, int zoneId, CancellationToken cancellationToken)
    {
        try
        {
            // Get current zone state using Mediator query
            var zoneStateResult = await mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(
                new GetZoneStateQuery { ZoneId = zoneId },
                cancellationToken
            );

            if (!zoneStateResult.IsSuccess || zoneStateResult.Value == null)
            {
                return false;
            }

            var zoneState = zoneStateResult.Value;

            // Publish comprehensive zone state notification
            // The existing ZoneStateNotificationHandler will handle publishing to integrations
            await mediator.PublishAsync(
                new ZoneStateChangedNotification { ZoneId = zoneId, ZoneState = zoneState },
                cancellationToken
            );

            // Convert string playback state to enum
            var playbackStateEnum = zoneState.PlaybackState.ToLowerInvariant() switch
            {
                "play" or "playing" => SnapDog2.Core.Enums.PlaybackState.Playing,
                "pause" or "paused" => SnapDog2.Core.Enums.PlaybackState.Paused,
                "stop" or "stopped" => SnapDog2.Core.Enums.PlaybackState.Stopped,
                _ => SnapDog2.Core.Enums.PlaybackState.Stopped,
            };

            // Also publish individual state notifications for granular updates
            await mediator.PublishAsync(
                new ZonePlaybackStateChangedNotification { ZoneId = zoneId, PlaybackState = playbackStateEnum },
                cancellationToken
            );

            await mediator.PublishAsync(
                new ZoneVolumeChangedNotification { ZoneId = zoneId, Volume = zoneState.Volume },
                cancellationToken
            );

            await mediator.PublishAsync(
                new ZoneMuteChangedNotification { ZoneId = zoneId, IsMuted = zoneState.Mute },
                cancellationToken
            );

            if (zoneState.Track != null)
            {
                await mediator.PublishAsync(
                    new ZoneTrackChangedNotification
                    {
                        ZoneId = zoneId,
                        TrackInfo = zoneState.Track,
                        TrackIndex = zoneState.Track.Index,
                    },
                    cancellationToken
                );
            }

            if (zoneState.Playlist != null)
            {
                await mediator.PublishAsync(
                    new ZonePlaylistChangedNotification
                    {
                        ZoneId = zoneId,
                        PlaylistInfo = zoneState.Playlist,
                        PlaylistIndex = zoneState.Playlist.Index ?? 1,
                    },
                    cancellationToken
                );
            }

            await mediator.PublishAsync(
                new ZoneRepeatModeChangedNotification
                {
                    ZoneId = zoneId,
                    TrackRepeatEnabled = zoneState.TrackRepeat,
                    PlaylistRepeatEnabled = zoneState.PlaylistRepeat,
                },
                cancellationToken
            );

            await mediator.PublishAsync(
                new ZoneShuffleModeChangedNotification { ZoneId = zoneId, ShuffleEnabled = zoneState.PlaylistShuffle },
                cancellationToken
            );

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Publishes complete client state including all "publish" direction states.
    /// Uses Mediator to publish notifications that are handled by existing notification handlers.
    /// </summary>
    private async Task<bool> PublishClientStateAsync(
        IMediator mediator,
        int clientId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Get current client state using Mediator query
            var clientStateResult = await mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(
                new GetClientQuery { ClientId = clientId },
                cancellationToken
            );

            if (!clientStateResult.IsSuccess || clientStateResult.Value == null)
            {
                return false;
            }

            var clientState = clientStateResult.Value;

            // Publish comprehensive client state notification
            // The existing ClientStateNotificationHandler will handle publishing to integrations
            await mediator.PublishAsync(
                new ClientStateChangedNotification { ClientId = clientId, ClientState = clientState },
                cancellationToken
            );

            // Also publish individual state notifications for granular updates
            await mediator.PublishAsync(
                new ClientVolumeChangedNotification { ClientId = clientId, Volume = clientState.Volume },
                cancellationToken
            );

            await mediator.PublishAsync(
                new ClientMuteChangedNotification { ClientId = clientId, IsMuted = clientState.Mute },
                cancellationToken
            );

            await mediator.PublishAsync(
                new ClientLatencyChangedNotification { ClientId = clientId, LatencyMs = clientState.LatencyMs },
                cancellationToken
            );

            await mediator.PublishAsync(
                new ClientZoneAssignmentChangedNotification
                {
                    ClientId = clientId,
                    ZoneId = clientState.ZoneId,
                    PreviousZoneId = null, // No previous zone for initial state
                },
                cancellationToken
            );

            await mediator.PublishAsync(
                new ClientConnectionChangedNotification { ClientId = clientId, IsConnected = clientState.Connected },
                cancellationToken
            );

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
