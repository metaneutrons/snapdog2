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
namespace SnapDog2.Shared.Enums;

using System.ComponentModel;

/// <summary>
/// Enum representing all possible status event types in the system.
/// This provides compile-time safety and eliminates hardcoded strings.
/// </summary>
public enum StatusEventType
{
    // Client Status Events
    [Description("CLIENT_VOLUME_STATUS")]
    ClientVolumeStatus,

    [Description("CLIENT_MUTE_STATUS")]
    ClientMuteStatus,

    [Description("CLIENT_LATENCY_STATUS")]
    ClientLatencyStatus,

    [Description("CLIENT_CONNECTED")]
    ClientConnected,

    [Description("CLIENT_ZONE_STATUS")]
    ClientZoneStatus,

    [Description("CLIENT_NAME_STATUS")]
    ClientNameStatus,

    [Description("CLIENT_STATE")]
    ClientState,

    // Zone Status Events
    [Description("PLAYBACK_STATE")]
    PlaybackState,

    [Description("VOLUME_STATUS")]
    VolumeStatus,

    [Description("MUTE_STATUS")]
    MuteStatus,

    [Description("TRACK_STATUS")]
    TrackIndex,

    [Description("TRACK_METADATA")]
    TrackMetadata,

    [Description("TRACK_METADATA_TITLE")]
    TrackMetadataTitle,

    [Description("TRACK_METADATA_ARTIST")]
    TrackMetadataArtist,

    [Description("TRACK_METADATA_ALBUM")]
    TrackMetadataAlbum,

    [Description("TRACK_METADATA_COVER")]
    TrackMetadataCover,

    [Description("TRACK_METADATA_DURATION")]
    TrackMetadataDuration,

    [Description("TRACK_PLAYING_STATUS")]
    TrackPlayingStatus,

    [Description("TRACK_POSITION_STATUS")]
    TrackPositionStatus,

    [Description("TRACK_PROGRESS_STATUS")]
    TrackProgressStatus,

    [Description("PLAYLIST_STATUS")]
    PlaylistIndex,

    [Description("PLAYLIST_INFO")]
    PlaylistInfo,

    [Description("TRACK_REPEAT_STATUS")]
    TrackRepeatStatus,

    [Description("PLAYLIST_NAME_STATUS")]
    PlaylistNameStatus,

    [Description("PLAYLIST_COUNT_STATUS")]
    PlaylistCountStatus,

    [Description("ZONE_NAME_STATUS")]
    ZoneNameStatus,

    [Description("CONTROL_STATUS")]
    ControlStatus,

    [Description("PLAYLIST_REPEAT_STATUS")]
    PlaylistRepeatStatus,

    [Description("PLAYLIST_SHUFFLE_STATUS")]
    PlaylistShuffleStatus,

    [Description("ZONES_INFO")]
    ZonesInfo,

    [Description("CLIENTS_INFO")]
    ClientsInfo,

    // Global Status Events
    [Description("VERSION_INFO")]
    VersionInfo,

    [Description("SYSTEM_STATUS")]
    SystemStatus,

    [Description("SERVER_STATS")]
    ServerStats,

    [Description("SYSTEM_ERROR")]
    SystemError,

    [Description("COMMAND_ERROR")]
    CommandError,

    [Description("COMMAND_STATUS")]
    CommandStatus,

    [Description("ERROR_STATUS")]
    ErrorStatus,
}

/// <summary>
/// Extension methods for StatusEventType enum.
/// </summary>
public static class StatusEventTypeExtensions
{
    /// <summary>
    /// Gets the string value from the Description attribute.
    /// </summary>
    /// <param name="eventType">The status event type.</param>
    /// <returns>The string representation of the status event type.</returns>
    public static string ToStatusString(this StatusEventType eventType)
    {
        var field = eventType.GetType().GetField(eventType.ToString());
        var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field!, typeof(DescriptionAttribute));
        return attribute?.Description ?? eventType.ToString();
    }

    /// <summary>
    /// Parses a status string to StatusEventType enum.
    /// </summary>
    /// <param name="statusString">The status string to parse.</param>
    /// <returns>The corresponding StatusEventType, or null if not found.</returns>
    public static StatusEventType? FromStatusString(string statusString)
    {
        foreach (StatusEventType eventType in Enum.GetValues<StatusEventType>())
        {
            if (eventType.ToStatusString().Equals(statusString, StringComparison.OrdinalIgnoreCase))
            {
                return eventType;
            }
        }
        return null;
    }
}
