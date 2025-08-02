namespace SnapDog2.Server.Features.Zones.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Commands;

/// <summary>
/// Handles the PlayCommand.
/// </summary>
public class PlayCommandHandler : ICommandHandler<PlayCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<PlayCommandHandler> _logger;

    public PlayCommandHandler(IZoneManager zoneManager, ILogger<PlayCommandHandler> logger)
    {
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result> Handle(PlayCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting playback for Zone {ZoneId} from {Source}", request.ZoneId, request.Source);

        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            _logger.LogWarning("Zone {ZoneId} not found for PlayCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value;

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
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result> Handle(PauseCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Pausing playback for Zone {ZoneId} from {Source}", request.ZoneId, request.Source);

        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            _logger.LogWarning("Zone {ZoneId} not found for PauseCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value;
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
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result> Handle(StopCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping playback for Zone {ZoneId} from {Source}", request.ZoneId, request.Source);

        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            _logger.LogWarning("Zone {ZoneId} not found for StopCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value;
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
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result> Handle(SetZoneVolumeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting volume for Zone {ZoneId} to {Volume} from {Source}", request.ZoneId, request.Volume, request.Source);

        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            _logger.LogWarning("Zone {ZoneId} not found for SetZoneVolumeCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value;
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
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result> Handle(VolumeUpCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Increasing volume for Zone {ZoneId} by {Step} from {Source}", request.ZoneId, request.Step, request.Source);

        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            _logger.LogWarning("Zone {ZoneId} not found for VolumeUpCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value;
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
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result> Handle(VolumeDownCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Decreasing volume for Zone {ZoneId} by {Step} from {Source}", request.ZoneId, request.Step, request.Source);

        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            _logger.LogWarning("Zone {ZoneId} not found for VolumeDownCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value;
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
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result> Handle(SetZoneMuteCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting mute for Zone {ZoneId} to {Enabled} from {Source}", request.ZoneId, request.Enabled, request.Source);

        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            _logger.LogWarning("Zone {ZoneId} not found for SetZoneMuteCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value;
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
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result> Handle(ToggleZoneMuteCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Toggling mute for Zone {ZoneId} from {Source}", request.ZoneId, request.Source);

        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            _logger.LogWarning("Zone {ZoneId} not found for ToggleZoneMuteCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value;
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
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result> Handle(SetTrackCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting track for Zone {ZoneId} to {TrackIndex} from {Source}", request.ZoneId, request.TrackIndex, request.Source);

        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            _logger.LogWarning("Zone {ZoneId} not found for SetTrackCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value;
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
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result> Handle(NextTrackCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Playing next track for Zone {ZoneId} from {Source}", request.ZoneId, request.Source);

        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            _logger.LogWarning("Zone {ZoneId} not found for NextTrackCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value;
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
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result> Handle(PreviousTrackCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Playing previous track for Zone {ZoneId} from {Source}", request.ZoneId, request.Source);

        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            _logger.LogWarning("Zone {ZoneId} not found for PreviousTrackCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value;
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
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result> Handle(SetPlaylistCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting playlist for Zone {ZoneId} from {Source}", request.ZoneId, request.Source);

        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            _logger.LogWarning("Zone {ZoneId} not found for SetPlaylistCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value;

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
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result> Handle(NextPlaylistCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Playing next playlist for Zone {ZoneId} from {Source}", request.ZoneId, request.Source);

        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            _logger.LogWarning("Zone {ZoneId} not found for NextPlaylistCommand", request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value;
        return await zone.NextPlaylistAsync().ConfigureAwait(false);
    }
}
