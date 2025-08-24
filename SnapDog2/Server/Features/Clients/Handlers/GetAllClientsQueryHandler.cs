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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Queries;

/// <summary>
/// Handles the GetAllClientsQuery.
/// </summary>
public partial class GetAllClientsQueryHandler(IClientManager clientManager, ILogger<GetAllClientsQueryHandler> logger)
    : IQueryHandler<GetAllClientsQuery, Result<List<ClientState>>>
{
    private readonly IClientManager _clientManager = clientManager;
    private readonly ILogger<GetAllClientsQueryHandler> _logger = logger;

    [LoggerMessage(
        EventId = 8400,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Handling GetAllClientsQuery"
    )]
    private partial void LogHandling();

    [LoggerMessage(
        EventId = 8401,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Error retrieving all clients: {ErrorMessage}"
    )]
    private partial void LogError(string errorMessage);

    public async Task<Result<List<ClientState>>> Handle(GetAllClientsQuery request, CancellationToken cancellationToken)
    {
        this.LogHandling();

        try
        {
            var result = await this._clientManager.GetAllClientsAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            this.LogError(ex.Message);
            return Result<List<ClientState>>.Failure(ex.Message ?? "An error occurred while retrieving all clients");
        }
    }
}
