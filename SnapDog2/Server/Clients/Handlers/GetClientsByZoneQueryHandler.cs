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
namespace SnapDog2.Server.Clients.Handlers;

using Cortex.Mediator.Queries;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Clients.Queries;
using SnapDog2.Shared.Models;

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

    [LoggerMessage(EventId = 112500, Level = LogLevel.Information, Message = "Handling GetClientsByZoneQuery for Zone {ZoneIndex}"
)]
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
