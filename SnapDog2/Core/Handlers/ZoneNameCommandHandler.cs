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
namespace SnapDog2.Core.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Commands;
using SnapDog2.Core.Models;

/// <summary>
/// Handles the ZoneNameCommand for setting zone names.
/// </summary>
public partial class ZoneNameCommandHandler(IZoneManager zoneManager, ILogger<ZoneNameCommandHandler> logger)
    : ICommandHandler<ZoneNameCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<ZoneNameCommandHandler> _logger = logger;

    [LoggerMessage(
        EventId = 8000,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Setting name for Zone {ZoneIndex} to '{Name}'"
    )]
    private partial void LogHandling(int zoneIndex, string name);

    [LoggerMessage(
        EventId = 8001,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for ZoneNameCommand"
    )]
    private partial void LogZoneNotFound(int zoneIndex);

    [LoggerMessage(
        EventId = 8002,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Zone name setting not yet implemented for Zone {ZoneIndex}"
    )]
    private partial void LogNotImplemented(int zoneIndex);

    public async Task<Result> Handle(ZoneNameCommand request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ZoneIndex, request.Name);

        // Get the zone
        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex);
            return zoneResult;
        }

        var zone = zoneResult.Value!;

        // TODO: Implement zone name setting when IZoneService supports it
        this.LogNotImplemented(request.ZoneIndex);

        // For now, return success as a placeholder
        return Result.Success();
    }
}
