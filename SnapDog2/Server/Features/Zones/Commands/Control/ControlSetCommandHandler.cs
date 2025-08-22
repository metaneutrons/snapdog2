namespace SnapDog2.Server.Features.Zones.Commands.Control;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Handles the ControlSetCommand for unified zone control operations.
/// </summary>
public partial class ControlSetCommandHandler(IZoneManager zoneManager, ILogger<ControlSetCommandHandler> logger)
    : ICommandHandler<ControlSetCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<ControlSetCommandHandler> _logger = logger;

    [LoggerMessage(
        4001,
        LogLevel.Information,
        "Executing control command '{Command}' for Zone {ZoneIndex} from {Source}"
    )]
    private partial void LogHandling(string command, int zoneIndex, CommandSource source);

    [LoggerMessage(4002, LogLevel.Warning, "Zone {ZoneIndex} not found for ControlSetCommand")]
    private partial void LogZoneNotFound(int zoneIndex);

    [LoggerMessage(4003, LogLevel.Warning, "Unknown control command '{Command}' for Zone {ZoneIndex}")]
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
            _ => HandleUnknownCommand(request.Command, request.ZoneIndex),
        };
    }

    private Result HandleUnknownCommand(string command, int zoneIndex)
    {
        this.LogUnknownCommand(command, zoneIndex);
        return Result.Failure($"Unknown control command: {command}");
    }
}
