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
using SnapDog2.Server.Clients.Commands.Config;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// Handles the SetClientLatencyCommand.
/// </summary>
public partial class SetClientLatencyCommandHandler(
    IClientManager clientManager,
    ILogger<SetClientLatencyCommandHandler> logger
) : ICommandHandler<SetClientLatencyCommand, Result>
{
    private readonly IClientManager _clientManager = clientManager;
    private readonly ILogger<SetClientLatencyCommandHandler> _logger = logger;

    [LoggerMessage(EventId = 112600, Level = LogLevel.Information, Message = "Setting latency for Client {ClientIndex} → {LatencyMs}ms from {Source}"
)]
    private partial void LogHandling(int clientIndex, int latencyMs, CommandSource source);

    [LoggerMessage(EventId = 112601, Level = LogLevel.Warning, Message = "Client {ClientIndex} not found for SetClientLatencyCommand"
)]
    private partial void LogClientNotFound(int clientIndex);

    public async Task<Result> Handle(SetClientLatencyCommand request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ClientIndex, request.LatencyMs, request.Source);

        // Get the client
        var clientResult = await this._clientManager.GetClientAsync(request.ClientIndex).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            this.LogClientNotFound(request.ClientIndex);
            return clientResult;
        }

        var client = clientResult.Value!;

        // Set the latency
        var result = await client.SetLatencyAsync(request.LatencyMs).ConfigureAwait(false);

        return result;
    }
}
