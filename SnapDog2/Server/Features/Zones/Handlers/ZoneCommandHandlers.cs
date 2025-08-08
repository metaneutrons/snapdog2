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
public class PlayCommandHandler : ICommandHandler<PlayCommand, Result>
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
        this._logger.LogInformation(
            "Starting playback for Zone {ZoneId} from {Source}",
            request.ZoneId,
            request.Source
        );

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for PlayCommand", request.ZoneId);
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
}

/// <summary>
/// Handles the PauseCommand.
/// </summary>
public class PauseCommandHandler : ICommandHandler<PauseCommand, Result>
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
        this._logger.LogInformation("Pausing playback for Zone {ZoneId} from {Source}", request.ZoneId, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for PauseCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.PauseAsync().ConfigureAwait(false);
    }
}

/// <summary>
/// Handles the StopCommand.
/// </summary>
public class StopCommandHandler : ICommandHandler<StopCommand, Result>
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
        this._logger.LogInformation(
            "Stopping playback for Zone {ZoneId} from {Source}",
            request.ZoneId,
            request.Source
        );

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for StopCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.StopAsync().ConfigureAwait(false);
    }
}

/// <summary>
/// Handles the SetZoneVolumeCommand.
/// </summary>
public class SetZoneVolumeCommandHandler : ICommandHandler<SetZoneVolumeCommand, Result>
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
        this._logger.LogInformation(
            "Setting volume for Zone {ZoneId} to {Volume} from {Source}",
            request.ZoneId,
            request.Volume,
            request.Source
        );

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for SetZoneVolumeCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.SetVolumeAsync(request.Volume).ConfigureAwait(false);
    }
}

/// <summary>
/// Handles the VolumeUpCommand.
/// </summary>
public class VolumeUpCommandHandler : ICommandHandler<VolumeUpCommand, Result>
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
        this._logger.LogInformation(
            "Increasing volume for Zone {ZoneId} by {Step} from {Source}",
            request.ZoneId,
            request.Step,
            request.Source
        );

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for VolumeUpCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.VolumeUpAsync(request.Step).ConfigureAwait(false);
    }
}

/// <summary>
/// Handles the VolumeDownCommand.
/// </summary>
public class VolumeDownCommandHandler : ICommandHandler<VolumeDownCommand, Result>
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
        this._logger.LogInformation(
            "Decreasing volume for Zone {ZoneId} by {Step} from {Source}",
            request.ZoneId,
            request.Step,
            request.Source
        );

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for VolumeDownCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.VolumeDownAsync(request.Step).ConfigureAwait(false);
    }
}

/// <summary>
/// Handles the SetZoneMuteCommand.
/// </summary>
public class SetZoneMuteCommandHandler : ICommandHandler<SetZoneMuteCommand, Result>
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
        this._logger.LogInformation(
            "Setting mute for Zone {ZoneId} to {Enabled} from {Source}",
            request.ZoneId,
            request.Enabled,
            request.Source
        );

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for SetZoneMuteCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.SetMuteAsync(request.Enabled).ConfigureAwait(false);
    }
}

/// <summary>
/// Handles the ToggleZoneMuteCommand.
/// </summary>
public class ToggleZoneMuteCommandHandler : ICommandHandler<ToggleZoneMuteCommand, Result>
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
        this._logger.LogInformation("Toggling mute for Zone {ZoneId} from {Source}", request.ZoneId, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for ToggleZoneMuteCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.ToggleMuteAsync().ConfigureAwait(false);
    }
}

/// <summary>
/// Handles the SetTrackCommand.
/// </summary>
public class SetTrackCommandHandler : ICommandHandler<SetTrackCommand, Result>
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
        this._logger.LogInformation(
            "Setting track for Zone {ZoneId} to {TrackIndex} from {Source}",
            request.ZoneId,
            request.TrackIndex,
            request.Source
        );

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for SetTrackCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.SetTrackAsync(request.TrackIndex).ConfigureAwait(false);
    }
}

/// <summary>
/// Handles the NextTrackCommand.
/// </summary>
public class NextTrackCommandHandler : ICommandHandler<NextTrackCommand, Result>
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
        this._logger.LogInformation(
            "Playing next track for Zone {ZoneId} from {Source}",
            request.ZoneId,
            request.Source
        );

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for NextTrackCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.NextTrackAsync().ConfigureAwait(false);
    }
}

/// <summary>
/// Handles the PreviousTrackCommand.
/// </summary>
public class PreviousTrackCommandHandler : ICommandHandler<PreviousTrackCommand, Result>
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
        this._logger.LogInformation(
            "Playing previous track for Zone {ZoneId} from {Source}",
            request.ZoneId,
            request.Source
        );

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for PreviousTrackCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.PreviousTrackAsync().ConfigureAwait(false);
    }
}

/// <summary>
/// Handles the SetPlaylistCommand.
/// </summary>
public class SetPlaylistCommandHandler : ICommandHandler<SetPlaylistCommand, Result>
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
        this._logger.LogInformation("Setting playlist for Zone {ZoneId} from {Source}", request.ZoneId, request.Source);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for SetPlaylistCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value!;

        if (request.PlaylistIndex.HasValue)
        {
            return await zone.SetPlaylistAsync(request.PlaylistIndex.Value).ConfigureAwait(false);
        }
        else if (!string.IsNullOrEmpty(request.PlaylistId))
        {
            return await zone.SetPlaylistAsync(request.PlaylistId).ConfigureAwait(false);
        }

        return Result.Failure("Either PlaylistIndex or PlaylistId must be specified");
    }
}

/// <summary>
/// Handles the NextPlaylistCommand.
/// </summary>
public class NextPlaylistCommandHandler : ICommandHandler<NextPlaylistCommand, Result>
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
        this._logger.LogInformation(
            "Playing next playlist for Zone {ZoneId} from {Source}",
            request.ZoneId,
            request.Source
        );

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for NextPlaylistCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.NextPlaylistAsync().ConfigureAwait(false);
    }
}

/// <summary>
/// Handles the PreviousPlaylistCommand.
/// </summary>
public class PreviousPlaylistCommandHandler : ICommandHandler<PreviousPlaylistCommand, Result>
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
        this._logger.LogInformation(
            "Playing previous playlist for Zone {ZoneId} from {Source}",
            request.ZoneId,
            request.Source
        );

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for PreviousPlaylistCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.PreviousPlaylistAsync().ConfigureAwait(false);
    }
}

/// <summary>
/// Handles the SetTrackRepeatCommand.
/// </summary>
public class SetTrackRepeatCommandHandler : ICommandHandler<SetTrackRepeatCommand, Result>
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
        this._logger.LogInformation(
            "Setting track repeat for Zone {ZoneId} to {Enabled} from {Source}",
            request.ZoneId,
            request.Enabled,
            request.Source
        );

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for SetTrackRepeatCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.SetTrackRepeatAsync(request.Enabled).ConfigureAwait(false);
    }
}

/// <summary>
/// Handles the ToggleTrackRepeatCommand.
/// </summary>
public class ToggleTrackRepeatCommandHandler : ICommandHandler<ToggleTrackRepeatCommand, Result>
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
        this._logger.LogInformation(
            "Toggling track repeat for Zone {ZoneId} from {Source}",
            request.ZoneId,
            request.Source
        );

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for ToggleTrackRepeatCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.ToggleTrackRepeatAsync().ConfigureAwait(false);
    }
}

/// <summary>
/// Handles the SetPlaylistShuffleCommand.
/// </summary>
public class SetPlaylistShuffleCommandHandler : ICommandHandler<SetPlaylistShuffleCommand, Result>
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
        this._logger.LogInformation(
            "Setting playlist shuffle for Zone {ZoneId} to {Enabled} from {Source}",
            request.ZoneId,
            request.Enabled,
            request.Source
        );

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for SetPlaylistShuffleCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.SetPlaylistShuffleAsync(request.Enabled).ConfigureAwait(false);
    }
}

/// <summary>
/// Handles the TogglePlaylistShuffleCommand.
/// </summary>
public class TogglePlaylistShuffleCommandHandler : ICommandHandler<TogglePlaylistShuffleCommand, Result>
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
        this._logger.LogInformation(
            "Toggling playlist shuffle for Zone {ZoneId} from {Source}",
            request.ZoneId,
            request.Source
        );

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for TogglePlaylistShuffleCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.TogglePlaylistShuffleAsync().ConfigureAwait(false);
    }
}

/// <summary>
/// Handles the SetPlaylistRepeatCommand.
/// </summary>
public class SetPlaylistRepeatCommandHandler : ICommandHandler<SetPlaylistRepeatCommand, Result>
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
        this._logger.LogInformation(
            "Setting playlist repeat for Zone {ZoneId} to {Enabled} from {Source}",
            request.ZoneId,
            request.Enabled,
            request.Source
        );

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for SetPlaylistRepeatCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.SetPlaylistRepeatAsync(request.Enabled).ConfigureAwait(false);
    }
}

/// <summary>
/// Handles the TogglePlaylistRepeatCommand.
/// </summary>
public class TogglePlaylistRepeatCommandHandler : ICommandHandler<TogglePlaylistRepeatCommand, Result>
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
        this._logger.LogInformation(
            "Toggling playlist repeat for Zone {ZoneId} from {Source}",
            request.ZoneId,
            request.Source
        );

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this._logger.LogWarning("Zone {ZoneId} not found for TogglePlaylistRepeatCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value!;
        return await zone.TogglePlaylistRepeatAsync().ConfigureAwait(false);
    }
}
