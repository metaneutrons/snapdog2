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
namespace SnapDog2.Domain.Services;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using SnapDog2.Domain.Abstractions;

/// <summary>
/// Wrapper for HealthCheckService to enable proper unit testing.
/// Implements IAppHealthCheckService interface to make the service mockable.
/// </summary>
public class AppHealthCheckService(HealthCheckService healthCheckService) : IAppHealthCheckService
{
    private readonly HealthCheckService _healthCheckService = healthCheckService;

    /// <inheritdoc/>
    public async Task<HealthReport> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        return await _healthCheckService.CheckHealthAsync(cancellationToken);
    }
}
