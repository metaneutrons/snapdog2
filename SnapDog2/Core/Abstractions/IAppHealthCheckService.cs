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

using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Interface for health check service to enable proper unit testing.
/// Wraps the concrete HealthCheckService to make it mockable.
/// </summary>
public interface IAppHealthCheckService
{
    /// <summary>
    /// Runs the health checks and returns the aggregated health status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The health report containing the status of all health checks.</returns>
    Task<HealthReport> CheckHealthAsync(CancellationToken cancellationToken = default);
}
