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
/// Handles the SetClientVolumeCommand.
/// </summary>
public partial class SetClientVolumeCommandHandler(
    IClientManager clientManager,
    ILogger<SetClientVolumeCommandHandler> logger
) : ICommandHandler<SetClientVolumeCommand, Result>
{
    private readonly IClientManager _clientManager = clientManager;
    private readonly ILogger<SetClientVolumeCommandHandler> _logger = logger;

    [LoggerMessage(3001, LogLevel.Information, "Setting volume for Client {ClientIndex} to {Volume} from {Source}")]
    private partial void LogHandling(int clientIndex, int volume, CommandSource source);

    [LoggerMessage(3002, LogLevel.Warning, "Client {ClientIndex} not found for SetClientVolumeCommand")]
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

        // Set the volume
        var result = await client.SetVolumeAsync(request.Volume).ConfigureAwait(false);

        return result;
    }
}
