namespace SnapDog2.Core.Constants;

using SnapDog2.Core.Attributes;
using SnapDog2.Server.Features.Clients.Notifications;
using SnapDog2.Server.Features.Global.Notifications;
using SnapDog2.Server.Features.Zones.Notifications;

/// <summary>
/// Strongly-typed constants for StatusId values.
/// These are derived from the StatusIdAttribute on notification classes,
/// ensuring compile-time safety and eliminating hardcoded strings.
/// </summary>
public static class StatusIds
{
    // Client Status IDs
    public static readonly string ClientVolumeStatus = StatusIdAttribute.GetStatusId<ClientVolumeChangedNotification>();
    public static readonly string ClientMuteStatus = StatusIdAttribute.GetStatusId<ClientMuteChangedNotification>();
    public static readonly string ClientLatencyStatus =
        StatusIdAttribute.GetStatusId<ClientLatencyChangedNotification>();
    public static readonly string ClientConnected =
        StatusIdAttribute.GetStatusId<ClientConnectionChangedNotification>();
    public static readonly string ClientZoneStatus =
        StatusIdAttribute.GetStatusId<ClientZoneAssignmentChangedNotification>();
    public static readonly string ClientState = StatusIdAttribute.GetStatusId<ClientStateChangedNotification>();

    // Zone Status IDs
    public static readonly string PlaybackState = StatusIdAttribute.GetStatusId<ZonePlaybackStateChangedNotification>();
    public static readonly string VolumeStatus = StatusIdAttribute.GetStatusId<ZoneVolumeChangedNotification>();
    public static readonly string MuteStatus = StatusIdAttribute.GetStatusId<ZoneMuteChangedNotification>();
    public static readonly string TrackIndex = StatusIdAttribute.GetStatusId<ZoneTrackChangedNotification>();
    public static readonly string PlaylistIndex = StatusIdAttribute.GetStatusId<ZonePlaylistChangedNotification>();
    public static readonly string TrackRepeatStatus =
        StatusIdAttribute.GetStatusId<ZoneTrackRepeatChangedNotification>();
    public static readonly string PlaylistRepeatStatus =
        StatusIdAttribute.GetStatusId<ZonePlaylistRepeatChangedNotification>();
    public static readonly string PlaylistShuffleStatus =
        StatusIdAttribute.GetStatusId<ZoneShuffleModeChangedNotification>();
    public static readonly string ZoneState = StatusIdAttribute.GetStatusId<ZoneStateChangedNotification>();

    // Global Status IDs
    public static readonly string VersionInfo = StatusIdAttribute.GetStatusId<VersionInfoChangedNotification>();
    public static readonly string SystemStatus = StatusIdAttribute.GetStatusId<SystemStatusChangedNotification>();
    public static readonly string ServerStats = StatusIdAttribute.GetStatusId<ServerStatsChangedNotification>();
    public static readonly string SystemError = StatusIdAttribute.GetStatusId<SystemErrorNotification>();
}
