# 8. Zone Commands Implementation

This document continues the Cortex.Mediator implementation with detailed Zone Commands.

## 8.1. Volume and Mute Commands

```csharp
// /Server/Features/Zones/Commands/SetZoneVolumeCommand.cs
namespace SnapDog2.Server.Features.Zones.Commands;

using Cortex.Mediator;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to set the volume for a specific zone.
/// </summary>
public record SetZoneVolumeCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the desired volume level (0-100).
    /// </summary>
    public required int Volume { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to increase zone volume.
/// </summary>
public record VolumeUpCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the volume step to increase (default 5).
    /// </summary>
    public int Step { get; init; } = 5;

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to decrease zone volume.
/// </summary>
public record VolumeDownCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the volume step to decrease (default 5).
    /// </summary>
    public int Step { get; init; } = 5;

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to set zone mute state.
/// </summary>
public record SetZoneMuteCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets whether to mute (true) or unmute (false) the zone.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to toggle zone mute state.
/// </summary>
public record ToggleZoneMuteCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
```

## 8.2. Track Management Commands

```csharp
// /Server/Features/Zones/Commands/TrackCommands.cs
namespace SnapDog2.Server.Features.Zones.Commands;

using Cortex.Mediator;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to set a specific track in a zone.
/// </summary>
public record SetTrackCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the track index to play (1-based).
    /// </summary>
    public required int TrackIndex { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to play the next track in a zone.
/// </summary>
public record NextTrackCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to play the previous track in a zone.
/// </summary>
public record PreviousTrackCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to set track repeat mode.
/// </summary>
public record SetTrackRepeatCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets whether to enable track repeat.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to toggle track repeat mode.
/// </summary>
public record ToggleTrackRepeatCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
```

## 8.3. Playlist Management Commands

```csharp
// /Server/Features/Zones/Commands/PlaylistCommands.cs
namespace SnapDog2.Server.Features.Zones.Commands;

using Cortex.Mediator;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to set a specific playlist in a zone.
/// </summary>
public record SetPlaylistCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the playlist index (1-based, where 1 = Radio).
    /// </summary>
    public int? PlaylistIndex { get; init; }

    /// <summary>
    /// Gets the playlist ID (alternative to index).
    /// </summary>
    public string? PlaylistId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to play the next playlist in a zone.
/// </summary>
public record NextPlaylistCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to play the previous playlist in a zone.
/// </summary>
public record PreviousPlaylistCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to set playlist shuffle mode.
/// </summary>
public record SetPlaylistShuffleCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets whether to enable playlist shuffle.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to toggle playlist shuffle mode.
/// </summary>
public record TogglePlaylistShuffleCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to set playlist repeat mode.
/// </summary>
public record SetPlaylistRepeatCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets whether to enable playlist repeat.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to toggle playlist repeat mode.
/// </summary>
public record TogglePlaylistRepeatCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
```

## 8.4. Zone Command Validators

```csharp
// /Server/Features/Zones/Validators/ZoneCommandValidators.cs
namespace SnapDog2.Server.Features.Zones.Validators;

using FluentValidation;
using SnapDog2.Server.Features.Zones.Commands;

/// <summary>
/// Validator for the SetZoneVolumeCommand.
/// </summary>
public class SetZoneVolumeCommandValidator : AbstractValidator<SetZoneVolumeCommand>
{
    public SetZoneVolumeCommandValidator()
    {
        RuleFor(x => x.ZoneId)
            .GreaterThan(0)
            .WithMessage("Zone ID must be a positive integer.");

        RuleFor(x => x.Volume)
            .InclusiveBetween(0, 100)
            .WithMessage("Volume must be between 0 and 100.");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Invalid command source specified.");
    }
}

/// <summary>
/// Validator for volume step commands.
/// </summary>
public class VolumeStepCommandValidator<T> : AbstractValidator<T> where T : class
{
    public VolumeStepCommandValidator()
    {
        RuleFor(x => GetZoneId(x))
            .GreaterThan(0)
            .WithMessage("Zone ID must be a positive integer.");

        RuleFor(x => GetStep(x))
            .InclusiveBetween(1, 50)
            .WithMessage("Volume step must be between 1 and 50.");

        RuleFor(x => GetSource(x))
            .IsInEnum()
            .WithMessage("Invalid command source specified.");
    }

    private static int GetZoneId(T command) => command switch
    {
        VolumeUpCommand cmd => cmd.ZoneId,
        VolumeDownCommand cmd => cmd.ZoneId,
        _ => 0
    };

    private static int GetStep(T command) => command switch
    {
        VolumeUpCommand cmd => cmd.Step,
        VolumeDownCommand cmd => cmd.Step,
        _ => 0
    };

    private static CommandSource GetSource(T command) => command switch
    {
        VolumeUpCommand cmd => cmd.Source,
        VolumeDownCommand cmd => cmd.Source,
        _ => CommandSource.Internal
    };
}

/// <summary>
/// Validator for the SetTrackCommand.
/// </summary>
public class SetTrackCommandValidator : AbstractValidator<SetTrackCommand>
{
    public SetTrackCommandValidator()
    {
        RuleFor(x => x.ZoneId)
            .GreaterThan(0)
            .WithMessage("Zone ID must be a positive integer.");

        RuleFor(x => x.TrackIndex)
            .GreaterThan(0)
            .WithMessage("Track index must be a positive integer (1-based).");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Invalid command source specified.");
    }
}

/// <summary>
/// Validator for the SetPlaylistCommand.
/// </summary>
public class SetPlaylistCommandValidator : AbstractValidator<SetPlaylistCommand>
{
    public SetPlaylistCommandValidator()
    {
        RuleFor(x => x.ZoneId)
            .GreaterThan(0)
            .WithMessage("Zone ID must be a positive integer.");

        RuleFor(x => x)
            .Must(x => x.PlaylistIndex.HasValue || !string.IsNullOrEmpty(x.PlaylistId))
            .WithMessage("Either PlaylistIndex or PlaylistId must be specified.");

        RuleFor(x => x.PlaylistIndex)
            .GreaterThan(0)
            .When(x => x.PlaylistIndex.HasValue)
            .WithMessage("Playlist index must be a positive integer (1-based).");

        RuleFor(x => x.PlaylistId)
            .NotEmpty()
            .When(x => !x.PlaylistIndex.HasValue)
            .WithMessage("Playlist ID must not be empty when specified.");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Invalid command source specified.");
    }
}
```

## 8.5. Zone Command Handlers

```csharp
// /Server/Features/Zones/Handlers/SetZoneVolumeCommandHandler.cs
namespace SnapDog2.Server.Features.Zones.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Commands;

/// <summary>
/// Handles the SetZoneVolumeCommand.
/// </summary>
public partial class SetZoneVolumeCommandHandler : ICommandHandler<SetZoneVolumeCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<SetZoneVolumeCommandHandler> _logger;

    [LoggerMessage(2001, LogLevel.Information, "Setting volume for Zone {ZoneId} to {Volume} from {Source}")]
    private partial void LogHandling(int zoneId, int volume, CommandSource source);

    [LoggerMessage(2002, LogLevel.Warning, "Zone {ZoneId} not found for SetZoneVolumeCommand")]
    private partial void LogZoneNotFound(int zoneId);

    public SetZoneVolumeCommandHandler(
        IZoneManager zoneManager,
        ILogger<SetZoneVolumeCommandHandler> logger)
    {
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result> Handle(SetZoneVolumeCommand request, CancellationToken cancellationToken)
    {
        LogHandling(request.ZoneId, request.Volume, request.Source);

        // Get the zone service
        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            LogZoneNotFound(request.ZoneId);
            return zoneResult;
        }

        var zone = zoneResult.Value;

        // Delegate to the zone service
        var result = await zone.SetVolumeAsync(request.Volume).ConfigureAwait(false);

        return result;
    }
}

// /Server/Features/Zones/Handlers/PlayCommandHandler.cs
namespace SnapDog2.Server.Features.Zones.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Commands;

/// <summary>
/// Handles the PlayCommand.
/// </summary>
public partial class PlayCommandHandler : ICommandHandler<PlayCommand, Result>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<PlayCommandHandler> _logger;

    [LoggerMessage(2101, LogLevel.Information, "Starting playback for Zone {ZoneId} from {Source}")]
    private partial void LogHandling(int zoneId, CommandSource source);

    [LoggerMessage(2102, LogLevel.Information, "Starting playback for Zone {ZoneId} with track {TrackIndex} from {Source}")]
    private partial void LogHandlingWithTrack(int zoneId, int trackIndex, CommandSource source);

    [LoggerMessage(2103, LogLevel.Information, "Starting playback for Zone {ZoneId} with URL {MediaUrl} from {Source}")]
    private partial void LogHandlingWithUrl(int zoneId, string mediaUrl, CommandSource source);

    public PlayCommandHandler(
        IZoneManager zoneManager,
        ILogger<PlayCommandHandler> logger)
    {
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result> Handle(PlayCommand request, CancellationToken cancellationToken)
    {
        if (request.TrackIndex.HasValue)
        {
            LogHandlingWithTrack(request.ZoneId, request.TrackIndex.Value, request.Source);
        }
        else if (!string.IsNullOrEmpty(request.MediaUrl))
        {
            LogHandlingWithUrl(request.ZoneId, request.MediaUrl, request.Source);
        }
        else
        {
            LogHandling(request.ZoneId, request.Source);
        }

        // Get the zone service
        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            return zoneResult;
        }

        var zone = zoneResult.Value;

        // Handle different play scenarios
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
```

---

**Next**: Continue with Client Commands (6c), Zone Queries (6d), and Status Notifications (6e).
