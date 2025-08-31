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

namespace SnapDog2.Shared.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Simplified track information for MQTT publishing.
/// </summary>
public record PublishableTrackInfo
{
    /// <summary>
    /// Gets the track index in the playlist (0-based).
    /// </summary>
    [JsonPropertyName("Index")]
    public required int Index { get; init; }

    /// <summary>
    /// Gets the track title.
    /// </summary>
    [JsonPropertyName("Title")]
    public required string Title { get; init; }

    /// <summary>
    /// Gets the track artist.
    /// </summary>
    [JsonPropertyName("Artist")]
    public required string Artist { get; init; }

    /// <summary>
    /// Gets the track album.
    /// </summary>
    [JsonPropertyName("Album")]
    public required string Album { get; init; }

    /// <summary>
    /// Gets the track cover art URL (if available).
    /// </summary>
    [JsonPropertyName("CoverArtUrl")]
    public string? CoverArtUrl { get; init; }

    /// <summary>
    /// Gets the track genre (if available).
    /// </summary>
    [JsonPropertyName("Genre")]
    public string? Genre { get; init; }

    /// <summary>
    /// Gets the track number (if available).
    /// </summary>
    [JsonPropertyName("TrackNumber")]
    public int? TrackNumber { get; init; }

    /// <summary>
    /// Gets the track year (if available).
    /// </summary>
    [JsonPropertyName("Year")]
    public int? Year { get; init; }

    /// <summary>
    /// Gets the track source (e.g., "subsonic", "local").
    /// </summary>
    [JsonPropertyName("Source")]
    public required string Source { get; init; }
}
