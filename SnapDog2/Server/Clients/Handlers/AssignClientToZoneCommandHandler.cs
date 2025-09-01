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
/// Handles the AssignClientToZoneCommand.
/// </summary>
public partial class AssignClientToZoneCommandHandler(
    IClientManager clientManager,
    IZoneManager zoneManager,
    ILogger<AssignClientToZoneCommandHandler> logger
) : ICommandHandler<AssignClientToZoneCommand, Result>
{
    private readonly IClientManager _clientManager = clientManager;
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<AssignClientToZoneCommandHandler> _logger = logger;

    [LoggerMessage(
        EventId = 10300,
        Level = LogLevel.Information,
        Message = "Assigning Client {ClientIndex} to Zone {ZoneIndex} from {Source}"
    )]
    private partial void LogHandling(int clientIndex, int zoneIndex, CommandSource source);

    [LoggerMessage(
        EventId = 10301,
        Level = LogLevel.Warning,
        Message = "Client {ClientIndex} not found for AssignClientToZoneCommand"
    )]
    private partial void LogClientNotFound(int clientIndex);

    [LoggerMessage(
        EventId = 10302,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for AssignClientToZoneCommand"
    )]
    private partial void LogZoneNotFound(int zoneIndex);

    public async Task<Result> Handle(AssignClientToZoneCommand request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ClientIndex, request.ZoneIndex, request.Source);

        // Get the current client state to capture the previous zone assignment
        var clientStateResult = await this
            ._clientManager.GetClientAsync(request.ClientIndex)
            .ConfigureAwait(false);
        if (clientStateResult.IsFailure)
        {
            this.LogClientNotFound(request.ClientIndex);
            return clientStateResult;
        }

        // Validate client exists (get IClient for the assignment operation)
        var clientResult = await this._clientManager.GetClientAsync(request.ClientIndex).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            this.LogClientNotFound(request.ClientIndex);
            return clientResult;
        }

        var client = clientResult.Value!;

        // Validate zone exists
        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex);
            return zoneResult;
        }

        // Use the IClient method directly
        var result = await client.AssignToZoneAsync(request.ZoneIndex).ConfigureAwait(false);

        // ✅ Command-Status Flow Pattern: Only instruct external system
        // External system will send notification → event handler → storage → integrations
        // Note: Initially return 200 OK to avoid breaking changes
        // Will switch to 202 Accepted in Phase 3 (Week 5)
        return result;
    }
}
