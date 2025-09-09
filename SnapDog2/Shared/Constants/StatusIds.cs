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

/// <summary>
/// Strongly-typed constants for StatusId values.
/// These constants preserve the original StatusId values from deleted notification classes
/// for blueprint validation and API contract compatibility.
/// </summary>
public static class StatusIds
{
    // Client Status IDs
    public static readonly string ClientVolumeStatus = "CLIENT_VOLUME_CHANGED";
    public static readonly string ClientMuteStatus = "CLIENT_MUTE_CHANGED";
    public static readonly string ClientLatencyStatus = "CLIENT_LATENCY_CHANGED";
    public static readonly string ClientConnected = "CLIENT_CONNECTION_CHANGED";
    public static readonly string ClientZoneStatus = "CLIENT_ZONE_ASSIGNMENT_CHANGED";
    public static readonly string ClientState = "CLIENT_STATE_CHANGED";
    public static readonly string ClientName = "CLIENT_NAME_CHANGED";

    // Client Status Notification IDs (Blueprint Compliance - Status Publishing)
    public static readonly string ClientVolumeStatusPublish = "CLIENT_VOLUME_STATUS";
    public static readonly string ClientMuteStatusPublish = "CLIENT_MUTE_STATUS";
    public static readonly string ClientLatencyStatusPublish = "CLIENT_LATENCY_STATUS";
    public static readonly string ClientZoneStatusPublish = "CLIENT_ZONE_STATUS";
    public static readonly string ClientConnectionStatusPublish = "CLIENT_CONNECTION_STATUS";
    public static readonly string ClientStateStatusPublish = "CLIENT_STATE_STATUS";

    // Zone Status IDs
    public static readonly string PlaybackState = "ZONE_PLAYBACK_STATE_CHANGED";
    public static readonly string VolumeStatus = "ZONE_VOLUME_CHANGED";
    public static readonly string MuteStatus = "ZONE_MUTE_CHANGED";
    public static readonly string TrackIndex = "ZONE_TRACK_CHANGED";
    public static readonly string PlaylistIndex = "ZONE_PLAYLIST_CHANGED";
    public static readonly string TrackRepeatStatus = "ZONE_TRACK_REPEAT_CHANGED";
    public static readonly string PlaylistRepeatStatus = "ZONE_PLAYLIST_REPEAT_CHANGED";
    public static readonly string PlaylistShuffleStatus = "ZONE_SHUFFLE_MODE_CHANGED";

    // Track Metadata Status IDs (Static information about media files)
    public static readonly string TrackMetadata = "ZONE_TRACK_METADATA_CHANGED";
    public static readonly string TrackMetadataDuration = "ZONE_TRACK_DURATION_CHANGED";
    public static readonly string TrackMetadataTitle = "ZONE_TRACK_TITLE_CHANGED";
    public static readonly string TrackMetadataArtist = "ZONE_TRACK_ARTIST_CHANGED";
    public static readonly string TrackMetadataAlbum = "ZONE_TRACK_ALBUM_CHANGED";
    public static readonly string TrackMetadataCover = "ZONE_TRACK_COVER_CHANGED";

    // Track Playback Status IDs (Dynamic playback state)
    public static readonly string TrackPositionStatus = "ZONE_TRACK_POSITION_CHANGED";
    public static readonly string TrackPlayingStatus = "ZONE_TRACK_PLAYING_STATUS_CHANGED";
    public static readonly string TrackProgressStatus = "ZONE_TRACK_PROGRESS_CHANGED";

    // Playlist Information Status IDs
    public static readonly string PlaylistInfo = "ZONE_PLAYLIST_INFO_CHANGED";

    // Command Response Status IDs
    public static readonly string CommandStatus = "COMMAND_STATUS";
    public static readonly string CommandError = "COMMAND_ERROR";

    // Global Status IDs
    public static readonly string VersionInfo = "VERSION_INFO_CHANGED";
    public static readonly string SystemStatus = "SYSTEM_STATUS_CHANGED";
    public static readonly string ServerStats = "SERVER_STATS_CHANGED";
    public static readonly string SystemError = "SYSTEM_ERROR";
    public static readonly string ZonesInfo = "ZONES_INFO_CHANGED";
}
