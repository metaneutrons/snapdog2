namespace SnapDog2.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Health check controller providing application health status.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public partial class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthController"/> class.
    /// </summary>
    /// <param name="healthCheckService">The health check service.</param>
    /// <param name="logger">The logger.</param>
    public HealthController(HealthCheckService healthCheckService, ILogger<HealthController> logger)
    {
        this._healthCheckService = healthCheckService;
        this._logger = logger;
    }

    /// <summary>
    /// Gets the health status of the application.
    /// </summary>
    /// <returns>The health status.</returns>
    [HttpGet]
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
                    Description = entry.Value.Description,
                    Data = entry.Value.Data,
                    Exception = entry.Value.Exception?.Message,
                }),
            };

            var statusCode = healthReport.Status switch
            {
                HealthStatus.Healthy => StatusCodes.Status200OK,
                HealthStatus.Degraded => StatusCodes.Status200OK,
                HealthStatus.Unhealthy => StatusCodes.Status503ServiceUnavailable,
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
    [LoggerMessage(
        EventId = 2805,
        Level = LogLevel.Information,
        Message = "Health check completed with status {status} in {duration}ms"
    )]
    private partial void LogHealthCheckCompleted(HealthStatus status, double duration);

    [LoggerMessage(EventId = 2806, Level = LogLevel.Error, Message = "Health check failed with exception")]
    private partial void LogHealthCheckFailed(Exception ex);

    [LoggerMessage(EventId = 2807, Level = LogLevel.Error, Message = "Ready check failed with exception")]
    private partial void LogReadyCheckFailed(Exception ex);

    [LoggerMessage(EventId = 2808, Level = LogLevel.Error, Message = "Live check failed with exception")]
    private partial void LogLiveCheckFailed(Exception ex);
}
