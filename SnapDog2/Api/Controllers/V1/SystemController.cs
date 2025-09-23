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
namespace SnapDog2.Api.Controllers.V1;

using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Attributes;
using Swashbuckle.AspNetCore.Annotations;

/// <summary>
/// System status controller for blueprint compliance.
/// </summary>
[ApiController]
[Route("api/v1/system")]
[Authorize]
[Produces("application/json")]
[Tags("System")]
public class SystemController : ControllerBase
{
    private readonly ICommandStatusService _commandStatusService;
    private readonly IErrorTrackingService _errorTrackingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemController"/> class.
    /// </summary>
    /// <param name="commandStatusService">The command status service.</param>
    /// <param name="errorTrackingService">The error tracking service.</param>
    public SystemController(
        ICommandStatusService commandStatusService,
        IErrorTrackingService errorTrackingService)
    {
        _commandStatusService = commandStatusService;
        _errorTrackingService = errorTrackingService;
    }
    /// <summary>
    /// Gets command status.
    /// </summary>
    [HttpGet("commands/status")]
    [StatusId("COMMAND_STATUS")]
    [SwaggerOperation(OperationId = "getCommandStatus")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetCommandStatus()
    {
        var status = await _commandStatusService.GetStatusAsync();
        var errors = await _commandStatusService.GetRecentErrorsAsync();

        return Ok(new
        {
            status = status,
            lastCommand = errors.Length > 0 ? "error" : "",
            timestamp = DateTimeOffset.UtcNow,
            errorCount = errors.Length
        });
    }

    /// <summary>
    /// Gets command error information.
    /// </summary>
    [HttpGet("commands/errors")]
    [StatusId("COMMAND_ERROR")]
    [SwaggerOperation(OperationId = "getCommandErrors")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetCommandError()
    {
        var errors = await _commandStatusService.GetRecentErrorsAsync();
        var latestError = errors.Length > 0 ? errors[0] : "";

        return Ok(new
        {
            error = latestError,
            timestamp = DateTimeOffset.UtcNow,
            command = latestError.Length > 0 ? "unknown" : "",
            totalErrors = errors.Length
        });
    }

    /// <summary>
    /// Gets system error information.
    /// </summary>
    [HttpGet("errors")]
    [StatusId("SYSTEM_ERROR")]
    [SwaggerOperation(OperationId = "getSystemErrors")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetSystemError()
    {
        var latestError = await _errorTrackingService.GetLatestErrorAsync();

        return Ok(new
        {
            error = latestError?.Message ?? "",
            timestamp = latestError?.TimestampUtc ?? DateTime.UtcNow,
            component = latestError?.Component ?? "",
            errorCode = latestError?.ErrorCode ?? "",
            level = latestError?.Level ?? 0,
            context = latestError?.Context ?? ""
        });
    }

    /// <summary>
    /// Gets system status.
    /// </summary>
    [HttpGet("status")]
    [StatusId("SYSTEM_STATUS")]
    [SwaggerOperation(OperationId = "getSystemStatus")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetSystemStatus()
    {
        var uptime = DateTimeOffset.UtcNow - Process.GetCurrentProcess().StartTime;

        return await Task.FromResult(Ok(new
        {
            status = "running",
            uptime = uptime,
            version = GetVersion()
        }));
    }

    /// <summary>
    /// Gets version information.
    /// </summary>
    [HttpGet("version")]
    [StatusId("VERSION_INFO")]
    [SwaggerOperation(OperationId = "getSystemVersion")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetVersionInfo()
    {
        var version = GetVersion();
        var buildTime = GetBuildTime();

        return await Task.FromResult(Ok(new
        {
            version = version,
            build = "release",
            timestamp = buildTime
        }));
    }

    /// <summary>
    /// Gets server statistics.
    /// </summary>
    [HttpGet("stats")]
    [StatusId("SERVER_STATS")]
    [SwaggerOperation(OperationId = "getSystemStats")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetServerStats()
    {
        var uptime = DateTimeOffset.UtcNow - Process.GetCurrentProcess().StartTime;
        var process = Process.GetCurrentProcess();

        return await Task.FromResult(Ok(new
        {
            uptime = uptime,
            memoryUsage = process.WorkingSet64,
            cpuTime = process.TotalProcessorTime
        }));
    }

    private static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0";
    }

    private static DateTimeOffset GetBuildTime()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var location = assembly.Location;
        if (System.IO.File.Exists(location))
        {
            return System.IO.File.GetCreationTime(location);
        }
        return DateTimeOffset.UtcNow;
    }
}
