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
using Microsoft.Extensions.Options;
using SnapDog2.Api.Models;
using SnapDog2.Shared.Configuration;

/// <summary>
/// Controller for retrieving zone and client icons.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public partial class IconController : ControllerBase
{
    private readonly SnapDogConfiguration _config;

    public IconController(IOptions<SnapDogConfiguration> config)
    {
        _config = config.Value;
    }

    /// <summary>
    /// Get all zone and client icons.
    /// </summary>
    /// <returns>Dictionary of zone and client icons</returns>
    [HttpGet]
    [ProducesResponseType<IconResponse>(StatusCodes.Status200OK)]
    public ActionResult<IconResponse> GetIcons()
    {
        var zones = _config.Zones
            .Select((z, index) => new { Config = z, Index = index + 1 })
            .Where(z => !string.IsNullOrEmpty(z.Config.Icon))
            .ToDictionary(z => $"zone_{z.Index}", z => z.Config.Icon);

        var clients = _config.Clients
            .Select((c, index) => new { Config = c, Index = index + 1 })
            .Where(c => !string.IsNullOrEmpty(c.Config.Icon))
            .ToDictionary(c => $"client_{c.Index}", c => c.Config.Icon);

        return Ok(new IconResponse
        {
            Zones = zones,
            Clients = clients
        });
    }
}

/// <summary>
/// Response model for icon endpoint.
/// </summary>
public record IconResponse
{
    /// <summary>
    /// Zone icons keyed by "zone_{index}".
    /// </summary>
    public Dictionary<string, string> Zones { get; init; } = new();

    /// <summary>
    /// Client icons keyed by "client_{index}".
    /// </summary>
    public Dictionary<string, string> Clients { get; init; } = new();
}
