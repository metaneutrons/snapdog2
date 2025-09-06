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
/// Handles the SetClientVolumeCommand.
/// </summary>
public partial class SetClientVolumeCommandHandler(
    IClientManager clientManager,
    ILogger<SetClientVolumeCommandHandler> logger
) : ICommandHandler<SetClientVolumeCommand, Result>
{
    private readonly IClientManager _clientManager = clientManager;
    private readonly ILogger<SetClientVolumeCommandHandler> _logger = logger;

    [LoggerMessage(EventId = 112900, Level = LogLevel.Information, Message = "Setting volume for Client {ClientIndex} → {Volume} from {Source}"
)]
    private partial void LogHandling(int clientIndex, int volume, CommandSource source);

    [LoggerMessage(EventId = 112901, Level = LogLevel.Warning, Message = "Client {ClientIndex} not found for SetClientVolumeCommand"
)]
    private partial void LogClientNotFound(int clientIndex);

    public async Task<Result> Handle(SetClientVolumeCommand request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ClientIndex, request.Volume, request.Source);

        // Get the client
        var clientResult = await this._clientManager.GetClientAsync(request.ClientIndex).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            this.LogClientNotFound(request.ClientIndex);
            return clientResult;
        }

        var client = clientResult.Value!;

        // ✅ Command-Status Flow Pattern: Only instruct external system
        // External system will send notification → event handler → storage → integrations
        var result = await client.SetVolumeAsync(request.Volume).ConfigureAwait(false);

        // Note: Initially return 200 OK to avoid breaking changes
        // Will switch to 202 Accepted in Phase 3 (Week 5)
        return result;
    }
}
