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
namespace SnapDog2.Core.Abstractions;

using SnapDog2.Core.Models;

/// <summary>
/// Service for recording application metrics.
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Records the duration of a Cortex.Mediator request.
    /// </summary>
    /// <param name="requestType">The type of request (Command/Query).</param>
    /// <param name="requestName">The name of the request.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    /// <param name="success">Whether the request was successful.</param>
    void RecordCortexMediatorRequestDuration(string requestType, string requestName, long durationMs, bool success);

    /// <summary>
    /// Gets the current server performance statistics.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the server statistics.</returns>
    Task<ServerStats> GetServerStatsAsync();

    // Lightweight metrics hooks (stubbed until full telemetry is implemented)
    void IncrementCounter(string name, long delta = 1, params (string Key, string Value)[] labels);
    void SetGauge(string name, double value, params (string Key, string Value)[] labels);

    // Additional metric recording methods for comprehensive monitoring
    void RecordError(string errorType, string component, string? operation = null);
    void RecordException(Exception exception, string component, string? operation = null);
}
