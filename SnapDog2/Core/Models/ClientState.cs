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
namespace SnapDog2.Core.Models;

/// <summary>
/// Represents the complete state of a Snapcast client.
/// </summary>
public record ClientState
{
    /// <summary>
    /// Gets the client Index.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the client name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the client MAC address.
    /// </summary>
    public required string Mac { get; init; }

    /// <summary>
    /// Gets the Snapcast client Index (UUID from Snapcast server).
    /// </summary>
    public required string SnapcastId { get; init; }

    /// <summary>
    /// Gets the client volume (0-100).
    /// </summary>
    public required int Volume { get; init; }

    /// <summary>
    /// Gets whether the client is muted.
    /// </summary>
    public required bool Mute { get; init; }

    /// <summary>
    /// Gets whether the client is currently connected to Snapcast server.
    /// </summary>
    public required bool Connected { get; init; }

    /// <summary>
    /// Gets the current zone ID this client is assigned to.
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the client latency in milliseconds.
    /// </summary>
    public required int LatencyMs { get; init; }

    /// <summary>
    /// Gets the configured Snapcast client name.
    /// </summary>
    public required string ConfiguredSnapcastName { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the client was last seen.
    /// </summary>
    public required DateTime LastSeenUtc { get; init; }

    /// <summary>
    /// Gets the client host IP address.
    /// </summary>
    public required string HostIpAddress { get; init; }

    /// <summary>
    /// Gets the client host name.
    /// </summary>
    public required string HostName { get; init; }

    /// <summary>
    /// Gets the client host operating system.
    /// </summary>
    public required string HostOs { get; init; }

    /// <summary>
    /// Gets the client host architecture.
    /// </summary>
    public required string HostArch { get; init; }

    /// <summary>
    /// Gets the Snapcast client version.
    /// </summary>
    public string? SnapClientVersion { get; init; }

    /// <summary>
    /// Gets the Snapcast client protocol version.
    /// </summary>
    public string? SnapClientProtocolVersion { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the client state was recorded.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
