namespace SnapDog2.Server.Features.Shared.Handlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Attributes;
using SnapDog2.Server.Features.Zones.Notifications;

/// <summary>
/// Handles zone state change notifications to log and process status updates.
/// </summary>
public partial class ZoneStateNotificationHandler
    : INotificationHandler<ZoneVolumeChangedNotification>,
        INotificationHandler<ZoneMuteChangedNotification>,
        INotificationHandler<ZonePlaybackStateChangedNotification>,
        INotificationHandler<ZoneTrackChangedNotification>,
        INotificationHandler<ZonePlaylistChangedNotification>,
        INotificationHandler<ZoneTrackRepeatChangedNotification>,
        INotificationHandler<ZonePlaylistRepeatChangedNotification>,
        INotificationHandler<ZoneShuffleModeChangedNotification>,
        INotificationHandler<ZoneStateChangedNotification>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ZoneStateNotificationHandler> _logger;

    public ZoneStateNotificationHandler(IServiceProvider serviceProvider, ILogger<ZoneStateNotificationHandler> logger)
    {
        this._serviceProvider = serviceProvider;
        this._logger = logger;
    }

    [LoggerMessage(6001, LogLevel.Information, "Zone {ZoneIndex} volume changed to {Volume}")]
    private partial void LogVolumeChange(int zoneIndex, int volume);

    [LoggerMessage(6002, LogLevel.Information, "Zone {ZoneIndex} mute changed to {IsMuted}")]
    private partial void LogMuteChange(int zoneIndex, bool isMuted);

    [LoggerMessage(6003, LogLevel.Information, "Zone {ZoneIndex} playback state changed to {PlaybackState}")]
    private partial void LogPlaybackStateChange(int zoneIndex, string playbackState);

    [LoggerMessage(6004, LogLevel.Information, "Zone {ZoneIndex} track changed to {TrackTitle} by {Artist}")]
    private partial void LogTrackChange(int zoneIndex, string trackTitle, string artist);

    [LoggerMessage(
        6005,
        LogLevel.Information,
        "Zone {ZoneIndex} playlist changed to {PlaylistName} (Index: {PlaylistIndex})"
    )]
    private partial void LogPlaylistChange(int zoneIndex, string playlistName, int playlistIndex);

    [LoggerMessage(6006, LogLevel.Information, "Zone {ZoneIndex} track repeat changed to {TrackRepeatEnabled}")]
    private partial void LogTrackRepeatChange(int zoneIndex, bool trackRepeatEnabled);

    [LoggerMessage(6007, LogLevel.Information, "Zone {ZoneIndex} playlist repeat changed to {PlaylistRepeatEnabled}")]
    private partial void LogPlaylistRepeatChange(int zoneIndex, bool playlistRepeatEnabled);

    [LoggerMessage(6008, LogLevel.Information, "Zone {ZoneIndex} shuffle mode changed to {ShuffleEnabled}")]
    private partial void LogShuffleModeChange(int zoneIndex, bool shuffleEnabled);

    [LoggerMessage(6008, LogLevel.Information, "Zone {ZoneIndex} complete state changed")]
    private partial void LogStateChange(int zoneIndex);

    public Task Handle(ZoneVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogVolumeChange(notification.ZoneIndex, notification.Volume);

        // Publish to external systems (MQTT, KNX) - fire and forget to prevent blocking
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await this.PublishToExternalSystemsAsync(
                        StatusIdAttribute.GetStatusId<ZoneVolumeChangedNotification>(),
                        notification.ZoneIndex,
                        notification.Volume,
                        CancellationToken.None
                    );
                }
                catch (Exception ex)
                {
                    // Log but don't throw - this is fire and forget
                    this.LogFailedToPublishZoneEventToExternalSystems(
                        ex,
                        StatusIdAttribute.GetStatusId<ZoneVolumeChangedNotification>(),
                        notification.ZoneIndex.ToString()
                    );
                }
            },
            CancellationToken.None
        );

        return Task.CompletedTask;
    }

    public Task Handle(ZoneMuteChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogMuteChange(notification.ZoneIndex, notification.IsMuted);

        // Publish to external systems (MQTT, KNX) - fire and forget to prevent blocking
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await this.PublishToExternalSystemsAsync(
                        StatusIdAttribute.GetStatusId<ZoneMuteChangedNotification>(),
                        notification.ZoneIndex,
                        notification.IsMuted,
                        CancellationToken.None
                    );
                }
                catch (Exception ex)
                {
                    // Log but don't throw - this is fire and forget
                    this.LogFailedToPublishZoneEventToExternalSystems(
                        ex,
                        StatusIdAttribute.GetStatusId<ZoneMuteChangedNotification>(),
                        notification.ZoneIndex.ToString()
                    );
                }
            },
            CancellationToken.None
        );

        return Task.CompletedTask;
    }

    public Task Handle(ZonePlaybackStateChangedNotification notification, CancellationToken cancellationToken)
    {
        var playbackStateString = notification.PlaybackState.ToString().ToLowerInvariant();
        this.LogPlaybackStateChange(notification.ZoneIndex, playbackStateString);

        // Publish to external systems (MQTT, KNX) - fire and forget to prevent blocking
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await this.PublishToExternalSystemsAsync(
                        StatusIdAttribute.GetStatusId<ZonePlaybackStateChangedNotification>(),
                        notification.ZoneIndex,
                        playbackStateString,
                        CancellationToken.None
                    );
                }
                catch (Exception ex)
                {
                    // Log but don't throw - this is fire and forget
                    this.LogFailedToPublishZoneEventToExternalSystems(
                        ex,
                        StatusIdAttribute.GetStatusId<ZonePlaybackStateChangedNotification>(),
                        notification.ZoneIndex.ToString()
                    );
                }
            },
            CancellationToken.None
        );

        return Task.CompletedTask;
    }

    public Task Handle(ZoneTrackChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogTrackChange(notification.ZoneIndex, notification.TrackInfo.Title, notification.TrackInfo.Artist);

        // Publish to external systems (MQTT, KNX) - fire and forget to prevent blocking
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await this.PublishToExternalSystemsAsync(
                        StatusIdAttribute.GetStatusId<ZoneTrackChangedNotification>(),
                        notification.ZoneIndex,
                        notification.TrackInfo,
                        CancellationToken.None
                    );
                }
                catch (Exception ex)
                {
                    // Log but don't throw - this is fire and forget
                    this.LogFailedToPublishZoneEventToExternalSystems(
                        ex,
                        StatusIdAttribute.GetStatusId<ZoneTrackChangedNotification>(),
                        notification.ZoneIndex.ToString()
                    );
                }
            },
            CancellationToken.None
        );

        return Task.CompletedTask;
    }

    public Task Handle(ZonePlaylistChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogPlaylistChange(notification.ZoneIndex, notification.PlaylistInfo.Name, notification.PlaylistIndex);

        // Publish to external systems (MQTT, KNX) - fire and forget to prevent blocking
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await this.PublishToExternalSystemsAsync(
                        StatusIdAttribute.GetStatusId<ZonePlaylistChangedNotification>(),
                        notification.ZoneIndex,
                        new { PlaylistInfo = notification.PlaylistInfo, PlaylistIndex = notification.PlaylistIndex },
                        CancellationToken.None
                    );
                }
                catch (Exception ex)
                {
                    // Log but don't throw - this is fire and forget
                    this.LogFailedToPublishZoneEventToExternalSystems(
                        ex,
                        StatusIdAttribute.GetStatusId<ZonePlaylistChangedNotification>(),
                        notification.ZoneIndex.ToString()
                    );
                }
            },
            CancellationToken.None
        );

        return Task.CompletedTask;
    }

    public Task Handle(ZoneTrackRepeatChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogTrackRepeatChange(notification.ZoneIndex, notification.Enabled);

        // Publish to external systems (MQTT, KNX) - fire and forget to prevent blocking
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await this.PublishToExternalSystemsAsync(
                        StatusIdAttribute.GetStatusId<ZoneTrackRepeatChangedNotification>(),
                        notification.ZoneIndex,
                        notification.Enabled,
                        CancellationToken.None
                    );
                }
                catch (Exception ex)
                {
                    // Log but don't throw - this is fire and forget
                    this.LogFailedToPublishZoneEventToExternalSystems(
                        ex,
                        StatusIdAttribute.GetStatusId<ZoneTrackRepeatChangedNotification>(),
                        notification.ZoneIndex.ToString()
                    );
                }
            },
            CancellationToken.None
        );

        return Task.CompletedTask;
    }

    public Task Handle(ZonePlaylistRepeatChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogPlaylistRepeatChange(notification.ZoneIndex, notification.Enabled);

        // Publish to external systems (MQTT, KNX) - fire and forget to prevent blocking
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await this.PublishToExternalSystemsAsync(
                        StatusIdAttribute.GetStatusId<ZonePlaylistRepeatChangedNotification>(),
                        notification.ZoneIndex,
                        notification.Enabled,
                        CancellationToken.None
                    );
                }
                catch (Exception ex)
                {
                    // Log but don't throw - this is fire and forget
                    this.LogFailedToPublishZoneEventToExternalSystems(
                        ex,
                        StatusIdAttribute.GetStatusId<ZonePlaylistRepeatChangedNotification>(),
                        notification.ZoneIndex.ToString()
                    );
                }
            },
            CancellationToken.None
        );

        return Task.CompletedTask;
    }

    public Task Handle(ZoneShuffleModeChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogShuffleModeChange(notification.ZoneIndex, notification.ShuffleEnabled);

        // Publish to external systems (MQTT, KNX) - fire and forget to prevent blocking
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await this.PublishToExternalSystemsAsync(
                        StatusIdAttribute.GetStatusId<ZoneShuffleModeChangedNotification>(),
                        notification.ZoneIndex,
                        notification.ShuffleEnabled,
                        CancellationToken.None
                    );
                }
                catch (Exception ex)
                {
                    // Log but don't throw - this is fire and forget
                    this.LogFailedToPublishZoneEventToExternalSystems(
                        ex,
                        StatusIdAttribute.GetStatusId<ZoneShuffleModeChangedNotification>(),
                        notification.ZoneIndex.ToString()
                    );
                }
            },
            CancellationToken.None
        );

        return Task.CompletedTask;
    }

    public Task Handle(ZoneStateChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogStateChange(notification.ZoneIndex);

        // Publish to external systems (MQTT, KNX) - fire and forget to prevent blocking
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await this.PublishToExternalSystemsAsync(
                        StatusIdAttribute.GetStatusId<ZoneStateChangedNotification>(),
                        notification.ZoneIndex,
                        notification,
                        CancellationToken.None
                    );
                }
                catch (Exception ex)
                {
                    // Log but don't throw - this is fire and forget
                    this.LogFailedToPublishZoneEventToExternalSystems(
                        ex,
                        StatusIdAttribute.GetStatusId<ZoneStateChangedNotification>(),
                        notification.ZoneIndex.ToString()
                    );
                }
            },
            CancellationToken.None
        );

        return Task.CompletedTask;
    }

    /// <summary>
    /// Publishes zone events to external systems (MQTT, KNX) if they are enabled.
    /// </summary>
    private async Task PublishToExternalSystemsAsync<T>(
        string eventType,
        int zoneIndex,
        T payload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Check if service provider is disposed before attempting to use it
            if (this._serviceProvider == null)
            {
                return;
            }

            // Publish to MQTT if enabled
            var mqttService = this._serviceProvider.GetService<IMqttService>();
            if (mqttService != null)
            {
                await mqttService.PublishZoneStatusAsync(zoneIndex, eventType, payload, cancellationToken);
            }

            // Publish to KNX if enabled
            var knxService = this._serviceProvider.GetService<IKnxService>();
            if (knxService != null)
            {
                await knxService.PublishZoneStatusAsync(zoneIndex, eventType, payload, cancellationToken);
            }
        }
        catch (ObjectDisposedException)
        {
            // Service provider is disposed, silently ignore
            return;
        }
        catch (Exception ex)
        {
            this.LogFailedToPublishZoneEventToExternalSystems(ex, eventType, zoneIndex.ToString());
        }
    }
}
