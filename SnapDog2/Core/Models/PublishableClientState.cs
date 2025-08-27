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

namespace SnapDog2.Core.Models;

/// <summary>
/// Simplified client state for MQTT publishing - contains only user-facing information.
/// </summary>
public record PublishableClientState
{
    /// <summary>
    /// Gets the client name.
    /// </summary>
    [JsonPropertyName("Name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the client volume (0-100).
    /// </summary>
    [JsonPropertyName("Volume")]
    public required int Volume { get; init; }

    /// <summary>
    /// Gets whether the client is muted.
    /// </summary>
    [JsonPropertyName("Mute")]
    public required bool Mute { get; init; }

    /// <summary>
    /// Gets whether the client is currently connected.
    /// </summary>
    [JsonPropertyName("Connected")]
    public required bool Connected { get; init; }

    /// <summary>
    /// Gets the current zone index this client is assigned to.
    /// </summary>
    [JsonPropertyName("ZoneIndex")]
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the client latency in milliseconds.
    /// </summary>
    [JsonPropertyName("LatencyMs")]
    public required int LatencyMs { get; init; }
}
