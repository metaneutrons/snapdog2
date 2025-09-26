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
namespace SnapDog2.Domain.Abstractions;

using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Events;
using SnapDog2.Shared.Models;

/// <summary>
/// Interface for persisting zone states across requests.
/// </summary>
public interface IZoneStateStore
{
    /// <summary>
    /// Event raised when zone state changes.
    /// </summary>
    [StatusId("ZONE_STATE_CHANGED")]
    event EventHandler<ZoneStateChangedEventArgs>? ZoneStateChanged;

    /// <summary>
    /// Event raised when zone playlist changes.
    /// </summary>
    [StatusId("PLAYLIST_STATUS")]
    event EventHandler<ZonePlaylistChangedEventArgs>? ZonePlaylistChanged;

    /// <summary>
    /// Event raised when zone position changes (debounced to 500ms).
    /// </summary>
    [StatusId("ZONE_POSITION_CHANGED")]
    event EventHandler<ZonePositionChangedEventArgs>? ZonePositionChanged;

    /// <summary>
    /// Event raised when zone volume changes.
    /// </summary>
    [StatusId("VOLUME_STATUS")]
    event EventHandler<ZoneVolumeChangedEventArgs>? ZoneVolumeChanged;

    /// <summary>
    /// Event raised when zone track changes.
    /// </summary>
    [StatusId("TRACK_STATUS")]
    event EventHandler<ZoneTrackChangedEventArgs>? ZoneTrackChanged;

    /// <summary>
    /// Event raised when zone playback state changes.
    /// </summary>
    [StatusId("PLAYBACK_STATE")]
    event EventHandler<ZonePlaybackStateChangedEventArgs>? ZonePlaybackStateChanged;

    /// <summary>
    /// Gets the current state for a zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Zone state or null if not found</returns>
    ZoneState? GetZoneState(int zoneIndex);

    /// <summary>
    /// Sets the current state for a zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="state">Zone state to store</param>
    void SetZoneState(int zoneIndex, ZoneState state);

    /// <summary>
    /// Surgical updates - only trigger specific change events
    /// </summary>
    void UpdateTrack(int zoneIndex, TrackInfo track);
    void UpdateVolume(int zoneIndex, int volume);
    void UpdatePlaybackState(int zoneIndex, bool state);
    void UpdatePlaylist(int zoneIndex, PlaylistInfo playlist);
    void UpdatePosition(int zoneIndex, int positionMs, double progress);
    void UpdateMute(int zoneIndex, bool mute);
    void UpdateRepeat(int zoneIndex, bool trackRepeat, bool playlistRepeat);
    void UpdateShuffle(int zoneIndex, bool shuffle);
    void UpdateClients(int zoneIndex, int[] clientIds);

    /// <summary>
    /// Publish current state by firing all relevant events (for state restoration)
    /// </summary>
    void PublishCurrentState(int zoneIndex);

    /// <summary>
    /// Gets all zone states.
    /// </summary>
    /// <returns>Dictionary of zone states by zone index</returns>
    Dictionary<int, ZoneState> GetAllZoneStates();

    /// <summary>
    /// Initializes default state for a zone if it doesn't exist.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="defaultState">Default state to use</param>
    void InitializeZoneState(int zoneIndex, ZoneState defaultState);
}
