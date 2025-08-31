# 22. Zone Queries and Status Notifications

This document covers the Query implementations and Status Notifications for the Cortex.Mediator command framework.

## 22.1. Zone Queries

```csharp
// /Server/Features/Zones/Queries/ZoneQueries.cs
namespace SnapDog2.Server.Features.Zones.Queries;

using System.Collections.Generic;
using Cortex.Mediator;
using SnapDog2.Core.Models;

/// <summary>
/// Query to retrieve the state of all zones.
/// </summary>
public record GetAllZonesQuery : IQuery<Result<List<ZoneState>>>;

/// <summary>
/// Query to retrieve the state of a specific zone.
/// </summary>
public record GetZoneStateQuery : IQuery<Result<ZoneState>>
{
    /// <summary>
    /// Gets the ID of the zone to retrieve.
    /// </summary>
    public required int ZoneIndex { get; init; }
}

/// <summary>
/// Query to retrieve the current track information for a zone.
/// </summary>
public record GetZoneTrackInfoQuery : IQuery<Result<TrackInfo>>
{
    /// <summary>
    /// Gets the ID of the zone.
    /// </summary>
    public required int ZoneIndex { get; init; }
}

/// <summary>
/// Query to retrieve the current playlist information for a zone.
/// </summary>
public record GetZonePlaylistInfoQuery : IQuery<Result<PlaylistInfo>>
{
    /// <summary>
    /// Gets the ID of the zone.
    /// </summary>
    public required int ZoneIndex { get; init; }
}

/// <summary>
/// Query to retrieve all available playlists.
/// </summary>
public record GetAllPlaylistsQuery : IQuery<Result<List<PlaylistInfo>>>;

/// <summary>
/// Query to retrieve tracks for a specific playlist.
/// </summary>
public record GetPlaylistTracksQuery : IQuery<Result<List<TrackInfo>>>
{
    /// <summary>
    /// Gets the playlist ID or index (1-based).
    /// </summary>
    public string? PlaylistIndex { get; init; }

    /// <summary>
    /// Gets the playlist index (1-based, alternative to ID).
    /// </summary>
    public int? PlaylistIndex { get; init; }
}
```

## 22.2. Zone Query Handlers

```csharp
// /Server/Features/Zones/Handlers/GetAllZonesQueryHandler.cs
namespace SnapDog2.Server.Features.Zones.Handlers;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Queries;

/// <summary>
/// Handles the GetAllZonesQuery.
/// </summary>
public partial class GetAllZonesQueryHandler : IQueryHandler<GetAllZonesQuery, Result<List<ZoneState>>>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<GetAllZonesQueryHandler> _logger;

    [LoggerMessage(5001, LogLevel.Information, "Handling GetAllZonesQuery")]
    private partial void LogHandling();

    public GetAllZonesQueryHandler(
        IZoneManager zoneManager,
        ILogger<GetAllZonesQueryHandler> logger)
    {
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result<List<ZoneState>>> Handle(GetAllZonesQuery request, CancellationToken cancellationToken)
    {
        LogHandling();

        try
        {
            var zones = await _zoneManager.GetAllZoneStatesAsync().ConfigureAwait(false);
            return Result<List<ZoneState>>.Success(zones);
        }
        catch (Exception ex)
        {
            return Result<List<ZoneState>>.Failure(ex);
        }
    }
}

// /Server/Features/Zones/Handlers/GetZoneStateQueryHandler.cs
namespace SnapDog2.Server.Features.Zones.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Queries;

/// <summary>
/// Handles the GetZoneStateQuery.
/// </summary>
public partial class GetZoneStateQueryHandler : IQueryHandler<GetZoneStateQuery, Result<ZoneState>>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<GetZoneStateQueryHandler> _logger;

    [LoggerMessage(5101, LogLevel.Information, "Handling GetZoneStateQuery for Zone {ZoneIndex}")]
    private partial void LogHandling(int zoneIndex);

    public GetZoneStateQueryHandler(
        IZoneManager zoneManager,
        ILogger<GetZoneStateQueryHandler> logger)
    {
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result<ZoneState>> Handle(GetZoneStateQuery request, CancellationToken cancellationToken)
    {
        LogHandling(request.ZoneIndex);

        var result = await _zoneManager.GetZoneStateAsync(request.ZoneIndex).ConfigureAwait(false);
        return result;
    }
}

// /Server/Features/Zones/Handlers/GetAllPlaylistsQueryHandler.cs
namespace SnapDog2.Server.Features.Zones.Handlers;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Queries;

/// <summary>
/// Handles the GetAllPlaylistsQuery.
/// </summary>
public partial class GetAllPlaylistsQueryHandler : IQueryHandler<GetAllPlaylistsQuery, Result<List<PlaylistInfo>>>
{
    private readonly IPlaylistManager _playlistManager;
    private readonly ILogger<GetAllPlaylistsQueryHandler> _logger;

    [LoggerMessage(5201, LogLevel.Information, "Handling GetAllPlaylistsQuery")]
    private partial void LogHandling();

    public GetAllPlaylistsQueryHandler(
        IPlaylistManager playlistManager,
        ILogger<GetAllPlaylistsQueryHandler> logger)
    {
        _playlistManager = playlistManager;
        _logger = logger;
    }

    public async Task<Result<List<PlaylistInfo>>> Handle(GetAllPlaylistsQuery request, CancellationToken cancellationToken)
    {
        LogHandling();

        try
        {
            var playlists = await _playlistManager.GetAllPlaylistsAsync().ConfigureAwait(false);
            return Result<List<PlaylistInfo>>.Success(playlists);
        }
        catch (Exception ex)
        {
            return Result<List<PlaylistInfo>>.Failure(ex);
        }
    }
}
```

## 22.3. Status Notifications

### 22.3.1. Zone Status Notifications

```csharp
// /Server/Features/Shared/Notifications/ZoneNotifications.cs
namespace SnapDog2.Server.Features.Shared.Notifications;

using System;
using Cortex.Mediator;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Notification published when a zone's playback state changes.
/// </summary>
public record ZonePlaybackStateChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone ID.
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the new playback state.
    /// </summary>
    public required PlaybackStatus PlaybackState { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the state changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a zone's volume changes.
/// </summary>
public record ZoneVolumeChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone ID.
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the new volume level (0-100).
    /// </summary>
    public required int Volume { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the volume changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a zone's mute state changes.
/// </summary>
public record ZoneMuteChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone ID.
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets whether the zone is muted.
    /// </summary>
    public required bool IsMuted { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the mute state changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a zone's current track changes.
/// </summary>
public record ZoneTrackChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone ID.
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the new track information.
    /// </summary>
    public required TrackInfo TrackInfo { get; init; }

    /// <summary>
    /// Gets the new track index (1-based).
    /// </summary>
    public required int TrackIndex { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the track changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a zone's current playlist changes.
/// </summary>
public record ZonePlaylistChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone ID.
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the new playlist information.
    /// </summary>
    public required PlaylistInfo PlaylistInfo { get; init; }

    /// <summary>
    /// Gets the new playlist index (1-based).
    /// </summary>
    public required int PlaylistIndex { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the playlist changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a zone's repeat mode changes.
/// </summary>
public record ZoneRepeatModeChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone ID.
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets whether track repeat is enabled.
    /// </summary>
    public required bool TrackRepeatEnabled { get; init; }

    /// <summary>
    /// Gets whether playlist repeat is enabled.
    /// </summary>
    public required bool PlaylistRepeatEnabled { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the repeat mode changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a zone's shuffle mode changes.
/// </summary>
public record ZoneShuffleModeChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone ID.
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets whether shuffle is enabled.
    /// </summary>
    public required bool ShuffleEnabled { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the shuffle mode changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a zone's complete state changes.
/// </summary>
public record ZoneStateChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone ID.
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the complete zone state.
    /// </summary>
    public required ZoneState ZoneState { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the state changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
```

### 22.3.2. Client Status Notifications

```csharp
// /Server/Features/Shared/Notifications/ClientNotifications.cs
namespace SnapDog2.Server.Features.Shared.Notifications;

using System;
using Cortex.Mediator;
using SnapDog2.Core.Models;

/// <summary>
/// Notification published when a client's volume changes.
/// </summary>
public record ClientVolumeChangedNotification : INotification
{
    /// <summary>
    /// Gets the client Index.
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets the new volume level (0-100).
    /// </summary>
    public required int Volume { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the volume changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a client's mute state changes.
/// </summary>
public record ClientMuteChangedNotification : INotification
{
    /// <summary>
    /// Gets the client Index.
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets whether the client is muted.
    /// </summary>
    public required bool IsMuted { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the mute state changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a client's latency changes.
/// </summary>
public record ClientLatencyChangedNotification : INotification
{
    /// <summary>
    /// Gets the client Index.
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets the new latency in milliseconds.
    /// </summary>
    public required int LatencyMs { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the latency changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a client is assigned to a different zone.
/// </summary>
public record ClientZoneAssignmentChangedNotification : INotification
{
    /// <summary>
    /// Gets the client Index.
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets the new zone ID (null if unassigned).
    /// </summary>
    public int? ZoneIndex { get; init; }

    /// <summary>
    /// Gets the previous zone ID (null if was unassigned).
    /// </summary>
    public int? PreviousZoneIndex { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the assignment changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a client's connection status changes.
/// </summary>
public record ClientConnectionChangedNotification : INotification
{
    /// <summary>
    /// Gets the client Index.
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets whether the client is connected.
    /// </summary>
    public required bool IsConnected { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the connection status changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a client's complete state changes.
/// </summary>
public record ClientStateChangedNotification : INotification
{
    /// <summary>
    /// Gets the client Index.
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets the complete client state.
    /// </summary>
    public required ClientState ClientState { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the state changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
```

## 22.4. Generic Status Changed Notification

```csharp
// /Server/Features/Shared/Notifications/StatusChangedNotification.cs
namespace SnapDog2.Server.Features.Shared.Notifications;

using System;
using Cortex.Mediator;

/// <summary>
/// Generic notification published when any tracked status changes within the system.
/// This is used by infrastructure adapters (MQTT, KNX) for protocol-agnostic status updates.
/// </summary>
public record StatusChangedNotification : INotification
{
    /// <summary>
    /// Gets the type of status that changed (matches Command Framework Status IDs).
    /// </summary>
    public required string StatusType { get; init; }

    /// <summary>
    /// Gets the identifier for the entity whose status changed.
    /// </summary>
    public required string TargetId { get; init; }

    /// <summary>
    /// Gets the new value of the status.
    /// </summary>
    public required object Value { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the notification was created.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
```

## 22.5. Notification Handlers Example

```csharp
// /Server/Features/Shared/Handlers/ZoneNotificationHandlers.cs
namespace SnapDog2.Server.Features.Shared.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using SnapDog2.Server.Features.Shared.Notifications;

/// <summary>
/// Handles zone state change notifications to publish generic status updates.
/// </summary>
public partial class ZoneStateNotificationHandler :
    INotificationHandler<ZoneVolumeChangedNotification>,
    INotificationHandler<ZoneMuteChangedNotification>,
    INotificationHandler<ZonePlaybackStateChangedNotification>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ZoneStateNotificationHandler> _logger;

    [LoggerMessage(6001, LogLevel.Debug, "Publishing generic status for Zone {ZoneIndex} volume change to {Volume}")]
    private partial void LogVolumeChange(int zoneIndex, int volume);

    [LoggerMessage(6002, LogLevel.Debug, "Publishing generic status for Zone {ZoneIndex} mute change to {IsMuted}")]
    private partial void LogMuteChange(int zoneIndex, bool isMuted);

    [LoggerMessage(6003, LogLevel.Debug, "Publishing generic status for Zone {ZoneIndex} playback state change to {PlaybackState}")]
    private partial void LogPlaybackStateChange(int zoneIndex, string playbackState);

    public ZoneStateNotificationHandler(
        IMediator mediator,
        ILogger<ZoneStateNotificationHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(ZoneVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        LogVolumeChange(notification.ZoneIndex, notification.Volume);

        var statusNotification = new StatusChangedNotification
        {
            StatusType = "VOLUME_STATUS",
            TargetId = $"zone_{notification.ZoneIndex}",
            Value = notification.Volume
        };

        await _mediator.Publish(statusNotification, cancellationToken).ConfigureAwait(false);
    }

    public async Task Handle(ZoneMuteChangedNotification notification, CancellationToken cancellationToken)
    {
        LogMuteChange(notification.ZoneIndex, notification.IsMuted);

        var statusNotification = new StatusChangedNotification
        {
            StatusType = "MUTE_STATUS",
            TargetId = $"zone_{notification.ZoneIndex}",
            Value = notification.IsMuted
        };

        await _mediator.Publish(statusNotification, cancellationToken).ConfigureAwait(false);
    }

    public async Task Handle(ZonePlaybackStateChangedNotification notification, CancellationToken cancellationToken)
    {
        var playbackStateString = notification.PlaybackState.ToString().ToLowerInvariant();
        LogPlaybackStateChange(notification.ZoneIndex, playbackStateString);

        var statusNotification = new StatusChangedNotification
        {
            StatusType = "PLAYBACK_STATE",
            TargetId = $"zone_{notification.ZoneIndex}",
            Value = playbackStateString
        };

        await _mediator.Publish(statusNotification, cancellationToken).ConfigureAwait(false);
    }
}
```

---

**Next**: Integration with Infrastructure Adapters (6e) and Testing Strategies (6f).
