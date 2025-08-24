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

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Commands.Config;

/// <summary>
/// Handles the SetClientNameCommand.
/// </summary>
public partial class SetClientNameCommandHandler(
    IClientManager clientManager,
    ILogger<SetClientNameCommandHandler> logger
) : ICommandHandler<SetClientNameCommand, Result>
{
    private readonly IClientManager _clientManager = clientManager;
    private readonly ILogger<SetClientNameCommandHandler> _logger = logger;

    [LoggerMessage(3010, LogLevel.Information, "Setting name for Client {ClientIndex} to '{Name}' from {Source}")]
    private partial void LogHandling(int clientIndex, string name, CommandSource source);

    [LoggerMessage(3011, LogLevel.Warning, "Client {ClientIndex} not found for SetClientNameCommand")]
    private partial void LogClientNotFound(int clientIndex);

    public async Task<Result> Handle(SetClientNameCommand request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ClientIndex, request.Name, request.Source);

        // Get the client
        var clientResult = await this._clientManager.GetClientAsync(request.ClientIndex).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            this.LogClientNotFound(request.ClientIndex);
            return clientResult;
        }

        var client = clientResult.Value!;

        // Set the name
        var result = await client.SetNameAsync(request.Name).ConfigureAwait(false);

        return result;
    }
}
