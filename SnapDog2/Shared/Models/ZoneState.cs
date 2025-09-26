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

using SnapDog2.Shared.Enums;

/// <summary>
/// Represents the complete state of a zone.
/// </summary>
public record ZoneState
{
    /// <summary>
    /// Gets the zone name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the zone icon character.
    /// </summary>
    public string Icon { get; init; } = string.Empty;

    /// <summary>
    /// Gets the current playback state (true = playing, false = paused/stopped).
    /// </summary>
    public required bool PlaybackState { get; init; }

    /// <summary>
    /// Gets the zone volume (0-100).
    /// </summary>
    public required int Volume { get; init; }

    /// <summary>
    /// Gets whether the zone is muted.
    /// </summary>
    public required bool Mute { get; init; }

    /// <summary>
    /// Gets whether track repeat is enabled.
    /// </summary>
    public required bool TrackRepeat { get; init; }

    /// <summary>
    /// Gets whether playlist repeat is enabled.
    /// </summary>
    public required bool PlaylistRepeat { get; init; }

    /// <summary>
    /// Gets whether playlist shuffle is enabled.
    /// </summary>
    public required bool PlaylistShuffle { get; init; }

    /// <summary>
    /// Gets the Snapcast group ID.
    /// </summary>
    public required string SnapcastGroupId { get; init; }

    /// <summary>
    /// Gets the Snapcast stream ID.
    /// </summary>
    public required string SnapcastStreamId { get; init; }

    /// <summary>
    /// Gets the current playlist information.
    /// </summary>
    public PlaylistInfo? Playlist { get; init; }

    /// <summary>
    /// Gets the current track information.
    /// </summary>
    public TrackInfo? Track { get; init; }

    /// <summary>
    /// Gets the list of SnapDog2 Client Indexs currently in this zone.
    /// </summary>
    public required int[] Clients { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the zone state was recorded.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
