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
    public static readonly string ClientName = StatusIdAttribute.GetStatusId<ClientNameChangedNotification>();

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

    // Track Information Status IDs
    public static readonly string TrackInfo = StatusIdAttribute.GetStatusId<ZoneTrackInfoChangedNotification>();
    public static readonly string TrackInfoLength = StatusIdAttribute.GetStatusId<ZoneTrackLengthChangedNotification>();
    public static readonly string TrackInfoPosition =
        StatusIdAttribute.GetStatusId<ZoneTrackPositionChangedNotification>();
    public static readonly string TrackInfoTitle = StatusIdAttribute.GetStatusId<ZoneTrackTitleChangedNotification>();
    public static readonly string TrackInfoArtist = StatusIdAttribute.GetStatusId<ZoneTrackArtistChangedNotification>();
    public static readonly string TrackInfoAlbum = StatusIdAttribute.GetStatusId<ZoneTrackAlbumChangedNotification>();
    public static readonly string TrackPlayingStatus =
        StatusIdAttribute.GetStatusId<ZoneTrackPlayingStatusChangedNotification>();
    public static readonly string TrackProgressStatus =
        StatusIdAttribute.GetStatusId<ZoneTrackProgressChangedNotification>();

    // Playlist Information Status IDs
    public static readonly string PlaylistInfo = StatusIdAttribute.GetStatusId<ZonePlaylistInfoChangedNotification>();

    // Command Response Status IDs
    public static readonly string CommandStatus = StatusIdAttribute.GetStatusId<CommandStatusNotification>();
    public static readonly string CommandError = StatusIdAttribute.GetStatusId<CommandErrorNotification>();

    // Global Status IDs
    public static readonly string VersionInfo = StatusIdAttribute.GetStatusId<VersionInfoChangedNotification>();
    public static readonly string SystemStatus = StatusIdAttribute.GetStatusId<SystemStatusChangedNotification>();
    public static readonly string ServerStats = StatusIdAttribute.GetStatusId<ServerStatsChangedNotification>();
    public static readonly string SystemError = StatusIdAttribute.GetStatusId<SystemErrorNotification>();
    public static readonly string ZonesInfo = StatusIdAttribute.GetStatusId<ZonesInfoChangedNotification>();
}
