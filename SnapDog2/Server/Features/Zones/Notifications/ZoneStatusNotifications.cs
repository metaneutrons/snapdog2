namespace SnapDog2.Server.Features.Zones.Notifications;

using Cortex.Mediator.Notifications;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Notification published when a zone's playback state status is published.
/// </summary>
[StatusId("ZONE_PLAYBACK_STATE")]
public record ZonePlaybackStateStatusNotification(int ZoneIndex, PlaybackState PlaybackState) : INotification;

/// <summary>
/// Notification published when a zone's volume status is published.
/// </summary>
[StatusId("ZONE_VOLUME_STATUS")]
public record ZoneVolumeStatusNotification(int ZoneIndex, int Volume) : INotification;

/// <summary>
/// Notification published when a zone's mute status is published.
/// </summary>
[StatusId("ZONE_MUTE_STATUS")]
public record ZoneMuteStatusNotification(int ZoneIndex, bool IsMuted) : INotification;

/// <summary>
/// Notification published when a zone's track status is published.
/// </summary>
[StatusId("ZONE_TRACK_STATUS")]
public record ZoneTrackStatusNotification(int ZoneIndex, TrackInfo TrackInfo, int TrackIndex) : INotification;

/// <summary>
/// Notification published when a zone's playlist status is published.
/// </summary>
[StatusId("ZONE_PLAYLIST_STATUS")]
public record ZonePlaylistStatusNotification(int ZoneIndex, PlaylistInfo PlaylistInfo, int PlaylistIndex)
    : INotification;

/// <summary>
/// Notification published when a zone's complete state status is published.
/// </summary>
[StatusId("ZONE_STATE_STATUS")]
public record ZoneStateStatusNotification(int ZoneIndex, ZoneState ZoneState) : INotification;
