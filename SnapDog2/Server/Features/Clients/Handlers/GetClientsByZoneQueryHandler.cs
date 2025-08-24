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
namespace SnapDog2.Server.Features.Clients.Handlers;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Queries;

/// <summary>
/// Handles the GetClientsByZoneQuery.
/// </summary>
public partial class GetClientsByZoneQueryHandler(
    IClientManager clientManager,
    ILogger<GetClientsByZoneQueryHandler> logger
) : IQueryHandler<GetClientsByZoneQuery, Result<List<ClientState>>>
{
    private readonly IClientManager _clientManager = clientManager;
    private readonly ILogger<GetClientsByZoneQueryHandler> _logger = logger;

    [LoggerMessage(4201, LogLevel.Information, "Handling GetClientsByZoneQuery for Zone {ZoneIndex}")]
    private partial void LogHandling(int zoneIndex);

    public async Task<Result<List<ClientState>>> Handle(
        GetClientsByZoneQuery request,
        CancellationToken cancellationToken
    )
    {
        this.LogHandling(request.ZoneIndex);

        var result = await this
            ._clientManager.GetClientsByZoneAsync(request.ZoneIndex, cancellationToken)
            .ConfigureAwait(false);
        return result;
    }
}
