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

// TODO: SystemController temporarily disabled during mediator removal
// This controller needs to be updated to use direct service calls instead of query handlers
// or implement a system status service similar to IZoneService/IClientService

/*
using Cortex.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Api.Models;
using SnapDog2.Server.Global.Handlers;
using SnapDog2.Server.Global.Queries;
using SnapDog2.Shared.Models;

/// <summary>
/// Controller for system-wide information endpoints.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SystemController"/> class.
/// </remarks>
[ApiController]
[Route("api/v1/system")]
[Authorize]
[Produces("application/json")]
[Tags("System")]
public partial class SystemController(
    IServiceProvider serviceProvider,
    IMediator mediator,
    ILogger<SystemController> logger
) : ControllerBase
{
    // Implementation temporarily removed during mediator infrastructure cleanup
    // Will be restored with direct service calls in future update
}
*/
