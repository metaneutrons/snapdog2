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
using SnapDog2.Shared.Attributes;

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
    /// <summary>
    /// Gets command status.
    /// </summary>
    [HttpGet("commands/status")]
    [StatusId("COMMAND_STATUS")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetCommandStatus()
    {
        return await Task.FromResult(Ok(new
        {
            status = "ready",
            lastCommand = "",
            timestamp = DateTimeOffset.UtcNow
        }));
    }

    /// <summary>
    /// Gets command error information.
    /// </summary>
    [HttpGet("commands/errors")]
    [StatusId("COMMAND_ERROR")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetCommandError()
    {
        return await Task.FromResult(Ok(new
        {
            error = "",
            timestamp = DateTimeOffset.UtcNow,
            command = ""
        }));
    }

    /// <summary>
    /// Gets system error information.
    /// </summary>
    [HttpGet("errors")]
    [StatusId("SYSTEM_ERROR")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetSystemError()
    {
        return await Task.FromResult(Ok(new
        {
            error = "",
            timestamp = DateTimeOffset.UtcNow,
            component = ""
        }));
    }

    /// <summary>
    /// Gets system status.
    /// </summary>
    [HttpGet("status")]
    [StatusId("SYSTEM_STATUS")]
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
