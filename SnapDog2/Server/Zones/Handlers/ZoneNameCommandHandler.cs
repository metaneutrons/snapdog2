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

using Cortex.Mediator.Commands;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Zones.Commands;
using SnapDog2.Shared.Models;

namespace SnapDog2.Server.Zones.Handlers;

/// <summary>
/// Handles the ZoneNameCommand for setting zone names.
/// </summary>
public partial class ZoneNameCommandHandler(IZoneManager zoneManager, ILogger<ZoneNameCommandHandler> logger)
    : ICommandHandler<ZoneNameCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<ZoneNameCommandHandler> _logger = logger;

    /// <summary>
    /// Handles the ZoneNameCommand asynchronously.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with a Result.</returns>
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Handling zone name command for zone {ZoneIndex} with name '{Name}'")]
    private partial void LogHandling(int ZoneIndex, string Name);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Zone {ZoneIndex} not found")]
    private partial void LogZoneNotFound(int ZoneIndex);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Zone name setting not implemented for zone {ZoneIndex}")]
    private partial void LogNotImplemented(int ZoneIndex);

    public async Task<Result> Handle(ZoneNameCommand command, CancellationToken cancellationToken = default)
    {
        this.LogHandling(command.ZoneIndex, command.Name);

        // TODO: Implement zone name setting logic
        this.LogNotImplemented(command.ZoneIndex);

        return await Task.FromResult(Result.Success());
    }
}
