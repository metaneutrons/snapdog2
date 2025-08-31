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

using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Clients.Commands.Volume;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// Handler for ClientVolumeDownCommand.
/// </summary>
public partial class ClientVolumeDownCommandHandler(
    IClientManager clientManager,
    ILogger<ClientVolumeDownCommandHandler> logger
) : ICommandHandler<ClientVolumeDownCommand, Result>
{
    private readonly IClientManager _clientManager = clientManager;
    private readonly ILogger<ClientVolumeDownCommandHandler> _logger = logger;

    [LoggerMessage(
        EventId = 8200,
        Level = LogLevel.Information,
        Message = "Decreasing volume for Client {ClientIndex} by {Step} from {Source}"
    )]
    private partial void LogHandling(int clientIndex, int step, CommandSource source);

    [LoggerMessage(
        EventId = 8201,
        Level = LogLevel.Warning,
        Message = "Client {ClientIndex} not found for ClientVolumeDownCommand"
    )]
    private partial void LogClientNotFound(int clientIndex);

    public async Task<Result> Handle(ClientVolumeDownCommand request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ClientIndex, request.Step, request.Source);

        // Get the client for operations
        var clientResult = await this._clientManager.GetClientAsync(request.ClientIndex).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            this.LogClientNotFound(request.ClientIndex);
            return clientResult;
        }

        var client = clientResult.Value!;

        // Use the IClient method directly
        var result = await client.VolumeDownAsync(request.Step).ConfigureAwait(false);

        return result;
    }
}
