//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
namespace SnapDog2.Application.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Clients.Notifications;
using SnapDog2.Server.Clients.Queries;
using SnapDog2.Server.Global.Notifications;
using SnapDog2.Server.Global.Queries;
using SnapDog2.Server.Zones.Notifications;
using SnapDog2.Server.Zones.Queries;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Models;

/// <summary>
/// Service responsible for publishing initial state to all integrations at application startup.
/// According to the command framework blueprint (Section 15), all states with direction "publish"
/// should be published at startup to establish a defined state across all integrations.
/// From then on, only state changes are published.
///
/// This service uses the Cortex.Mediator pattern to publish notifications, which are then
/// handled by the existing notification handlers that publish to integration services (MQTT, KNX).
/// </summary>
public partial class StatePublishingService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<StatePublishingService> logger,
    SnapDogConfiguration configuration
) : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly ILogger<StatePublishingService> _logger = logger;
    private readonly SnapDogConfiguration _configuration = configuration;

    #region Logging

    [LoggerMessage(
        EventId = 7600,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "🚀 Starting initial state publishing for all integrations..."
    )]
    private partial void LogStatePublishingStarted();

    [LoggerMessage(
        EventId = 7601,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "📊 Publishing global system state..."
    )]
    private partial void LogPublishingGlobalState();

    [LoggerMessage(
        EventId = 7602,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "🎵 Publishing initial state for {ZoneCount} zones..."
    )]
    private partial void LogPublishingZoneStates(int zoneCount);

    [LoggerMessage(
        EventId = 7603,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "📱 Publishing initial state for {ClientCount} clients..."
    )]
    private partial void LogPublishingClientStates(int clientCount);

    [LoggerMessage(
        EventId = 7604,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "✅ Zone {ZoneIndex} initial state published successfully"
    )]
    private partial void LogZoneStatePublished(int zoneIndex);

    [LoggerMessage(
        EventId = 7605,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "✅ Client {ClientIndex} initial state published successfully"
    )]
    private partial void LogClientStatePublished(int clientIndex);

    [LoggerMessage(
        EventId = 7606,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "✅ Global system state published successfully"
    )]
    private partial void LogGlobalStatePublished();

    [LoggerMessage(
        EventId = 7607,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "⚠️ Failed to publish zone {ZoneIndex} state: {ErrorMessage}"
    )]
    private partial void LogZoneStatePublishFailed(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 7608,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "⚠️ Failed to publish client {ClientIndex} state: {ErrorMessage}"
    )]
    private partial void LogClientStatePublishFailed(int clientIndex, string errorMessage);

    [LoggerMessage(
        EventId = 7609,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "⚠️ Failed to publish global state: {ErrorMessage}"
    )]
    private partial void LogGlobalStatePublishFailed(string errorMessage);

    [LoggerMessage(
        EventId = 7610,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "✅ Initial state publishing completed successfully"
    )]
    private partial void LogStatePublishingCompleted();

    [LoggerMessage(
        EventId = 7611,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "⚠️ State publishing completed with some failures"
    )]
    private partial void LogStatePublishingCompletedWithFailures();

    [LoggerMessage(
        EventId = 7612,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "📴 No integration services available - skipping state publishing"
    )]
    private partial void LogNoIntegrationServices();

    [LoggerMessage(
        EventId = 7613,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "❌ Critical error during state publishing: {ErrorMessage}"
    )]
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
            using var tempScope = this._serviceScopeFactory.CreateScope();
            var mqttService = tempScope.ServiceProvider.GetService<IMqttService>();
            var knxService = tempScope.ServiceProvider.GetService<IKnxService>();

            if (mqttService == null && knxService == null)
            {
                this.LogNoIntegrationServices();
                return;
            }

            var publishingTasks = new List<Task>();
            var successCount = 0;
            var failureCount = 0;

            // Create scope to resolve scoped services like IMediator
            // NOTE: Using 'using' statement for automatic disposal. Services that implement
            // only IAsyncDisposable (like ZoneManager, MediaPlayerService) have been given
            // sync Dispose() wrappers as a workaround. See TODO comments in those services.
            using var scope = this._serviceScopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

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
                    var zoneIndex = i + 1; // 1-based indexing
                    publishingTasks.Add(
                        PublishZoneStateAsync(mediator, zoneIndex, stoppingToken)
                            .ContinueWith(
                                t =>
                                {
                                    if (t.IsCompletedSuccessfully && t.Result)
                                    {
                                        this.LogZoneStatePublished(zoneIndex);
                                        Interlocked.Increment(ref successCount);
                                    }
                                    else
                                    {
                                        var error = t.Exception?.GetBaseException()?.Message ?? "Unknown error";
                                        this.LogZoneStatePublishFailed(zoneIndex, error);
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
                    var clientIndex = i + 1; // 1-based indexing
                    publishingTasks.Add(
                        PublishClientStateAsync(mediator, clientIndex, stoppingToken)
                            .ContinueWith(
                                t =>
                                {
                                    if (t.IsCompletedSuccessfully && t.Result)
                                    {
                                        this.LogClientStatePublished(clientIndex);
                                        Interlocked.Increment(ref successCount);
                                    }
                                    else
                                    {
                                        var error = t.Exception?.GetBaseException()?.Message ?? "Unknown error";
                                        this.LogClientStatePublishFailed(clientIndex, error);
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
            this.LogStatePublishingCancelledDuringShutdown();
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
                new SystemStatusChangedNotification { Status = systemStatusResult.Value! },
                cancellationToken
            );

            await mediator.PublishAsync(
                new VersionInfoChangedNotification { VersionInfo = versionInfoResult.Value! },
                cancellationToken
            );

            await mediator.PublishAsync(
                new ServerStatsChangedNotification { Stats = serverStatsResult.Value! },
                cancellationToken
            );

            // Publish zones info for system discovery
            var zoneIndices =
                this._configuration.Zones?.Select((zone, index) => index + 1) // Zones are 1-based
                    .ToList() ?? new List<int>();
            await mediator.PublishAsync(new ZonesInfoChangedNotification(zoneIndices), cancellationToken);

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
    private static async Task<bool> PublishZoneStateAsync(
        IMediator mediator,
        int zoneIndex,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Get current zone state using Mediator query
            var zoneStateResult = await mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(
                new GetZoneStateQuery { ZoneIndex = zoneIndex },
                cancellationToken
            );

            if (!zoneStateResult.IsSuccess || zoneStateResult.Value == null)
            {
                return false;
            }

            var zoneState = zoneStateResult.Value;

            // PlaybackState is already an enum, no conversion needed
            var playbackStateEnum = zoneState.PlaybackState;

            // Also publish individual state notifications for granular updates
            await mediator.PublishAsync(
                new ZonePlaybackStateChangedNotification { ZoneIndex = zoneIndex, PlaybackState = playbackStateEnum },
                cancellationToken
            );

            await mediator.PublishAsync(
                new ZoneVolumeChangedNotification { ZoneIndex = zoneIndex, Volume = zoneState.Volume },
                cancellationToken
            );

            await mediator.PublishAsync(
                new ZoneMuteChangedNotification { ZoneIndex = zoneIndex, IsMuted = zoneState.Mute },
                cancellationToken
            );

            if (zoneState.Track != null)
            {
                await mediator.PublishAsync(
                    new ZoneTrackChangedNotification
                    {
                        ZoneIndex = zoneIndex,
                        TrackInfo = zoneState.Track,
                        TrackIndex = zoneState.Track.Index ?? 0,
                    },
                    cancellationToken
                );
            }

            if (zoneState.Playlist != null)
            {
                await mediator.PublishAsync(
                    new ZonePlaylistChangedNotification
                    {
                        ZoneIndex = zoneIndex,
                        PlaylistInfo = zoneState.Playlist,
                        PlaylistIndex = zoneState.Playlist.Index ?? 1,
                    },
                    cancellationToken
                );
            }

            await mediator.PublishAsync(
                new ZoneTrackRepeatChangedNotification { ZoneIndex = zoneIndex, Enabled = zoneState.TrackRepeat },
                cancellationToken
            );

            await mediator.PublishAsync(
                new ZonePlaylistRepeatChangedNotification { ZoneIndex = zoneIndex, Enabled = zoneState.PlaylistRepeat },
                cancellationToken
            );

            await mediator.PublishAsync(
                new ZoneShuffleModeChangedNotification
                {
                    ZoneIndex = zoneIndex,
                    ShuffleEnabled = zoneState.PlaylistShuffle,
                },
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
    private static async Task<bool> PublishClientStateAsync(
        IMediator mediator,
        int clientIndex,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Get current client state using Mediator query
            var clientStateResult = await mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(
                new GetClientQuery { ClientIndex = clientIndex },
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
                new ClientStateChangedNotification { ClientIndex = clientIndex, ClientState = clientState },
                cancellationToken
            );

            // Also publish individual state notifications for granular updates
            await mediator.PublishAsync(
                new ClientVolumeChangedNotification { ClientIndex = clientIndex, Volume = clientState.Volume },
                cancellationToken
            );

            await mediator.PublishAsync(
                new ClientMuteChangedNotification { ClientIndex = clientIndex, IsMuted = clientState.Mute },
                cancellationToken
            );

            await mediator.PublishAsync(
                new ClientLatencyChangedNotification { ClientIndex = clientIndex, LatencyMs = clientState.LatencyMs },
                cancellationToken
            );

            // Note: ClientZoneAssignmentChangedNotification is not published during initial state
            // publishing to avoid conflicts with ClientZoneInitializationService. Zone assignment
            // notifications should only be published when zones are actually changed by user actions.

            await mediator.PublishAsync(
                new ClientConnectionChangedNotification
                {
                    ClientIndex = clientIndex,
                    IsConnected = clientState.Connected,
                },
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
