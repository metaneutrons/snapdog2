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
/// Represents server performance statistics.
/// </summary>
public record ServerStats
{
    /// <summary>
    /// Gets the UTC timestamp when the stats were recorded.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the CPU usage percentage (0-100).
    /// </summary>
    public required double CpuUsagePercent { get; init; }

    /// <summary>
    /// Gets the memory usage in megabytes.
    /// </summary>
    public required double MemoryUsageMb { get; init; }

    /// <summary>
    /// Gets the total available memory in megabytes.
    /// </summary>
    public required double TotalMemoryMb { get; init; }

    /// <summary>
    /// Gets the application uptime.
    /// </summary>
    public required TimeSpan Uptime { get; init; }

    /// <summary>
    /// Gets the number of active connections.
    /// </summary>
    public int ActiveConnections { get; init; }

    /// <summary>
    /// Gets the number of processed requests.
    /// </summary>
    public long ProcessedRequests { get; init; }
}
