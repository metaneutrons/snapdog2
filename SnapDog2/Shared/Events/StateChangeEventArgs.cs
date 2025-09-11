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
namespace SnapDog2.Shared.Events;

using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// Base event args for zone state changes.
/// </summary>
public record ZoneStateChangedEventArgs(
    int ZoneIndex,
    ZoneState? OldState,
    ZoneState NewState,
    DateTime Timestamp = default
)
{
    public DateTime Timestamp { get; } = Timestamp == default ? DateTime.UtcNow : Timestamp;
}

/// <summary>
/// Event args for zone playlist changes.
/// </summary>
[StatusId("PLAYLIST_STATUS")]
public record ZonePlaylistChangedEventArgs(
    int ZoneIndex,
    PlaylistInfo? OldPlaylist,
    PlaylistInfo? NewPlaylist,
    DateTime Timestamp = default
) : ZoneStateChangedEventArgs(ZoneIndex, null, null!, Timestamp);

/// <summary>
/// Event args for zone volume changes.
/// </summary>
[StatusId("VOLUME_STATUS")]
public record ZoneVolumeChangedEventArgs(
    int ZoneIndex,
    int OldVolume,
    int NewVolume,
    DateTime Timestamp = default
) : ZoneStateChangedEventArgs(ZoneIndex, null, null!, Timestamp);

/// <summary>
/// Event args for zone track changes.
/// </summary>
[StatusId("TRACK_STATUS")]
public record ZoneTrackChangedEventArgs(
    int ZoneIndex,
    TrackInfo? OldTrack,
    TrackInfo? NewTrack,
    DateTime Timestamp = default
) : ZoneStateChangedEventArgs(ZoneIndex, null, null!, Timestamp);

/// <summary>
/// Event args for zone playback state changes.
/// </summary>
[StatusId("PLAYBACK_STATE")]
public record ZonePlaybackStateChangedEventArgs(
    int ZoneIndex,
    PlaybackState OldPlaybackState,
    PlaybackState NewPlaybackState,
    DateTime Timestamp = default
) : ZoneStateChangedEventArgs(ZoneIndex, null, null!, Timestamp);

/// <summary>
/// Base event args for client state changes.
/// </summary>
public record ClientStateChangedEventArgs(
    int ClientIndex,
    ClientState? OldState,
    ClientState NewState,
    DateTime Timestamp = default
)
{
    public DateTime Timestamp { get; } = Timestamp == default ? DateTime.UtcNow : Timestamp;
}

/// <summary>
/// Event args for client volume changes.
/// </summary>
[StatusId("CLIENT_VOLUME_STATUS")]
public record ClientVolumeChangedEventArgs(
    int ClientIndex,
    int OldVolume,
    int NewVolume,
    DateTime Timestamp = default
) : ClientStateChangedEventArgs(ClientIndex, null, null!, Timestamp);

/// <summary>
/// Event args for client connection changes.
/// </summary>
[StatusId("CLIENT_CONNECTED")]
public record ClientConnectionChangedEventArgs(
    int ClientIndex,
    bool OldConnected,
    bool NewConnected,
    DateTime Timestamp = default
) : ClientStateChangedEventArgs(ClientIndex, null, null!, Timestamp);

/// <summary>
/// Event args for client name changes.
/// </summary>
[StatusId("CLIENT_NAME_STATUS")]
public record ClientNameChangedEventArgs(
    int ClientIndex,
    string OldName,
    string NewName,
    DateTime Timestamp = default
) : ClientStateChangedEventArgs(ClientIndex, null, null!, Timestamp);

/// <summary>
/// Event args for client latency changes.
/// </summary>
[StatusId("CLIENT_LATENCY_STATUS")]
public record ClientLatencyChangedEventArgs(
    int ClientIndex,
    int OldLatencyMs,
    int NewLatencyMs,
    DateTime Timestamp = default
) : ClientStateChangedEventArgs(ClientIndex, null, null!, Timestamp);
