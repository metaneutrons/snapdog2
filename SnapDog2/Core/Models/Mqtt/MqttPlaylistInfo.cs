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
/// Simplified playlist information for MQTT publishing.
/// </summary>
public record MqttPlaylistInfo
{
    /// <summary>
    /// Gets the current track index in the playlist (0-based).
    /// </summary>
    [JsonPropertyName("Index")]
    public required int Index { get; init; }

    /// <summary>
    /// Gets the playlist name.
    /// </summary>
    [JsonPropertyName("Name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the total number of tracks in the playlist.
    /// </summary>
    [JsonPropertyName("TrackCount")]
    public required int TrackCount { get; init; }

    /// <summary>
    /// Gets the total duration of the playlist in seconds.
    /// </summary>
    [JsonPropertyName("TotalDurationSec")]
    public required int TotalDurationSec { get; init; }

    /// <summary>
    /// Gets the playlist cover art URL (if available).
    /// </summary>
    [JsonPropertyName("CoverArtUrl")]
    public string? CoverArtUrl { get; init; }

    /// <summary>
    /// Gets the playlist source (e.g., "subsonic", "local").
    /// </summary>
    [JsonPropertyName("Source")]
    public required string Source { get; init; }
}
