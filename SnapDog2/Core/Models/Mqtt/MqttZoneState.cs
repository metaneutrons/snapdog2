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
using System.Text.Json.Serialization;

namespace SnapDog2.Core.Models.Mqtt;

/// <summary>
/// Simplified zone state for MQTT publishing - contains only user-facing information.
/// </summary>
public record MqttZoneState
{
    /// <summary>
    /// Gets the zone name.
    /// </summary>
    [JsonPropertyName("Name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets whether the zone is currently playing (true) or paused/stopped (false).
    /// </summary>
    [JsonPropertyName("PlaybackState")]
    public required bool PlaybackState { get; init; }

    /// <summary>
    /// Gets the zone volume (0-100).
    /// </summary>
    [JsonPropertyName("Volume")]
    public required int Volume { get; init; }

    /// <summary>
    /// Gets whether the zone is muted.
    /// </summary>
    [JsonPropertyName("Mute")]
    public required bool Mute { get; init; }

    /// <summary>
    /// Gets whether track repeat is enabled.
    /// </summary>
    [JsonPropertyName("RepeatTrack")]
    public required bool RepeatTrack { get; init; }

    /// <summary>
    /// Gets whether playlist repeat is enabled.
    /// </summary>
    [JsonPropertyName("RepeatPlaylist")]
    public required bool RepeatPlaylist { get; init; }

    /// <summary>
    /// Gets whether playlist shuffle is enabled.
    /// </summary>
    [JsonPropertyName("Shuffle")]
    public required bool Shuffle { get; init; }

    /// <summary>
    /// Gets the current playlist information (if any).
    /// </summary>
    [JsonPropertyName("Playlist")]
    public MqttPlaylistInfo? Playlist { get; init; }

    /// <summary>
    /// Gets the current track information (if any).
    /// </summary>
    [JsonPropertyName("Track")]
    public MqttTrackInfo? Track { get; init; }
}
