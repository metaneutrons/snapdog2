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

// TODO: MediaController temporarily disabled during mediator removal
// This controller needs to be updated to use direct service calls instead of query handlers
// or implement a playlist service similar to IZoneService/IClientService

/*
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Api.Models;
using SnapDog2.Server.Playlists.Queries;
using SnapDog2.Shared.Models;

/// <summary>
/// Media controller for managing playlists and tracks.
/// Provides access to Subsonic integration and media library functionality.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MediaController"/> class.
/// </remarks>
/// <param name="serviceProvider">Service provider for handler resolution.</param>
/// <param name="logger">Logger instance.</param>
[ApiController]
[Route("api/v1/media")]
[Authorize]
public partial class MediaController(IServiceProvider serviceProvider, ILogger<MediaController> logger) : ControllerBase
{
    // Implementation temporarily removed during mediator infrastructure cleanup
    // Will be restored with direct service calls in future update
}
*/
