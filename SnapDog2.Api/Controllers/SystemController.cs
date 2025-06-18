using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Api.Models;

namespace SnapDog2.Api.Controllers;

/// <summary>
/// System endpoints for health checks, status information, and basic API diagnostics.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class SystemController : ControllerBase
{
    private readonly ILogger<SystemController> _logger;

    public SystemController(ILogger<SystemController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Health check endpoint that returns the current health status of the API.
    /// </summary>
    /// <returns>Health status information.</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<SystemHealthDto>), 200)]
    public ActionResult<ApiResponse<SystemHealthDto>> GetHealth()
    {
        _logger.LogInformation("Health check requested");

        var health = new SystemHealthDto
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = GetApiVersion(),
            Uptime = GetUptime(),
            Environment = GetEnvironmentName(),
        };

        return Ok(ApiResponse<SystemHealthDto>.Ok(health));
    }

    /// <summary>
    /// Status endpoint that returns detailed system status information.
    /// </summary>
    /// <returns>Detailed system status.</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<SystemStatusDto>), 200)]
    public ActionResult<ApiResponse<SystemStatusDto>> GetStatus()
    {
        _logger.LogInformation("System status requested");

        var status = new SystemStatusDto
        {
            ApiName = "SnapDog2 API",
            Version = GetApiVersion(),
            Status = "Running",
            Timestamp = DateTime.UtcNow,
            Uptime = GetUptime(),
            Environment = GetEnvironmentName(),
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            WorkingSet = GC.GetTotalMemory(false),
            ThreadCount = System.Diagnostics.Process.GetCurrentProcess().Threads.Count,
        };

        return Ok(ApiResponse<SystemStatusDto>.Ok(status));
    }

    /// <summary>
    /// Simple ping endpoint for connectivity testing.
    /// </summary>
    /// <returns>Pong response.</returns>
    [HttpGet("ping")]
    [ProducesResponseType(typeof(ApiResponse<PingResponseDto>), 200)]
    public ActionResult<ApiResponse<PingResponseDto>> Ping()
    {
        var response = new PingResponseDto
        {
            Message = "Pong",
            Timestamp = DateTime.UtcNow,
            RequestId = HttpContext.TraceIdentifier,
        };

        return Ok(ApiResponse<PingResponseDto>.Ok(response));
    }

    /// <summary>
    /// System information endpoint that returns detailed system information.
    /// </summary>
    /// <returns>System information.</returns>
    [HttpGet("info")]
    [ProducesResponseType(typeof(ApiResponse<SystemInfoDto>), 200)]
    public ActionResult<ApiResponse<SystemInfoDto>> GetSystemInfo()
    {
        _logger.LogInformation("System information requested");

        var info = new SystemInfoDto
        {
            ApiName = "SnapDog2 API",
            Version = GetApiVersion(),
            Description = "Multi-room audio streaming system API",
            BuildDate = GetBuildDate(),
            RuntimeVersion = Environment.Version.ToString(),
            OperatingSystem = Environment.OSVersion.ToString(),
            MachineName = Environment.MachineName,
            ProcessorCount = Environment.ProcessorCount,
            Environment = GetEnvironmentName(),
            Endpoints = GetAvailableEndpoints(),
        };

        return Ok(ApiResponse<SystemInfoDto>.Ok(info));
    }

    private string GetApiVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
    }

    private string GetUptime()
    {
        var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
        return uptime.ToString(@"dd\.hh\:mm\:ss");
    }

    private string GetEnvironmentName()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
    }

    private DateTime GetBuildDate()
    {
        // Simple approach - use assembly creation time as build date
        var assembly = Assembly.GetExecutingAssembly();
        var creationTime = System.IO.File.GetCreationTime(assembly.Location);
        return creationTime;
    }

    private List<string> GetAvailableEndpoints()
    {
        return new List<string>
        {
            "GET /api/system/health",
            "GET /api/system/status",
            "GET /api/system/ping",
            "GET /api/system/info",
        };
    }
}

/// <summary>
/// System health information.
/// </summary>
public class SystemHealthDto
{
    /// <summary>
    /// Current health status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the health check.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// API version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// System uptime.
    /// </summary>
    public string Uptime { get; set; } = string.Empty;

    /// <summary>
    /// Current environment.
    /// </summary>
    public string Environment { get; set; } = string.Empty;
}

/// <summary>
/// Detailed system status information.
/// </summary>
public class SystemStatusDto
{
    /// <summary>
    /// API name.
    /// </summary>
    public string ApiName { get; set; } = string.Empty;

    /// <summary>
    /// API version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Current status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Status timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// System uptime.
    /// </summary>
    public string Uptime { get; set; } = string.Empty;

    /// <summary>
    /// Current environment.
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Machine name.
    /// </summary>
    public string MachineName { get; set; } = string.Empty;

    /// <summary>
    /// Process ID.
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// Working set memory usage.
    /// </summary>
    public long WorkingSet { get; set; }

    /// <summary>
    /// Thread count.
    /// </summary>
    public int ThreadCount { get; set; }
}

/// <summary>
/// Ping response information.
/// </summary>
public class PingResponseDto
{
    /// <summary>
    /// Response message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Response timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Request ID for tracing.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;
}

/// <summary>
/// Detailed system information.
/// </summary>
public class SystemInfoDto
{
    /// <summary>
    /// API name.
    /// </summary>
    public string ApiName { get; set; } = string.Empty;

    /// <summary>
    /// API version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// API description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Build date.
    /// </summary>
    public DateTime BuildDate { get; set; }

    /// <summary>
    /// .NET runtime version.
    /// </summary>
    public string RuntimeVersion { get; set; } = string.Empty;

    /// <summary>
    /// Operating system information.
    /// </summary>
    public string OperatingSystem { get; set; } = string.Empty;

    /// <summary>
    /// Machine name.
    /// </summary>
    public string MachineName { get; set; } = string.Empty;

    /// <summary>
    /// Processor count.
    /// </summary>
    public int ProcessorCount { get; set; }

    /// <summary>
    /// Current environment.
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Available API endpoints.
    /// </summary>
    public List<string> Endpoints { get; set; } = new();
}
