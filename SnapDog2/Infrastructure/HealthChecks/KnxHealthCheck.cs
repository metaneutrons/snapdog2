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
namespace SnapDog2.Infrastructure.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Enums;

/// <summary>
/// Health check for KNX integration using Falcon SDK connection status.
/// Provides accurate health assessment based on actual KNX bus connection state.
/// </summary>
public class KnxHealthCheck : IHealthCheck
{
    private readonly IKnxService _knxService;

    public KnxHealthCheck(IKnxService knxService)
    {
        this._knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = this._knxService.Status;
            var isConnected = this._knxService.IsConnected;

            return status switch
            {
                ServiceStatus.Running when isConnected => Task.FromResult(
                    HealthCheckResult.Healthy("KNX service is connected and operational",
                        new Dictionary<string, object>
                        {
                            ["Status"] = status.ToString(),
                            ["IsConnected"] = isConnected,
                            ["ConnectionType"] = "Falcon SDK"
                        })),

                ServiceStatus.Stopped => Task.FromResult(
                    HealthCheckResult.Healthy("KNX service is disabled",
                        new Dictionary<string, object>
                        {
                            ["Status"] = status.ToString(),
                            ["IsConnected"] = isConnected,
                            ["Reason"] = "Service not initialized (likely disabled in configuration)"
                        })),

                ServiceStatus.Error => Task.FromResult(
                    HealthCheckResult.Unhealthy("KNX service is initialized but not connected",
                        data: new Dictionary<string, object>
                        {
                            ["Status"] = status.ToString(),
                            ["IsConnected"] = isConnected,
                            ["Reason"] = "Service initialized but KNX bus connection failed"
                        })),

                _ => Task.FromResult(
                    HealthCheckResult.Degraded($"KNX service in unexpected state: {status}",
                        data: new Dictionary<string, object>
                        {
                            ["Status"] = status.ToString(),
                            ["IsConnected"] = isConnected
                        }))
            };
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Error checking KNX service health", ex,
                    data: new Dictionary<string, object>
                    {
                        ["Error"] = ex.Message
                    }));
        }
    }
}
