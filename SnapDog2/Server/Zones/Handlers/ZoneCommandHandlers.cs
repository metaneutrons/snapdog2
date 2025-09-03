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

using Cortex.Mediator;
using Cortex.Mediator.Commands;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Zones.Commands.Playback;
using SnapDog2.Server.Zones.Commands.Playlist;
using SnapDog2.Server.Zones.Commands.Track;
using SnapDog2.Server.Zones.Commands.Volume;
using SnapDog2.Server.Zones.Notifications;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// Handles the PlayCommand.
/// </summary>
public partial class PlayCommandHandler(IZoneManager zoneManager, ILogger<PlayCommandHandler> logger, IMediator mediator)
    : ICommandHandler<PlayCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<PlayCommandHandler> _logger = logger;
    private readonly IMediator _mediator = mediator;

    public async Task<Result> Handle(PlayCommand request, CancellationToken cancellationToken)
    {
        this.LogStartingPlayback(request.ZoneIndex, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(PlayCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        Result result;

        if (request.TrackIndex.HasValue)
        {
            result = await zone.PlayTrackAsync(request.TrackIndex.Value).ConfigureAwait(false);
        }
        else if (!string.IsNullOrEmpty(request.MediaUrl))
        {
            result = await zone.PlayUrlAsync(request.MediaUrl).ConfigureAwait(false);
        }
        else
        {
            result = await zone.PlayAsync().ConfigureAwait(false);
        }

        if (result.IsSuccess)
        {
            await _mediator.PublishAsync(new ZonePlaybackStateChangedNotification
            {
                ZoneIndex = request.ZoneIndex,
                PlaybackState = PlaybackState.Playing
            }, cancellationToken);
        }

        return result;
    }

    [LoggerMessage(
        EventId = 12900,
        Level = LogLevel.Information,
        Message = "Starting playback for Zone {ZoneIndex} from {Source}"
    )]
    private partial void LogStartingPlayback(int zoneIndex, CommandSource source);

    [LoggerMessage(
        EventId = 12901,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the PauseCommand.
/// </summary>
public partial class PauseCommandHandler(IZoneManager zoneManager, ILogger<PauseCommandHandler> logger, IMediator mediator)
    : ICommandHandler<PauseCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<PauseCommandHandler> _logger = logger;
    private readonly IMediator _mediator = mediator;

    public async Task<Result> Handle(PauseCommand request, CancellationToken cancellationToken)
    {
        this.LogPausingPlayback(request.ZoneIndex, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(PauseCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        var result = await zone.PauseAsync().ConfigureAwait(false);

        if (result.IsSuccess)
        {
            await _mediator.PublishAsync(new ZonePlaybackStateChangedNotification
            {
                ZoneIndex = request.ZoneIndex,
                PlaybackState = PlaybackState.Paused
            }, cancellationToken);
        }

        return result;
    }

    [LoggerMessage(
        EventId = 12902,
        Level = LogLevel.Information,
        Message = "Pausing playback for Zone {ZoneIndex} from {Source}"
    )]
    private partial void LogPausingPlayback(int zoneIndex, CommandSource source);

    [LoggerMessage(
        EventId = 12903,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the StopCommand.
/// </summary>
public partial class StopCommandHandler(IZoneManager zoneManager, ILogger<StopCommandHandler> logger)
    : ICommandHandler<StopCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<StopCommandHandler> _logger = logger;

    public async Task<Result> Handle(StopCommand request, CancellationToken cancellationToken)
    {
        this.LogStoppingPlayback(request.ZoneIndex, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(StopCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.StopAsync().ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12904,
        Level = LogLevel.Information,
        Message = "Stopping playback for Zone {ZoneIndex} from {Source}"
    )]
    private partial void LogStoppingPlayback(int zoneIndex, CommandSource source);

    [LoggerMessage(
        EventId = 12905,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the SetZoneVolumeCommand.
/// </summary>
public partial class SetZoneVolumeCommandHandler(IZoneManager zoneManager, ILogger<SetZoneVolumeCommandHandler> logger)
    : ICommandHandler<SetZoneVolumeCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<SetZoneVolumeCommandHandler> _logger = logger;

    public async Task<Result> Handle(SetZoneVolumeCommand request, CancellationToken cancellationToken)
    {
        this.LogSettingVolume(request.ZoneIndex, request.Volume, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(SetZoneVolumeCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.SetVolumeAsync(request.Volume).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12906,
        Level = LogLevel.Information,
        Message = "Setting volume for Zone {ZoneIndex} to {Volume} from {Source}"
    )]
    private partial void LogSettingVolume(int zoneIndex, int volume, CommandSource source);

    [LoggerMessage(
        EventId = 12907,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the VolumeUpCommand.
/// </summary>
public partial class VolumeUpCommandHandler(IZoneManager zoneManager, ILogger<VolumeUpCommandHandler> logger)
    : ICommandHandler<VolumeUpCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<VolumeUpCommandHandler> _logger = logger;

    public async Task<Result> Handle(VolumeUpCommand request, CancellationToken cancellationToken)
    {
        this.LogIncreasingVolume(request.ZoneIndex, request.Step, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(VolumeUpCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.VolumeUpAsync(request.Step).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12908,
        Level = LogLevel.Information,
        Message = "Increasing volume for Zone {ZoneIndex} by {Step} from {Source}"
    )]
    private partial void LogIncreasingVolume(int zoneIndex, int step, CommandSource source);

    [LoggerMessage(
        EventId = 12909,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the VolumeDownCommand.
/// </summary>
public partial class VolumeDownCommandHandler(IZoneManager zoneManager, ILogger<VolumeDownCommandHandler> logger)
    : ICommandHandler<VolumeDownCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<VolumeDownCommandHandler> _logger = logger;

    public async Task<Result> Handle(VolumeDownCommand request, CancellationToken cancellationToken)
    {
        this.LogDecreasingVolume(request.ZoneIndex, request.Step, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(VolumeDownCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.VolumeDownAsync(request.Step).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12910,
        Level = LogLevel.Information,
        Message = "Decreasing volume for Zone {ZoneIndex} by {Step} from {Source}"
    )]
    private partial void LogDecreasingVolume(int zoneIndex, int step, CommandSource source);

    [LoggerMessage(
        EventId = 12911,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the SetZoneMuteCommand.
/// </summary>
public partial class SetZoneMuteCommandHandler(IZoneManager zoneManager, ILogger<SetZoneMuteCommandHandler> logger)
    : ICommandHandler<SetZoneMuteCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<SetZoneMuteCommandHandler> _logger = logger;

    public async Task<Result> Handle(SetZoneMuteCommand request, CancellationToken cancellationToken)
    {
        this.LogSettingMute(request.ZoneIndex, request.Enabled, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(SetZoneMuteCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.SetMuteAsync(request.Enabled).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12912,
        Level = LogLevel.Information,
        Message = "Setting mute for Zone {ZoneIndex} to {Enabled} from {Source}"
    )]
    private partial void LogSettingMute(int zoneIndex, bool enabled, CommandSource source);

    [LoggerMessage(
        EventId = 12913,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the ToggleZoneMuteCommand.
/// </summary>
public partial class ToggleZoneMuteCommandHandler(
    IZoneManager zoneManager,
    ILogger<ToggleZoneMuteCommandHandler> logger
) : ICommandHandler<ToggleZoneMuteCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<ToggleZoneMuteCommandHandler> _logger = logger;

    public async Task<Result> Handle(ToggleZoneMuteCommand request, CancellationToken cancellationToken)
    {
        this.LogTogglingMute(request.ZoneIndex, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(ToggleZoneMuteCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.ToggleMuteAsync().ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12914,
        Level = LogLevel.Information,
        Message = "Toggling mute for Zone {ZoneIndex} from {Source}"
    )]
    private partial void LogTogglingMute(int zoneIndex, CommandSource source);

    [LoggerMessage(
        EventId = 12915,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the SetTrackCommand.
/// </summary>
public partial class SetTrackCommandHandler(IZoneManager zoneManager, ILogger<SetTrackCommandHandler> logger)
    : ICommandHandler<SetTrackCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<SetTrackCommandHandler> _logger = logger;

    public async Task<Result> Handle(SetTrackCommand request, CancellationToken cancellationToken)
    {
        this.LogSettingTrack(request.ZoneIndex, request.TrackIndex, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(SetTrackCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.SetTrackAsync(request.TrackIndex).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12916,
        Level = LogLevel.Information,
        Message = "Setting track for Zone {ZoneIndex} to {TrackIndex} from {Source}"
    )]
    private partial void LogSettingTrack(int zoneIndex, int trackIndex, CommandSource source);

    [LoggerMessage(
        EventId = 12917,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the NextTrackCommand.
/// </summary>
public partial class NextTrackCommandHandler(IZoneManager zoneManager, ILogger<NextTrackCommandHandler> logger)
    : ICommandHandler<NextTrackCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<NextTrackCommandHandler> _logger = logger;

    public async Task<Result> Handle(NextTrackCommand request, CancellationToken cancellationToken)
    {
        this.LogPlayingNextTrack(request.ZoneIndex, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(NextTrackCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.NextTrackAsync().ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12918,
        Level = LogLevel.Information,
        Message = "Playing next track for Zone {ZoneIndex} from {Source}"
    )]
    private partial void LogPlayingNextTrack(int zoneIndex, CommandSource source);

    [LoggerMessage(
        EventId = 12919,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the PreviousTrackCommand.
/// </summary>
public partial class PreviousTrackCommandHandler(IZoneManager zoneManager, ILogger<PreviousTrackCommandHandler> logger)
    : ICommandHandler<PreviousTrackCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<PreviousTrackCommandHandler> _logger = logger;

    public async Task<Result> Handle(PreviousTrackCommand request, CancellationToken cancellationToken)
    {
        this.LogPlayingPreviousTrack(request.ZoneIndex, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(PreviousTrackCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.PreviousTrackAsync().ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12920,
        Level = LogLevel.Information,
        Message = "Playing previous track for Zone {ZoneIndex} from {Source}"
    )]
    private partial void LogPlayingPreviousTrack(int zoneIndex, CommandSource source);

    [LoggerMessage(
        EventId = 12921,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the SetPlaylistCommand.
/// </summary>
public partial class SetPlaylistCommandHandler(IZoneManager zoneManager, ILogger<SetPlaylistCommandHandler> logger)
    : ICommandHandler<SetPlaylistCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<SetPlaylistCommandHandler> _logger = logger;

    public async Task<Result> Handle(SetPlaylistCommand request, CancellationToken cancellationToken)
    {
        this.LogSettingPlaylist(request.ZoneIndex, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(SetPlaylistCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;

        return await zone.SetPlaylistAsync(request.PlaylistIndex).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12922,
        Level = LogLevel.Information,
        Message = "Setting playlist for Zone {ZoneIndex} from {Source}"
    )]
    private partial void LogSettingPlaylist(int zoneIndex, CommandSource source);

    [LoggerMessage(
        EventId = 12923,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the NextPlaylistCommand.
/// </summary>
public partial class NextPlaylistCommandHandler(IZoneManager zoneManager, ILogger<NextPlaylistCommandHandler> logger)
    : ICommandHandler<NextPlaylistCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<NextPlaylistCommandHandler> _logger = logger;

    public async Task<Result> Handle(NextPlaylistCommand request, CancellationToken cancellationToken)
    {
        this.LogPlayingNextPlaylist(request.ZoneIndex, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(NextPlaylistCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.NextPlaylistAsync().ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12924,
        Level = LogLevel.Information,
        Message = "Playing next playlist for Zone {ZoneIndex} from {Source}"
    )]
    private partial void LogPlayingNextPlaylist(int zoneIndex, CommandSource source);

    [LoggerMessage(
        EventId = 12925,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the PreviousPlaylistCommand.
/// </summary>
public partial class PreviousPlaylistCommandHandler(
    IZoneManager zoneManager,
    ILogger<PreviousPlaylistCommandHandler> logger
) : ICommandHandler<PreviousPlaylistCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<PreviousPlaylistCommandHandler> _logger = logger;

    public async Task<Result> Handle(PreviousPlaylistCommand request, CancellationToken cancellationToken)
    {
        this.LogPlayingPreviousPlaylist(request.ZoneIndex, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(PreviousPlaylistCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.PreviousPlaylistAsync().ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12926,
        Level = LogLevel.Information,
        Message = "Playing previous playlist for Zone {ZoneIndex} from {Source}"
    )]
    private partial void LogPlayingPreviousPlaylist(int zoneIndex, CommandSource source);

    [LoggerMessage(
        EventId = 12927,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the SetTrackRepeatCommand.
/// </summary>
public partial class SetTrackRepeatCommandHandler(
    IZoneManager zoneManager,
    ILogger<SetTrackRepeatCommandHandler> logger
) : ICommandHandler<SetTrackRepeatCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<SetTrackRepeatCommandHandler> _logger = logger;

    public async Task<Result> Handle(SetTrackRepeatCommand request, CancellationToken cancellationToken)
    {
        this.LogSettingTrackRepeat(request.ZoneIndex, request.Enabled, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(SetTrackRepeatCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.SetTrackRepeatAsync(request.Enabled).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12928,
        Level = LogLevel.Information,
        Message = "Setting track repeat for Zone {ZoneIndex} to {Enabled} from {Source}"
    )]
    private partial void LogSettingTrackRepeat(int zoneIndex, bool enabled, CommandSource source);

    [LoggerMessage(
        EventId = 12929,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the ToggleTrackRepeatCommand.
/// </summary>
public partial class ToggleTrackRepeatCommandHandler(
    IZoneManager zoneManager,
    ILogger<ToggleTrackRepeatCommandHandler> logger,
    IMediator mediator
) : ICommandHandler<ToggleTrackRepeatCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<ToggleTrackRepeatCommandHandler> _logger = logger;
    private readonly IMediator _mediator = mediator;

    public async Task<Result> Handle(ToggleTrackRepeatCommand request, CancellationToken cancellationToken)
    {
        this.LogTogglingTrackRepeat(request.ZoneIndex, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(ToggleTrackRepeatCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        var result = await zone.ToggleTrackRepeatAsync().ConfigureAwait(false);

        if (result.IsSuccess)
        {
            var stateResult = await this._zoneManager.GetZoneStateAsync(request.ZoneIndex).ConfigureAwait(false);
            if (stateResult.IsSuccess)
            {
                await _mediator.PublishAsync(new ZoneTrackRepeatChangedNotification
                {
                    ZoneIndex = request.ZoneIndex,
                    Enabled = stateResult.Value!.TrackRepeat
                }, cancellationToken);
            }
        }

        return result;
    }

    [LoggerMessage(
        EventId = 12930,
        Level = LogLevel.Information,
        Message = "Toggling track repeat for Zone {ZoneIndex} from {Source}"
    )]
    private partial void LogTogglingTrackRepeat(int zoneIndex, CommandSource source);

    [LoggerMessage(
        EventId = 12931,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the SetPlaylistShuffleCommand.
/// </summary>
public partial class SetPlaylistShuffleCommandHandler(
    IZoneManager zoneManager,
    ILogger<SetPlaylistShuffleCommandHandler> logger
) : ICommandHandler<SetPlaylistShuffleCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<SetPlaylistShuffleCommandHandler> _logger = logger;

    public async Task<Result> Handle(SetPlaylistShuffleCommand request, CancellationToken cancellationToken)
    {
        this.LogSettingPlaylistShuffle(request.ZoneIndex, request.Enabled, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(SetPlaylistShuffleCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.SetPlaylistShuffleAsync(request.Enabled).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12932,
        Level = LogLevel.Information,
        Message = "Setting playlist shuffle for Zone {ZoneIndex} to {Enabled} from {Source}"
    )]
    private partial void LogSettingPlaylistShuffle(
        int zoneIndex,
        bool enabled,
        CommandSource source
    );

    [LoggerMessage(
        EventId = 12933,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the TogglePlaylistShuffleCommand.
/// </summary>
public partial class TogglePlaylistShuffleCommandHandler(
    IZoneManager zoneManager,
    ILogger<TogglePlaylistShuffleCommandHandler> logger,
    IMediator mediator
) : ICommandHandler<TogglePlaylistShuffleCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<TogglePlaylistShuffleCommandHandler> _logger = logger;
    private readonly IMediator _mediator = mediator;

    public async Task<Result> Handle(TogglePlaylistShuffleCommand request, CancellationToken cancellationToken)
    {
        this.LogTogglingPlaylistShuffle(request.ZoneIndex, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(TogglePlaylistShuffleCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        var result = await zone.TogglePlaylistShuffleAsync().ConfigureAwait(false);

        if (result.IsSuccess)
        {
            var stateResult = await this._zoneManager.GetZoneStateAsync(request.ZoneIndex).ConfigureAwait(false);
            if (stateResult.IsSuccess)
            {
                await _mediator.PublishAsync(new ZoneShuffleModeChangedNotification
                {
                    ZoneIndex = request.ZoneIndex,
                    ShuffleEnabled = stateResult.Value!.PlaylistShuffle
                }, cancellationToken);
            }
        }

        return result;
    }

    [LoggerMessage(
        EventId = 12934,
        Level = LogLevel.Information,
        Message = "Toggling playlist shuffle for Zone {ZoneIndex} from {Source}"
    )]
    private partial void LogTogglingPlaylistShuffle(int zoneIndex, CommandSource source);

    [LoggerMessage(
        EventId = 12935,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the SetPlaylistRepeatCommand.
/// </summary>
public partial class SetPlaylistRepeatCommandHandler(
    IZoneManager zoneManager,
    ILogger<SetPlaylistRepeatCommandHandler> logger
) : ICommandHandler<SetPlaylistRepeatCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<SetPlaylistRepeatCommandHandler> _logger = logger;

    public async Task<Result> Handle(SetPlaylistRepeatCommand request, CancellationToken cancellationToken)
    {
        this.LogSettingPlaylistRepeat(request.ZoneIndex, request.Enabled, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(SetPlaylistRepeatCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.SetPlaylistRepeatAsync(request.Enabled).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12936,
        Level = LogLevel.Information,
        Message = "Setting playlist repeat for Zone {ZoneIndex} to {Enabled} from {Source}"
    )]
    private partial void LogSettingPlaylistRepeat(
        int zoneIndex,
        bool enabled,
        CommandSource source
    );

    [LoggerMessage(
        EventId = 12937,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the TogglePlaylistRepeatCommand.
/// </summary>
public partial class TogglePlaylistRepeatCommandHandler(
    IZoneManager zoneManager,
    ILogger<TogglePlaylistRepeatCommandHandler> logger,
    IMediator mediator
) : ICommandHandler<TogglePlaylistRepeatCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<TogglePlaylistRepeatCommandHandler> _logger = logger;
    private readonly IMediator _mediator = mediator;

    public async Task<Result> Handle(TogglePlaylistRepeatCommand request, CancellationToken cancellationToken)
    {
        this.LogTogglingPlaylistRepeat(request.ZoneIndex, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(TogglePlaylistRepeatCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        var result = await zone.TogglePlaylistRepeatAsync().ConfigureAwait(false);

        if (result.IsSuccess)
        {
            var stateResult = await this._zoneManager.GetZoneStateAsync(request.ZoneIndex).ConfigureAwait(false);
            if (stateResult.IsSuccess)
            {
                await _mediator.PublishAsync(new ZonePlaylistRepeatChangedNotification
                {
                    ZoneIndex = request.ZoneIndex,
                    Enabled = stateResult.Value!.PlaylistRepeat
                }, cancellationToken);
            }
        }

        return result;
    }

    [LoggerMessage(
        EventId = 12938,
        Level = LogLevel.Information,
        Message = "Toggling playlist repeat for Zone {ZoneIndex} from {Source}"
    )]
    private partial void LogTogglingPlaylistRepeat(int zoneIndex, CommandSource source);

    [LoggerMessage(
        EventId = 12939,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the SeekPositionCommand.
/// </summary>
public partial class SeekPositionCommandHandler(IZoneManager zoneManager, ILogger<SeekPositionCommandHandler> logger)
    : ICommandHandler<SeekPositionCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<SeekPositionCommandHandler> _logger = logger;

    public async Task<Result> Handle(SeekPositionCommand request, CancellationToken cancellationToken)
    {
        this.LogSeekingToPosition(request.ZoneIndex, request.PositionMs, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(SeekPositionCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.SeekToPositionAsync(request.PositionMs).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12940,
        Level = LogLevel.Information,
        Message = "Seeking Zone {ZoneIndex} to position {PositionMs}ms from {Source}"
    )]
    private partial void LogSeekingToPosition(int zoneIndex, long positionMs, CommandSource source);

    [LoggerMessage(
        EventId = 12941,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the SeekProgressCommand.
/// </summary>
public partial class SeekProgressCommandHandler(IZoneManager zoneManager, ILogger<SeekProgressCommandHandler> logger)
    : ICommandHandler<SeekProgressCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<SeekProgressCommandHandler> _logger = logger;

    public async Task<Result> Handle(SeekProgressCommand request, CancellationToken cancellationToken)
    {
        this.LogSeekingToProgress(request.ZoneIndex, request.Progress, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(SeekProgressCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.SeekToProgressAsync(request.Progress).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12942,
        Level = LogLevel.Information,
        Message = "Seeking Zone {ZoneIndex} to progress {Progress:P1} from {Source}"
    )]
    private partial void LogSeekingToProgress(int zoneIndex, float progress, CommandSource source);

    [LoggerMessage(
        EventId = 12943,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the PlayTrackByIndexCommand.
/// </summary>
public partial class PlayTrackByIndexCommandHandler(
    IZoneManager zoneManager,
    ILogger<PlayTrackByIndexCommandHandler> logger
) : ICommandHandler<PlayTrackByIndexCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<PlayTrackByIndexCommandHandler> _logger = logger;

    public async Task<Result> Handle(PlayTrackByIndexCommand request, CancellationToken cancellationToken)
    {
        this.LogPlayingTrackByIndex(request.ZoneIndex, request.TrackIndex, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(PlayTrackByIndexCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        var setResult = await zone.SetTrackAsync(request.TrackIndex).ConfigureAwait(false);
        if (setResult.IsFailure)
        {
            return setResult;
        }

        return await zone.PlayAsync().ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12944,
        Level = LogLevel.Information,
        Message = "Playing track {TrackIndex} for Zone {ZoneIndex} from {Source}"
    )]
    private partial void LogPlayingTrackByIndex(
        int zoneIndex,
        int trackIndex,
        CommandSource source
    );

    [LoggerMessage(
        EventId = 12945,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the PlayUrlCommand.
/// </summary>
public partial class PlayUrlCommandHandler(IZoneManager zoneManager, ILogger<PlayUrlCommandHandler> logger)
    : ICommandHandler<PlayUrlCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<PlayUrlCommandHandler> _logger = logger;

    public async Task<Result> Handle(PlayUrlCommand request, CancellationToken cancellationToken)
    {
        this.LogPlayingUrl(request.ZoneIndex, request.Url, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(PlayUrlCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.PlayUrlAsync(request.Url).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12946,
        Level = LogLevel.Information,
        Message = "Playing URL '{Url}' for Zone {ZoneIndex} from {Source}"
    )]
    private partial void LogPlayingUrl(int zoneIndex, string url, CommandSource source);

    [LoggerMessage(
        EventId = 12947,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the PlayTrackFromPlaylistCommand.
/// </summary>
public partial class PlayTrackFromPlaylistCommandHandler(
    IZoneManager zoneManager,
    ILogger<PlayTrackFromPlaylistCommandHandler> logger
) : ICommandHandler<PlayTrackFromPlaylistCommand, Result>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<PlayTrackFromPlaylistCommandHandler> _logger = logger;

    public async Task<Result> Handle(PlayTrackFromPlaylistCommand request, CancellationToken cancellationToken)
    {
        this.LogPlayingTrackFromPlaylist(request.ZoneIndex, request.PlaylistIndex, request.TrackIndex, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex, nameof(PlayTrackFromPlaylistCommand));
            return zoneResult;
        }

        var zone = zoneResult.Value!;

        // First set the playlist
        var playlistResult = await zone.SetPlaylistAsync(request.PlaylistIndex).ConfigureAwait(false);
        if (playlistResult.IsFailure)
        {
            return playlistResult;
        }

        // Then set the track within that playlist
        var trackResult = await zone.SetTrackAsync(request.TrackIndex).ConfigureAwait(false);
        if (trackResult.IsFailure)
        {
            return trackResult;
        }

        // Finally start playback
        return await zone.PlayAsync().ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 12948,
        Level = LogLevel.Information,
        Message = "Playing track {TrackIndex} from playlist {PlaylistIndex} for Zone {ZoneIndex} from {Source}"
    )]
    private partial void LogPlayingTrackFromPlaylist(
        int zoneIndex,
        int playlistIndex,
        int trackIndex,
        CommandSource source
    );

    [LoggerMessage(
        EventId = 12949,
        Level = LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found for {CommandName}"
    )]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}
