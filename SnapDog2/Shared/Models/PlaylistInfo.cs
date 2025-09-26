

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
/// Represents detailed information about a playlist.
/// </summary>
public record PlaylistInfo
{
    /// <summary>
    /// Gets the playlist identifier (can be "radio" for radio stations).
    /// Internal use only - not exposed in API responses.
    /// </summary>
    [JsonIgnore]
    public string SubsonicPlaylistId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the playlist name (can be "Radio Stations" for radio).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the 1-based playlist index (1=Radio, 2=First Subsonic, etc.).
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Gets the total number of tracks in the playlist.
    /// </summary>
    public int TrackCount { get; init; }

    /// <summary>
    /// Gets the total duration of the playlist in seconds (null for radio).
    /// </summary>
    public int? TotalDurationSec { get; init; }

    /// <summary>
    /// Gets the playlist description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the playlist cover art URL.
    /// </summary>
    public string? CoverArtUrl { get; init; }

    /// <summary>
    /// Gets the source type ("subsonic" or "radio").
    /// </summary>
    public required string Source { get; init; }


}
