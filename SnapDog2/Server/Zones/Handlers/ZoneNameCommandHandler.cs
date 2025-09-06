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

namespace SnapDog2.Server.Zones.Handlers;

using Cortex.Mediator.Commands;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Zones.Commands;
using SnapDog2.Shared.Models;

/// <summary>
/// Handles the ZoneNameCommand for setting zone names.
/// </summary>
public partial class ZoneNameCommandHandler(IZoneManager zoneManager, ILogger<ZoneNameCommandHandler> logger)
    : ICommandHandler<ZoneNameCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;

    /// <summary>
    /// Handles the ZoneNameCommand asynchronously.
    /// </summary>
    /// <param>The cancellation token.</param>
    /// <param name="zoneIndex">Zone to be renamed.</param>
    /// <param name="name">Name of the zone.</param>
    /// <returns>A task representing the asynchronous operation with a Result.</returns>
    /// TODO: Why is this never called?
    [LoggerMessage(EventId = 113450, Level = LogLevel.Information, Message = "Handling zone name command for zone {ZoneIndex} with name '{Name}'")]
    private partial void LogHandling(int zoneIndex, string name);

    [LoggerMessage(EventId = 113451, Level = LogLevel.Warning, Message = "Zone {ZoneIndex} not found")]
    private partial void LogZoneNotFound(int zoneIndex);

    [LoggerMessage(EventId = 113452, Level = LogLevel.Warning, Message = "Zone name setting not implemented for zone {ZoneIndex}")]
    private partial void LogNotImplemented(int zoneIndex);

    public async Task<Result> Handle(ZoneNameCommand command, CancellationToken cancellationToken = default)
    {
        this.LogHandling(command.ZoneIndex, command.Name);

        // TODO: Implement zone name setting logic
        this.LogNotImplemented(command.ZoneIndex);

        return await Task.FromResult(Result.Success());
    }
}
