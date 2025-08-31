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
namespace SnapDog2.Shared.Constants;

using SnapDog2.Server.Clients.Notifications;
using SnapDog2.Server.Global.Notifications;
using SnapDog2.Server.Zones.Notifications;
using SnapDog2.Shared.Attributes;

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

    // Client Status Notification IDs (Blueprint Compliance - Status Publishing)
    public static readonly string ClientVolumeStatusPublish =
        StatusIdAttribute.GetStatusId<ClientVolumeStatusNotification>();
    public static readonly string ClientMuteStatusPublish =
        StatusIdAttribute.GetStatusId<ClientMuteStatusNotification>();
    public static readonly string ClientLatencyStatusPublish =
        StatusIdAttribute.GetStatusId<ClientLatencyStatusNotification>();
    public static readonly string ClientZoneStatusPublish =
        StatusIdAttribute.GetStatusId<ClientZoneStatusNotification>();
    public static readonly string ClientConnectionStatusPublish =
        StatusIdAttribute.GetStatusId<ClientConnectionStatusNotification>();
    public static readonly string ClientStateStatusPublish = StatusIdAttribute.GetStatusId<ClientStateNotification>();

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

    // Track Metadata Status IDs (Static information about media files)
    public static readonly string TrackMetadata = StatusIdAttribute.GetStatusId<ZoneTrackMetadataChangedNotification>();
    public static readonly string TrackMetadataDuration =
        StatusIdAttribute.GetStatusId<ZoneTrackDurationChangedNotification>();
    public static readonly string TrackMetadataTitle =
        StatusIdAttribute.GetStatusId<ZoneTrackTitleChangedNotification>();
    public static readonly string TrackMetadataArtist =
        StatusIdAttribute.GetStatusId<ZoneTrackArtistChangedNotification>();
    public static readonly string TrackMetadataAlbum =
        StatusIdAttribute.GetStatusId<ZoneTrackAlbumChangedNotification>();
    public static readonly string TrackMetadataCover =
        StatusIdAttribute.GetStatusId<ZoneTrackCoverChangedNotification>();

    // Track Playback Status IDs (Dynamic playback state)
    public static readonly string TrackPositionStatus =
        StatusIdAttribute.GetStatusId<ZoneTrackPositionChangedNotification>();
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

    // Zone Status Notification IDs (Blueprint Compliance - Status Publishing)
    // Note: These obsolete status notifications have been removed as they don't match blueprint specification
    // The correct status IDs are defined above without the ZONE_ prefix
}
