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
using SnapDog2.Server.Features.Clients.Commands.Volume;

/// <summary>
/// Handles the ToggleClientMuteCommand.
/// </summary>
public partial class ToggleClientMuteCommandHandler(
    IClientManager clientManager,
    ILogger<ToggleClientMuteCommandHandler> logger
) : ICommandHandler<ToggleClientMuteCommand, Result>
{
    private readonly IClientManager _clientManager = clientManager;
    private readonly ILogger<ToggleClientMuteCommandHandler> _logger = logger;

    [LoggerMessage(
        EventId = 9100,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Toggling mute for Client {ClientIndex} from {Source}"
    )]
    private partial void LogHandling(int clientIndex, CommandSource source);

    [LoggerMessage(
        EventId = 9101,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Client {ClientIndex} not found for ToggleClientMuteCommand"
    )]
    private partial void LogClientNotFound(int clientIndex);

    [LoggerMessage(
        EventId = 9102,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Toggled mute for Client {ClientIndex} to {NewMuteState}"
    )]
    private partial void LogToggleResult(int clientIndex, bool newMuteState);

    public async Task<Result> Handle(ToggleClientMuteCommand request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ClientIndex, request.Source);

        // Get the client for operations
        var clientResult = await this._clientManager.GetClientAsync(request.ClientIndex).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            this.LogClientNotFound(request.ClientIndex);
            return clientResult;
        }

        var client = clientResult.Value!;

        // Use the IClient method directly
        var result = await client.ToggleMuteAsync().ConfigureAwait(false);

        if (result.IsSuccess)
        {
            // Note: The actual new mute state will be logged by the IClient method
            // We could get the current state if needed for logging
        }

        return result;
    }
}
