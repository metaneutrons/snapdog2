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
namespace SnapDog2.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SnapDog2.Shared.Attributes;
using Swashbuckle.AspNetCore.Annotations;

/// <summary>
/// Health check controller providing application health status.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="HealthController"/> class.
/// </remarks>
/// <param name="healthCheckService">The health check service.</param>
/// <param name="logger">The logger.</param>
[ApiController]
[Route("api/health")]
public partial class HealthController(HealthCheckService healthCheckService, ILogger<HealthController> logger)
    : ControllerBase
{
    private readonly HealthCheckService _healthCheckService = healthCheckService;
    private readonly ILogger<HealthController> _logger = logger;

    /// <summary>
    /// Gets the health status of the application.
    /// </summary>
    /// <returns>The health status.</returns>
    [HttpGet]
    [SwaggerOperation(OperationId = "getHealth")]
    public async Task<IActionResult> GetHealthAsync()
    {
        try
        {
            var healthReport = await this._healthCheckService.CheckHealthAsync();

            var response = new
            {
                Status = healthReport.Status.ToString(),
                TotalDuration = healthReport.TotalDuration.TotalMilliseconds,
                Checks = healthReport.Entries.Select(entry => new
                {
                    Name = entry.Key,
                    Status = entry.Value.Status.ToString(),
                    Duration = entry.Value.Duration.TotalMilliseconds,
                    entry.Value.Description,
                    entry.Value.Data,
                    Exception = entry.Value.Exception?.Message,
                }),
            };

            var statusCode = healthReport.Status switch
            {
                HealthStatus.Healthy => StatusCodes.Status200OK,
                HealthStatus.Degraded => StatusCodes.Status200OK,
                _ => StatusCodes.Status503ServiceUnavailable,
            };

            this.LogHealthCheckCompleted(healthReport.Status, healthReport.TotalDuration.TotalMilliseconds);

            return this.StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            this.LogHealthCheckFailed(ex);
            return this.StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { Status = "Unhealthy", Error = "Health check failed" }
            );
        }
    }

    /// <summary>
    /// Gets a simple ready status for container orchestration.
    /// </summary>
    /// <returns>Ready status.</returns>
    [HttpGet("ready")]
    [StatusId("HEALTH_READY")]
    [SwaggerOperation(OperationId = "getHealthReady")]
    public async Task<IActionResult> GetReadyAsync()
    {
        try
        {
            var healthReport = await this._healthCheckService.CheckHealthAsync(check => check.Tags.Contains("ready"));

            if (healthReport.Status == HealthStatus.Healthy)
            {
                return this.Ok(new { Status = "Ready" });
            }

            return this.StatusCode(StatusCodes.Status503ServiceUnavailable, new { Status = "Not Ready" });
        }
        catch (Exception ex)
        {
            this.LogReadyCheckFailed(ex);
            return this.StatusCode(StatusCodes.Status503ServiceUnavailable, new { Status = "Not Ready" });
        }
    }

    /// <summary>
    /// Gets a simple live status for container orchestration.
    /// </summary>
    /// <returns>Live status.</returns>
    [HttpGet("live")]
    [StatusId("HEALTH_LIVE")]
    [SwaggerOperation(OperationId = "getHealthLive")]
    public async Task<IActionResult> GetLiveAsync()
    {
        try
        {
            var healthReport = await this._healthCheckService.CheckHealthAsync(check => check.Tags.Contains("live"));

            if (healthReport.Status == HealthStatus.Healthy)
            {
                return this.Ok(new { Status = "Live" });
            }

            return this.StatusCode(StatusCodes.Status503ServiceUnavailable, new { Status = "Not Live" });
        }
        catch (Exception ex)
        {
            this.LogLiveCheckFailed(ex);
            return this.StatusCode(StatusCodes.Status503ServiceUnavailable, new { Status = "Not Live" });
        }
    }

    // LoggerMessage definitions for high-performance logging (ID range: 2805-2808)

    // Health check operations
    [LoggerMessage(EventId = 13043, Level = LogLevel.Information, Message = "Health check completed with status {status} in {duration}ms"
)]
    private partial void LogHealthCheckCompleted(HealthStatus status, double duration);

    [LoggerMessage(EventId = 13044, Level = LogLevel.Error, Message = "Health check failed with exception")]
    private partial void LogHealthCheckFailed(Exception ex);

    [LoggerMessage(EventId = 13045, Level = LogLevel.Error, Message = "Ready check failed with exception")]
    private partial void LogReadyCheckFailed(Exception ex);

    [LoggerMessage(EventId = 13046, Level = LogLevel.Error, Message = "Live check failed with exception")]
    private partial void LogLiveCheckFailed(Exception ex);
}
