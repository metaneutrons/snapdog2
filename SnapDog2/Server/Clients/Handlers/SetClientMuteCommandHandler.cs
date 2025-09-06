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
using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Clients.Commands.Volume;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// Handles the SetClientMuteCommand.
/// </summary>
public partial class SetClientMuteCommandHandler(
    IClientManager clientManager,
    ILogger<SetClientMuteCommandHandler> logger
) : ICommandHandler<SetClientMuteCommand, Result>
{
    private readonly IClientManager _clientManager = clientManager;
    private readonly ILogger<SetClientMuteCommandHandler> _logger = logger;

    [LoggerMessage(EventId = 112700, Level = LogLevel.Information, Message = "Setting mute for Client {ClientIndex} → {Enabled} from {Source}"
)]
    private partial void LogHandling(int clientIndex, bool enabled, CommandSource source);

    [LoggerMessage(EventId = 112701, Level = LogLevel.Warning, Message = "Client {ClientIndex} not found for SetClientMuteCommand"
)]
    private partial void LogClientNotFound(int clientIndex);

    public async Task<Result> Handle(SetClientMuteCommand request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ClientIndex, request.Enabled, request.Source);

        // Get the client
        var clientResult = await this._clientManager.GetClientAsync(request.ClientIndex).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            this.LogClientNotFound(request.ClientIndex);
            return clientResult;
        }

        var client = clientResult.Value!;

        // Set the mute state
        var result = await client.SetMuteAsync(request.Enabled).ConfigureAwait(false);

        return result;
    }
}
