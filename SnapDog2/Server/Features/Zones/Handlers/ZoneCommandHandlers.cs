namespace SnapDog2.Server.Features.Zones.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Commands.Playback;
using SnapDog2.Server.Features.Zones.Commands.Playlist;
using SnapDog2.Server.Features.Zones.Commands.Track;
using SnapDog2.Server.Features.Zones.Commands.Volume;

/// <summary>
/// Handles the PlayCommand.
/// </summary>
public partial class PlayCommandHandler : ICommandHandler<PlayCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<PlayCommandHandler> _logger;

    public PlayCommandHandler(IZoneManager zoneManager, ILogger<PlayCommandHandler> logger)
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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

        if (request.TrackIndex.HasValue)
        {
            return await zone.PlayTrackAsync(request.TrackIndex.Value).ConfigureAwait(false);
        }
        else if (!string.IsNullOrEmpty(request.MediaUrl))
        {
            return await zone.PlayUrlAsync(request.MediaUrl).ConfigureAwait(false);
        }
        else
        {
            return await zone.PlayAsync().ConfigureAwait(false);
        }
    }

    [LoggerMessage(9001, LogLevel.Information, "Starting playback for Zone {ZoneIndex} from {Source}")]
    private partial void LogStartingPlayback(int zoneIndex, SnapDog2.Core.Enums.CommandSource source);

    [LoggerMessage(9002, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the PauseCommand.
/// </summary>
public partial class PauseCommandHandler : ICommandHandler<PauseCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<PauseCommandHandler> _logger;

    public PauseCommandHandler(IZoneManager zoneManager, ILogger<PauseCommandHandler> logger)
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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
        return await zone.PauseAsync().ConfigureAwait(false);
    }

    [LoggerMessage(9003, LogLevel.Information, "Pausing playback for Zone {ZoneIndex} from {Source}")]
    private partial void LogPausingPlayback(int zoneIndex, SnapDog2.Core.Enums.CommandSource source);

    [LoggerMessage(9004, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the StopCommand.
/// </summary>
public partial class StopCommandHandler : ICommandHandler<StopCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<StopCommandHandler> _logger;

    public StopCommandHandler(IZoneManager zoneManager, ILogger<StopCommandHandler> logger)
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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

    [LoggerMessage(9005, LogLevel.Information, "Stopping playback for Zone {ZoneIndex} from {Source}")]
    private partial void LogStoppingPlayback(int zoneIndex, SnapDog2.Core.Enums.CommandSource source);

    [LoggerMessage(9006, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the SetZoneVolumeCommand.
/// </summary>
public partial class SetZoneVolumeCommandHandler : ICommandHandler<SetZoneVolumeCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<SetZoneVolumeCommandHandler> _logger;

    public SetZoneVolumeCommandHandler(IZoneManager zoneManager, ILogger<SetZoneVolumeCommandHandler> logger)
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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

    [LoggerMessage(9007, LogLevel.Information, "Setting volume for Zone {ZoneIndex} to {Volume} from {Source}")]
    private partial void LogSettingVolume(int zoneIndex, int volume, SnapDog2.Core.Enums.CommandSource source);

    [LoggerMessage(9008, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the VolumeUpCommand.
/// </summary>
public partial class VolumeUpCommandHandler : ICommandHandler<VolumeUpCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<VolumeUpCommandHandler> _logger;

    public VolumeUpCommandHandler(IZoneManager zoneManager, ILogger<VolumeUpCommandHandler> logger)
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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

    [LoggerMessage(9009, LogLevel.Information, "Increasing volume for Zone {ZoneIndex} by {Step} from {Source}")]
    private partial void LogIncreasingVolume(int zoneIndex, int step, SnapDog2.Core.Enums.CommandSource source);

    [LoggerMessage(9010, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the VolumeDownCommand.
/// </summary>
public partial class VolumeDownCommandHandler : ICommandHandler<VolumeDownCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<VolumeDownCommandHandler> _logger;

    public VolumeDownCommandHandler(IZoneManager zoneManager, ILogger<VolumeDownCommandHandler> logger)
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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

    [LoggerMessage(9011, LogLevel.Information, "Decreasing volume for Zone {ZoneIndex} by {Step} from {Source}")]
    private partial void LogDecreasingVolume(int zoneIndex, int step, SnapDog2.Core.Enums.CommandSource source);

    [LoggerMessage(9012, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the SetZoneMuteCommand.
/// </summary>
public partial class SetZoneMuteCommandHandler : ICommandHandler<SetZoneMuteCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<SetZoneMuteCommandHandler> _logger;

    public SetZoneMuteCommandHandler(IZoneManager zoneManager, ILogger<SetZoneMuteCommandHandler> logger)
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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

    [LoggerMessage(9013, LogLevel.Information, "Setting mute for Zone {ZoneIndex} to {Enabled} from {Source}")]
    private partial void LogSettingMute(int zoneIndex, bool enabled, SnapDog2.Core.Enums.CommandSource source);

    [LoggerMessage(9014, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the ToggleZoneMuteCommand.
/// </summary>
public partial class ToggleZoneMuteCommandHandler : ICommandHandler<ToggleZoneMuteCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<ToggleZoneMuteCommandHandler> _logger;

    public ToggleZoneMuteCommandHandler(IZoneManager zoneManager, ILogger<ToggleZoneMuteCommandHandler> logger)
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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

    [LoggerMessage(9015, LogLevel.Information, "Toggling mute for Zone {ZoneIndex} from {Source}")]
    private partial void LogTogglingMute(int zoneIndex, SnapDog2.Core.Enums.CommandSource source);

    [LoggerMessage(9016, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the SetTrackCommand.
/// </summary>
public partial class SetTrackCommandHandler : ICommandHandler<SetTrackCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<SetTrackCommandHandler> _logger;

    public SetTrackCommandHandler(IZoneManager zoneManager, ILogger<SetTrackCommandHandler> logger)
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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

    [LoggerMessage(9017, LogLevel.Information, "Setting track for Zone {ZoneIndex} to {TrackIndex} from {Source}")]
    private partial void LogSettingTrack(int zoneIndex, int trackIndex, SnapDog2.Core.Enums.CommandSource source);

    [LoggerMessage(9018, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the NextTrackCommand.
/// </summary>
public partial class NextTrackCommandHandler : ICommandHandler<NextTrackCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<NextTrackCommandHandler> _logger;

    public NextTrackCommandHandler(IZoneManager zoneManager, ILogger<NextTrackCommandHandler> logger)
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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

    [LoggerMessage(9019, LogLevel.Information, "Playing next track for Zone {ZoneIndex} from {Source}")]
    private partial void LogPlayingNextTrack(int zoneIndex, SnapDog2.Core.Enums.CommandSource source);

    [LoggerMessage(9020, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the PreviousTrackCommand.
/// </summary>
public partial class PreviousTrackCommandHandler : ICommandHandler<PreviousTrackCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<PreviousTrackCommandHandler> _logger;

    public PreviousTrackCommandHandler(IZoneManager zoneManager, ILogger<PreviousTrackCommandHandler> logger)
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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

    [LoggerMessage(9021, LogLevel.Information, "Playing previous track for Zone {ZoneIndex} from {Source}")]
    private partial void LogPlayingPreviousTrack(int zoneIndex, SnapDog2.Core.Enums.CommandSource source);

    [LoggerMessage(9022, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the SetPlaylistCommand.
/// </summary>
public partial class SetPlaylistCommandHandler : ICommandHandler<SetPlaylistCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<SetPlaylistCommandHandler> _logger;

    public SetPlaylistCommandHandler(IZoneManager zoneManager, ILogger<SetPlaylistCommandHandler> logger)
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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

    [LoggerMessage(9023, LogLevel.Information, "Setting playlist for Zone {ZoneIndex} from {Source}")]
    private partial void LogSettingPlaylist(int zoneIndex, SnapDog2.Core.Enums.CommandSource source);

    [LoggerMessage(9024, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the NextPlaylistCommand.
/// </summary>
public partial class NextPlaylistCommandHandler : ICommandHandler<NextPlaylistCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<NextPlaylistCommandHandler> _logger;

    public NextPlaylistCommandHandler(IZoneManager zoneManager, ILogger<NextPlaylistCommandHandler> logger)
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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

    [LoggerMessage(9025, LogLevel.Information, "Playing next playlist for Zone {ZoneIndex} from {Source}")]
    private partial void LogPlayingNextPlaylist(int zoneIndex, SnapDog2.Core.Enums.CommandSource source);

    [LoggerMessage(9026, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the PreviousPlaylistCommand.
/// </summary>
public partial class PreviousPlaylistCommandHandler : ICommandHandler<PreviousPlaylistCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<PreviousPlaylistCommandHandler> _logger;

    public PreviousPlaylistCommandHandler(IZoneManager zoneManager, ILogger<PreviousPlaylistCommandHandler> logger)
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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

    [LoggerMessage(9027, LogLevel.Information, "Playing previous playlist for Zone {ZoneIndex} from {Source}")]
    private partial void LogPlayingPreviousPlaylist(int zoneIndex, SnapDog2.Core.Enums.CommandSource source);

    [LoggerMessage(9028, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the SetTrackRepeatCommand.
/// </summary>
public partial class SetTrackRepeatCommandHandler : ICommandHandler<SetTrackRepeatCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<SetTrackRepeatCommandHandler> _logger;

    public SetTrackRepeatCommandHandler(IZoneManager zoneManager, ILogger<SetTrackRepeatCommandHandler> logger)
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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

    [LoggerMessage(9029, LogLevel.Information, "Setting track repeat for Zone {ZoneIndex} to {Enabled} from {Source}")]
    private partial void LogSettingTrackRepeat(int zoneIndex, bool enabled, SnapDog2.Core.Enums.CommandSource source);

    [LoggerMessage(9030, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the ToggleTrackRepeatCommand.
/// </summary>
public partial class ToggleTrackRepeatCommandHandler : ICommandHandler<ToggleTrackRepeatCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<ToggleTrackRepeatCommandHandler> _logger;

    public ToggleTrackRepeatCommandHandler(IZoneManager zoneManager, ILogger<ToggleTrackRepeatCommandHandler> logger)
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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

        this.LogToggleTrackRepeatCompleted(request.ZoneIndex, result.IsSuccess);
        return result;
    }

    [LoggerMessage(9031, LogLevel.Information, "Toggling track repeat for Zone {ZoneIndex} from {Source}")]
    private partial void LogTogglingTrackRepeat(int zoneIndex, SnapDog2.Core.Enums.CommandSource source);

    [LoggerMessage(9032, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);

    [LoggerMessage(
        9033,
        LogLevel.Information,
        "Toggle track repeat completed for Zone {ZoneIndex}, Success: {Success}"
    )]
    private partial void LogToggleTrackRepeatCompleted(int zoneIndex, bool success);
}

/// <summary>
/// Handles the SetPlaylistShuffleCommand.
/// </summary>
public partial class SetPlaylistShuffleCommandHandler : ICommandHandler<SetPlaylistShuffleCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<SetPlaylistShuffleCommandHandler> _logger;

    public SetPlaylistShuffleCommandHandler(IZoneManager zoneManager, ILogger<SetPlaylistShuffleCommandHandler> logger)
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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
        9033,
        LogLevel.Information,
        "Setting playlist shuffle for Zone {ZoneIndex} to {Enabled} from {Source}"
    )]
    private partial void LogSettingPlaylistShuffle(
        int zoneIndex,
        bool enabled,
        SnapDog2.Core.Enums.CommandSource source
    );

    [LoggerMessage(9034, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the TogglePlaylistShuffleCommand.
/// </summary>
public partial class TogglePlaylistShuffleCommandHandler : ICommandHandler<TogglePlaylistShuffleCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<TogglePlaylistShuffleCommandHandler> _logger;

    public TogglePlaylistShuffleCommandHandler(
        IZoneManager zoneManager,
        ILogger<TogglePlaylistShuffleCommandHandler> logger
    )
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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
        return await zone.TogglePlaylistShuffleAsync().ConfigureAwait(false);
    }

    [LoggerMessage(9035, LogLevel.Information, "Toggling playlist shuffle for Zone {ZoneIndex} from {Source}")]
    private partial void LogTogglingPlaylistShuffle(int zoneIndex, SnapDog2.Core.Enums.CommandSource source);

    [LoggerMessage(9036, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the SetPlaylistRepeatCommand.
/// </summary>
public partial class SetPlaylistRepeatCommandHandler : ICommandHandler<SetPlaylistRepeatCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<SetPlaylistRepeatCommandHandler> _logger;

    public SetPlaylistRepeatCommandHandler(IZoneManager zoneManager, ILogger<SetPlaylistRepeatCommandHandler> logger)
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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
        9037,
        LogLevel.Information,
        "Setting playlist repeat for Zone {ZoneIndex} to {Enabled} from {Source}"
    )]
    private partial void LogSettingPlaylistRepeat(
        int zoneIndex,
        bool enabled,
        SnapDog2.Core.Enums.CommandSource source
    );

    [LoggerMessage(9038, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}

/// <summary>
/// Handles the TogglePlaylistRepeatCommand.
/// </summary>
public partial class TogglePlaylistRepeatCommandHandler : ICommandHandler<TogglePlaylistRepeatCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<TogglePlaylistRepeatCommandHandler> _logger;

    public TogglePlaylistRepeatCommandHandler(
        IZoneManager zoneManager,
        ILogger<TogglePlaylistRepeatCommandHandler> logger
    )
    {
        this._zoneManager = zoneManager;
        this._logger = logger;
    }

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
        return await zone.TogglePlaylistRepeatAsync().ConfigureAwait(false);
    }

    [LoggerMessage(9039, LogLevel.Information, "Toggling playlist repeat for Zone {ZoneIndex} from {Source}")]
    private partial void LogTogglingPlaylistRepeat(int zoneIndex, SnapDog2.Core.Enums.CommandSource source);

    [LoggerMessage(9040, LogLevel.Warning, "Zone {ZoneIndex} not found for {CommandName}")]
    private partial void LogZoneNotFound(int zoneIndex, string commandName);
}
