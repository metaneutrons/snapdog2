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

using Microsoft.AspNetCore.Mvc;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Models;

/// <summary>
/// Controller for system status and information endpoints.
/// </summary>
[ApiController]
[Route("api/v1/system")]
public class SystemController : ControllerBase
{
    private readonly IAppStatusService _appStatusService;

    public SystemController(IAppStatusService appStatusService)
    {
        _appStatusService = appStatusService;
    }

    [HttpGet("status")]
    [ProducesResponseType<SystemStatus>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemStatus>> GetSystemStatus()
    {
        var status = await _appStatusService.GetCurrentStatusAsync();
        return Ok(status);
    }

    [HttpGet("version")]
    [ProducesResponseType<VersionDetails>(StatusCodes.Status200OK)]
    public async Task<ActionResult<VersionDetails>> GetVersionInfo()
    {
        var version = await _appStatusService.GetVersionInfoAsync();
        return Ok(version);
    }

    [HttpGet("stats")]
    [ProducesResponseType<ServerStats>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ServerStats>> GetServerStats()
    {
        var stats = await _appStatusService.GetServerStatsAsync();
        return Ok(stats);
    }
}
