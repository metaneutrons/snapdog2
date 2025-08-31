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

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Clients.Queries;
using SnapDog2.Shared.Models;

/// <summary>
/// Handles the GetClientQuery.
/// </summary>
public partial class GetClientQueryHandler(IClientManager clientManager, ILogger<GetClientQueryHandler> logger)
    : IQueryHandler<GetClientQuery, Result<ClientState>>
{
    private readonly IClientManager _clientManager = clientManager;
    private readonly ILogger<GetClientQueryHandler> _logger = logger;

    [LoggerMessage(
        EventId = 8500,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Handling GetClientQuery for Client {ClientIndex}"
    )]
    private partial void LogHandling(int clientIndex);

    public async Task<Result<ClientState>> Handle(GetClientQuery request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ClientIndex);

        var result = await this._clientManager.GetClientStateAsync(request.ClientIndex).ConfigureAwait(false);
        return result;
    }
}
