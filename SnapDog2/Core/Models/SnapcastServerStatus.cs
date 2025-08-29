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
/// Represents the complete status of a Snapcast server in SnapDog2's domain model.
/// This is a mapped representation of the raw Snapcast server state.
/// </summary>
public record SnapcastServerStatus
{
    /// <summary>
    /// Server information and version details.
    /// </summary>
    public required SnapcastServerInfo Server { get; init; }

    /// <summary>
    /// All groups currently configured on the server.
    /// </summary>
    public required IReadOnlyList<SnapcastGroupInfo> Groups { get; init; }

    /// <summary>
    /// All streams currently available on the server.
    /// </summary>
    public required IReadOnlyList<SnapcastStreamInfo> Streams { get; init; }

    /// <summary>
    /// All clients currently connected to the server.
    /// </summary>
    public IReadOnlyList<SnapcastClientInfo> Clients => this.Groups.SelectMany(g => g.Clients).ToList().AsReadOnly();
}

/// <summary>
/// Represents Snapcast server information.
/// </summary>
public record SnapcastServerInfo
{
    /// <summary>
    /// Server version information.
    /// </summary>
    public required VersionDetails Version { get; init; }

    /// <summary>
    /// Server host name.
    /// </summary>
    public required string Host { get; init; }

    /// <summary>
    /// Server control port.
    /// </summary>
    public required int Port { get; init; }

    /// <summary>
    /// Server uptime in seconds.
    /// </summary>
    public required long UptimeSeconds { get; init; }
}

/// <summary>
/// Represents a Snapcast group in SnapDog2's domain model.
/// </summary>
public record SnapcastGroupInfo
{
    /// <summary>
    /// Unique group identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable group name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Whether the group is muted.
    /// </summary>
    public required bool Muted { get; init; }

    /// <summary>
    /// Current stream ID assigned to this group.
    /// </summary>
    public required string StreamId { get; init; }

    /// <summary>
    /// Clients that belong to this group.
    /// </summary>
    public required IReadOnlyList<SnapcastClientInfo> Clients { get; init; }
}

/// <summary>
/// Represents a Snapcast client in SnapDog2's domain model.
/// </summary>
public record SnapcastClientInfo
{
    /// <summary>
    /// Unique client indexentifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable client name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Whether the client is currently connected.
    /// </summary>
    public required bool Connected { get; init; }

    /// <summary>
    /// Client volume (0-100).
    /// </summary>
    public required int Volume { get; init; }

    /// <summary>
    /// Whether the client is muted.
    /// </summary>
    public required bool Muted { get; init; }

    /// <summary>
    /// Client latency in milliseconds.
    /// </summary>
    public required int LatencyMs { get; init; }

    /// <summary>
    /// Client host information.
    /// </summary>
    public required SnapcastClientHost Host { get; init; }

    /// <summary>
    /// Last time the client was seen (UTC).
    /// </summary>
    public required DateTime LastSeenUtc { get; init; }
}

/// <summary>
/// Represents Snapcast client host information.
/// </summary>
public record SnapcastClientHost
{
    /// <summary>
    /// Client IP address.
    /// </summary>
    public required string Ip { get; init; }

    /// <summary>
    /// Client hostname.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Client MAC address.
    /// </summary>
    public required string Mac { get; init; }

    /// <summary>
    /// Client operating system.
    /// </summary>
    public required string Os { get; init; }

    /// <summary>
    /// Client architecture.
    /// </summary>
    public required string Arch { get; init; }
}

/// <summary>
/// Represents a Snapcast stream in SnapDog2's domain model.
/// </summary>
public record SnapcastStreamInfo
{
    /// <summary>
    /// Unique stream identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Stream status (idle, playing, etc.).
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Stream URI/source.
    /// </summary>
    public required string Uri { get; init; }

    /// <summary>
    /// Stream metadata properties.
    /// </summary>
    public required IReadOnlyDictionary<string, object> Properties { get; init; }
}
