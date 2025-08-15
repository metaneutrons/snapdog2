# 17. Zone Commands Implementation

This document continues the Cortex.Mediator implementation with detailed Zone Commands.

## 17.1. Volume and Mute Commands

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
    public required int ZoneIndex { get; init; }

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
    public required int ZoneIndex { get; init; }

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
    public required int ZoneIndex { get; init; }

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
    public required int ZoneIndex { get; init; }

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
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
```

## 17.2. Track Management Commands

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
    public required int ZoneIndex { get; init; }

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
    public required int ZoneIndex { get; init; }

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
    public required int ZoneIndex { get; init; }

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
    public required int ZoneIndex { get; init; }

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
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
```

## 17.3. Playlist Management Commands

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
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the playlist index (1-based, where 1 = Radio).
    /// </summary>
    public int? PlaylistIndex { get; init; }

    /// <summary>
    /// Gets the playlist ID (alternative to index).
    /// </summary>
    public string? PlaylistIndex { get; init; }

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
    public required int ZoneIndex { get; init; }

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
    public required int ZoneIndex { get; init; }

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
    public required int ZoneIndex { get; init; }

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
    public required int ZoneIndex { get; init; }

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
    public required int ZoneIndex { get; init; }

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
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
```

## 17.4. Zone Command Validators

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
        RuleFor(x => x.ZoneIndex)
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
        RuleFor(x => GetZoneIndex(x))
            .GreaterThan(0)
            .WithMessage("Zone ID must be a positive integer.");

        RuleFor(x => GetStep(x))
            .InclusiveBetween(1, 50)
            .WithMessage("Volume step must be between 1 and 50.");

        RuleFor(x => GetSource(x))
            .IsInEnum()
            .WithMessage("Invalid command source specified.");
    }

    private static int GetZoneIndex(T command) => command switch
    {
        VolumeUpCommand cmd => cmd.ZoneIndex,
        VolumeDownCommand cmd => cmd.ZoneIndex,
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
        RuleFor(x => x.ZoneIndex)
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
        RuleFor(x => x.ZoneIndex)
            .GreaterThan(0)
            .WithMessage("Zone ID must be a positive integer.");

        RuleFor(x => x)
            .Must(x => x.PlaylistIndex.HasValue || !string.IsNullOrEmpty(x.PlaylistIndex))
            .WithMessage("Either PlaylistIndex or PlaylistIndex must be specified.");

        RuleFor(x => x.PlaylistIndex)
            .GreaterThan(0)
            .When(x => x.PlaylistIndex.HasValue)
            .WithMessage("Playlist index must be a positive integer (1-based).");

        RuleFor(x => x.PlaylistIndex)
            .NotEmpty()
            .When(x => !x.PlaylistIndex.HasValue)
            .WithMessage("Playlist Index must not be empty when specified.");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Invalid command source specified.");
    }
}
```

## 17.5. Zone Command Handlers

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

    [LoggerMessage(2001, LogLevel.Information, "Setting volume for Zone {ZoneIndex} to {Volume} from {Source}")]
    private partial void LogHandling(int zoneIndex, int volume, CommandSource source);

    [LoggerMessage(2002, LogLevel.Warning, "Zone {ZoneIndex} not found for SetZoneVolumeCommand")]
    private partial void LogZoneNotFound(int zoneIndex);

    public SetZoneVolumeCommandHandler(
        IZoneManager zoneManager,
        ILogger<SetZoneVolumeCommandHandler> logger)
    {
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result> Handle(SetZoneVolumeCommand request, CancellationToken cancellationToken)
    {
        LogHandling(request.ZoneIndex, request.Volume, request.Source);

        // Get the zone service
        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            LogZoneNotFound(request.ZoneIndex);
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

    [LoggerMessage(2101, LogLevel.Information, "Starting playback for Zone {ZoneIndex} from {Source}")]
    private partial void LogHandling(int zoneIndex, CommandSource source);

    [LoggerMessage(2102, LogLevel.Information, "Starting playback for Zone {ZoneIndex} with track {TrackIndex} from {Source}")]
    private partial void LogHandlingWithTrack(int zoneIndex, int trackIndex, CommandSource source);

    [LoggerMessage(2103, LogLevel.Information, "Starting playback for Zone {ZoneIndex} with URL {MediaUrl} from {Source}")]
    private partial void LogHandlingWithUrl(int zoneIndex, string mediaUrl, CommandSource source);

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
            LogHandlingWithTrack(request.ZoneIndex, request.TrackIndex.Value, request.Source);
        }
        else if (!string.IsNullOrEmpty(request.MediaUrl))
        {
            LogHandlingWithUrl(request.ZoneIndex, request.MediaUrl, request.Source);
        }
        else
        {
            LogHandling(request.ZoneIndex, request.Source);
        }

        // Get the zone service
        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
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
