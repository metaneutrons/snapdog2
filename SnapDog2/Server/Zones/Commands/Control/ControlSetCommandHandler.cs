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
namespace SnapDog2.Server.Zones.Commands.Control;

using Cortex.Mediator.Commands;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// Handles the ControlSetCommand for unified zone control operations.
/// </summary>
public partial class ControlSetCommandHandler(IZoneManager zoneManager, ILogger<ControlSetCommandHandler> logger)
    : ICommandHandler<ControlSetCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<ControlSetCommandHandler> _logger = logger;

    [LoggerMessage(
        EventId = 9900,
        Level = LogLevel.Information,
        Message = "Executing control command '{Command}' for Zone {ZoneIndex} from {Source}"
    )]
    private partial void LogHandling(string command, int zoneIndex, CommandSource source);

    [LoggerMessage(
        EventId = 9901,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for ControlSetCommand"
    )]
    private partial void LogZoneNotFound(int zoneIndex);

    [LoggerMessage(
        EventId = 9902,
        Level = LogLevel.Warning,
        Message = "Unknown control command '{Command}' for Zone {ZoneIndex}"
    )]
    private partial void LogUnknownCommand(string command, int zoneIndex);

    public async Task<Result> Handle(ControlSetCommand request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.Command, request.ZoneIndex, request.Source);

        // Get the zone
        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex);
            return zoneResult;
        }

        var zone = zoneResult.Value!;

        // Execute the control command based on the command string
        return request.Command.ToLowerInvariant() switch
        {
            "play" => await zone.PlayAsync().ConfigureAwait(false),
            "pause" => await zone.PauseAsync().ConfigureAwait(false),
            "stop" => await zone.StopAsync().ConfigureAwait(false),
            "next" => await zone.NextTrackAsync().ConfigureAwait(false),
            "previous" => await zone.PreviousTrackAsync().ConfigureAwait(false),
            "shuffle_on" => await zone.SetPlaylistShuffleAsync(true).ConfigureAwait(false),
            "shuffle_off" => await zone.SetPlaylistShuffleAsync(false).ConfigureAwait(false),
            "repeat_on" => await zone.SetPlaylistRepeatAsync(true).ConfigureAwait(false),
            "repeat_off" => await zone.SetPlaylistRepeatAsync(false).ConfigureAwait(false),
            "mute_on" => await zone.SetMuteAsync(true).ConfigureAwait(false),
            "mute_off" => await zone.SetMuteAsync(false).ConfigureAwait(false),
            _ => this.HandleUnknownCommand(request.Command, request.ZoneIndex),
        };
    }

    private Result HandleUnknownCommand(string command, int zoneIndex)
    {
        this.LogUnknownCommand(command, zoneIndex);
        return Result.Failure($"Unknown control command: {command}");
    }
}
