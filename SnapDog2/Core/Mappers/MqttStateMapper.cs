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
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Core.Models.Mqtt;

namespace SnapDog2.Core.Mappers;

/// <summary>
/// Maps internal state models to simplified MQTT-friendly formats.
/// </summary>
public static class MqttStateMapper
{
    /// <summary>
    /// Converts a ZoneState to a simplified MqttZoneState for publishing.
    /// </summary>
    /// <param name="zoneState">The full zone state.</param>
    /// <returns>Simplified MQTT zone state with only user-facing information.</returns>
    public static MqttZoneState ToMqttZoneState(ZoneState zoneState)
    {
        return new MqttZoneState
        {
            Name = zoneState.Name,
            PlaybackState = zoneState.PlaybackState == PlaybackState.Playing,
            Volume = zoneState.Volume,
            Mute = zoneState.Mute,
            RepeatTrack = zoneState.TrackRepeat,
            RepeatPlaylist = zoneState.PlaylistRepeat,
            Shuffle = zoneState.PlaylistShuffle,
            Playlist = zoneState.Playlist != null ? ToMqttPlaylistInfo(zoneState.Playlist) : null,
            Track = zoneState.Track != null ? ToMqttTrackInfo(zoneState.Track) : null
        };
    }

    /// <summary>
    /// Converts PlaylistInfo to simplified MqttPlaylistInfo.
    /// </summary>
    /// <param name="playlistInfo">The full playlist info.</param>
    /// <returns>Simplified MQTT playlist info.</returns>
    private static MqttPlaylistInfo ToMqttPlaylistInfo(PlaylistInfo playlistInfo)
    {
        return new MqttPlaylistInfo
        {
            Index = playlistInfo.Index ?? 0,
            Name = playlistInfo.Name,
            TrackCount = playlistInfo.TrackCount,
            TotalDurationSec = playlistInfo.TotalDurationSec ?? 0,
            CoverArtUrl = playlistInfo.CoverArtUrl,
            Source = playlistInfo.Source
        };
    }

    /// <summary>
    /// Converts TrackInfo to simplified MqttTrackInfo.
    /// </summary>
    /// <param name="trackInfo">The full track info.</param>
    /// <returns>Simplified MQTT track info.</returns>
    private static MqttTrackInfo ToMqttTrackInfo(TrackInfo trackInfo)
    {
        return new MqttTrackInfo
        {
            Index = trackInfo.Index ?? 0,
            Title = trackInfo.Title ?? string.Empty,
            Artist = trackInfo.Artist ?? string.Empty,
            Album = trackInfo.Album ?? string.Empty,
            CoverArtUrl = trackInfo.CoverArtUrl,
            Genre = trackInfo.Genre,
            TrackNumber = trackInfo.TrackNumber,
            Year = trackInfo.Year,
            Source = trackInfo.Source
        };
    }

    /// <summary>
    /// Compares two MqttZoneState objects to determine if they represent a meaningful change.
    /// </summary>
    /// <param name="previous">The previous state (can be null).</param>
    /// <param name="current">The current state.</param>
    /// <returns>True if the states are different enough to warrant publishing.</returns>
    public static bool HasMeaningfulChange(MqttZoneState? previous, MqttZoneState current)
    {
        if (previous == null)
            return true; // First time publishing

        // Check basic zone properties
        if (previous.Name != current.Name ||
            previous.PlaybackState != current.PlaybackState ||
            previous.Volume != current.Volume ||
            previous.Mute != current.Mute ||
            previous.RepeatTrack != current.RepeatTrack ||
            previous.RepeatPlaylist != current.RepeatPlaylist ||
            previous.Shuffle != current.Shuffle)
        {
            return true;
        }

        // Check playlist changes
        if (!PlaylistsEqual(previous.Playlist, current.Playlist))
        {
            return true;
        }

        // Check track changes (but ignore position/progress changes)
        if (!TracksEqual(previous.Track, current.Track))
        {
            return true;
        }

        return false; // No meaningful changes
    }

    private static bool PlaylistsEqual(MqttPlaylistInfo? a, MqttPlaylistInfo? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        return a.Index == b.Index &&
               a.Name == b.Name &&
               a.TrackCount == b.TrackCount &&
               a.TotalDurationSec == b.TotalDurationSec &&
               a.CoverArtUrl == b.CoverArtUrl &&
               a.Source == b.Source;
    }

    private static bool TracksEqual(MqttTrackInfo? a, MqttTrackInfo? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        return a.Index == b.Index &&
               a.Title == b.Title &&
               a.Artist == b.Artist &&
               a.Album == b.Album &&
               a.CoverArtUrl == b.CoverArtUrl &&
               a.Genre == b.Genre &&
               a.TrackNumber == b.TrackNumber &&
               a.Year == b.Year &&
               a.Source == b.Source;
    }
}
