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
namespace SnapDog2.Domain.Abstractions;

using SnapDog2.Infrastructure.Metrics;

/// <summary>
/// Interface for application metrics collection and monitoring.
/// Provides comprehensive telemetry across all application layers.
/// </summary>
public interface IApplicationMetrics : IDisposable
{
    /// <summary>
    /// Records HTTP request metrics including method, endpoint, status code, and duration.
    /// </summary>
    void RecordHttpRequest(string method, string endpoint, int statusCode, double durationSeconds);

    /// <summary>
    /// Records command execution metrics including name, duration, and success status.
    /// </summary>
    void RecordCommand(string commandName, double durationSeconds, bool success);

    /// <summary>
    /// Records query execution metrics including name, duration, and success status.
    /// </summary>
    void RecordQuery(string queryName, double durationSeconds, bool success);

    /// <summary>
    /// Records error metrics with type, component, and optional operation context.
    /// </summary>
    void RecordError(string errorType, string component, string? operation = null);

    /// <summary>
    /// Records exception metrics with automatic error type detection.
    /// </summary>
    void RecordException(Exception exception, string component, string? operation = null);

    /// <summary>
    /// Updates system metrics including CPU, memory, and performance counters.
    /// </summary>
    void UpdateSystemMetrics(SystemMetricsState systemMetrics);

    /// <summary>
    /// Updates business metrics including zone counts, track counts, and operational data.
    /// </summary>
    void UpdateBusinessMetrics(BusinessMetricsState businessMetrics);

    /// <summary>
    /// Records track change events for audio playback monitoring.
    /// </summary>
    void RecordTrackChange(string zoneIndex, string? fromTrack, string? toTrack);

    /// <summary>
    /// Records volume change events for audio control monitoring.
    /// </summary>
    void RecordVolumeChange(string targetId, string targetType, int fromVolume, int toVolume);
}
