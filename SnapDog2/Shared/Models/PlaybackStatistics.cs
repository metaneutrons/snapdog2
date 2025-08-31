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

/// <summary>
/// Statistics about the media player service and active playback sessions.
/// </summary>
public class PlaybackStatistics
{
    /// <summary>
    /// Total number of active playback sessions across all zones.
    /// </summary>
    public int ActiveSessions { get; set; }

    /// <summary>
    /// Maximum number of concurrent sessions allowed.
    /// </summary>
    public int MaxSessions { get; set; }

    /// <summary>
    /// Total number of playback sessions started since service startup.
    /// </summary>
    public long TotalSessionsStarted { get; set; }

    /// <summary>
    /// Total number of playback errors encountered since service startup.
    /// </summary>
    public long TotalErrors { get; set; }

    /// <summary>
    /// Service uptime since last restart.
    /// </summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// Timestamp when the service was started (UTC).
    /// </summary>
    public DateTime ServiceStartedAt { get; set; }

    /// <summary>
    /// List of zones with their current playback status.
    /// </summary>
    public List<PlaybackStatus> ZoneStatuses { get; set; } = new();

    /// <summary>
    /// Current memory usage of the service in bytes.
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>
    /// Average CPU usage percentage over the last minute.
    /// </summary>
    public double CpuUsagePercent { get; set; }

    // Additional properties used in the implementation
    /// <summary>
    /// Number of active audio streams.
    /// </summary>
    public int ActiveStreams { get; set; }

    /// <summary>
    /// Maximum number of concurrent streams allowed.
    /// </summary>
    public int MaxStreams { get; set; }

    /// <summary>
    /// Number of configured zones.
    /// </summary>
    public int ConfiguredZones { get; set; }

    /// <summary>
    /// Number of active zones.
    /// </summary>
    public int ActiveZones { get; set; }

    /// <summary>
    /// Audio format being used for playback.
    /// </summary>
    public AudioFormat? AudioFormat { get; set; }

    /// <summary>
    /// Service uptime in seconds.
    /// </summary>
    public long UptimeSeconds { get; set; }
}
