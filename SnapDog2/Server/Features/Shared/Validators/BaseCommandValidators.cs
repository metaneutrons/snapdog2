namespace SnapDog2.Server.Features.Shared.Validators;

using FluentValidation;
using SnapDog2.Core.Enums;

/// <summary>
/// Base validator for commands that require a zone ID and command source.
/// </summary>
public abstract class BaseZoneCommandValidator<T> : AbstractValidator<T>
    where T : class
{
    protected BaseZoneCommandValidator()
    {
        RuleFor(x => GetZoneIndex(x)).GreaterThan(0).WithMessage("Zone ID must be a positive integer.");

        RuleFor(x => GetSource(x)).IsInEnum().WithMessage("Invalid command source specified.");
    }

    /// <summary>
    /// Extract the zone ID from the command. Must be implemented by derived validators.
    /// </summary>
    protected abstract int GetZoneIndex(T command);

    /// <summary>
    /// Extract the command source from the command. Must be implemented by derived validators.
    /// </summary>
    protected abstract CommandSource GetSource(T command);
}

/// <summary>
/// Base validator for commands that require a client ID and command source.
/// </summary>
public abstract class BaseClientCommandValidator<T> : AbstractValidator<T>
    where T : class
{
    protected BaseClientCommandValidator()
    {
        RuleFor(x => GetClientIndex(x)).GreaterThan(0).WithMessage("Client ID must be a positive integer.");

        RuleFor(x => GetSource(x)).IsInEnum().WithMessage("Invalid command source specified.");
    }

    /// <summary>
    /// Extract the client ID from the command. Must be implemented by derived validators.
    /// </summary>
    protected abstract int GetClientIndex(T command);

    /// <summary>
    /// Extract the command source from the command. Must be implemented by derived validators.
    /// </summary>
    protected abstract CommandSource GetSource(T command);
}

/// <summary>
/// Base validator for volume-related commands (both zone and client).
/// Provides common volume validation rules.
/// </summary>
public abstract class BaseVolumeCommandValidator<T> : AbstractValidator<T>
    where T : class
{
    protected BaseVolumeCommandValidator()
    {
        RuleFor(x => GetVolume(x)).InclusiveBetween(0, 100).WithMessage("Volume must be between 0 and 100.");
    }

    /// <summary>
    /// Extract the volume value from the command. Must be implemented by derived validators.
    /// </summary>
    protected abstract int GetVolume(T command);
}

/// <summary>
/// Base validator for volume step commands (volume up/down).
/// Provides common step validation rules.
/// </summary>
public abstract class BaseVolumeStepCommandValidator<T> : AbstractValidator<T>
    where T : class
{
    protected BaseVolumeStepCommandValidator()
    {
        RuleFor(x => GetStep(x)).InclusiveBetween(1, 50).WithMessage("Volume step must be between 1 and 50.");
    }

    /// <summary>
    /// Extract the step value from the command. Must be implemented by derived validators.
    /// </summary>
    protected abstract int GetStep(T command);
}

/// <summary>
/// Base validator for track-related commands.
/// Provides common track index validation rules.
/// </summary>
public abstract class BaseTrackCommandValidator<T> : AbstractValidator<T>
    where T : class
{
    protected BaseTrackCommandValidator()
    {
        RuleFor(x => GetTrackIndex(x)).GreaterThan(0).WithMessage("Track index must be a positive integer (1-based).");
    }

    /// <summary>
    /// Extract the track index from the command. Must be implemented by derived validators.
    /// </summary>
    protected abstract int GetTrackIndex(T command);
}

/// <summary>
/// Base validator for playlist-related commands.
/// Provides common playlist validation rules.
/// </summary>
public abstract class BasePlaylistCommandValidator<T> : AbstractValidator<T>
    where T : class
{
    protected BasePlaylistCommandValidator()
    {
        // Playlist index validation (required)
        RuleFor(x => GetPlaylistIndex(x))
            .GreaterThan(0)
            .WithMessage("Playlist index must be a positive integer (1-based).");
    }

    /// <summary>
    /// Extract the playlist index from the command. Must be implemented by derived validators.
    /// </summary>
    protected abstract int GetPlaylistIndex(T command);
}

/// <summary>
/// Base validator for latency-related commands.
/// Provides common latency validation rules.
/// </summary>
public abstract class BaseLatencyCommandValidator<T> : AbstractValidator<T>
    where T : class
{
    protected BaseLatencyCommandValidator()
    {
        RuleFor(x => GetLatencyMs(x))
            .InclusiveBetween(0, 10000)
            .WithMessage("Latency must be between 0 and 10000 milliseconds.");
    }

    /// <summary>
    /// Extract the latency value from the command. Must be implemented by derived validators.
    /// </summary>
    protected abstract int GetLatencyMs(T command);
}

/// <summary>
/// Composite validator that combines multiple base validators.
/// Useful for commands that need multiple types of validation.
/// </summary>
public abstract class CompositeZoneVolumeCommandValidator<T> : BaseZoneCommandValidator<T>
    where T : class
{
    protected CompositeZoneVolumeCommandValidator()
    {
        RuleFor(x => GetVolume(x)).InclusiveBetween(0, 100).WithMessage("Volume must be between 0 and 100.");
    }

    /// <summary>
    /// Extract the volume value from the command. Must be implemented by derived validators.
    /// </summary>
    protected abstract int GetVolume(T command);
}

/// <summary>
/// Composite validator for zone volume step commands.
/// </summary>
public abstract class CompositeZoneVolumeStepCommandValidator<T> : BaseZoneCommandValidator<T>
    where T : class
{
    protected CompositeZoneVolumeStepCommandValidator()
    {
        RuleFor(x => GetStep(x)).InclusiveBetween(1, 50).WithMessage("Volume step must be between 1 and 50.");
    }

    /// <summary>
    /// Extract the step value from the command. Must be implemented by derived validators.
    /// </summary>
    protected abstract int GetStep(T command);
}

/// <summary>
/// Composite validator for client volume commands.
/// </summary>
public abstract class CompositeClientVolumeCommandValidator<T> : BaseClientCommandValidator<T>
    where T : class
{
    protected CompositeClientVolumeCommandValidator()
    {
        RuleFor(x => GetVolume(x)).InclusiveBetween(0, 100).WithMessage("Volume must be between 0 and 100.");
    }

    /// <summary>
    /// Extract the volume value from the command. Must be implemented by derived validators.
    /// </summary>
    protected abstract int GetVolume(T command);
}

/// <summary>
/// Composite validator for zone track commands.
/// </summary>
public abstract class CompositeZoneTrackCommandValidator<T> : BaseZoneCommandValidator<T>
    where T : class
{
    protected CompositeZoneTrackCommandValidator()
    {
        RuleFor(x => GetTrackIndex(x)).GreaterThan(0).WithMessage("Track index must be a positive integer (1-based).");
    }

    /// <summary>
    /// Extract the track index from the command. Must be implemented by derived validators.
    /// </summary>
    protected abstract int GetTrackIndex(T command);
}

/// <summary>
/// Composite validator for zone playlist commands.
/// </summary>
public abstract class CompositeZonePlaylistCommandValidator<T> : BaseZoneCommandValidator<T>
    where T : class
{
    protected CompositeZonePlaylistCommandValidator()
    {
        // Playlist index validation (required)
        RuleFor(x => GetPlaylistIndex(x))
            .GreaterThan(0)
            .WithMessage("Playlist index must be a positive integer (1-based).");
    }

    /// <summary>
    /// Extract the playlist index from the command. Must be implemented by derived validators.
    /// </summary>
    protected abstract int GetPlaylistIndex(T command);
}

/// <summary>
/// Composite validator for client latency commands.
/// </summary>
public abstract class CompositeClientLatencyCommandValidator<T> : BaseClientCommandValidator<T>
    where T : class
{
    protected CompositeClientLatencyCommandValidator()
    {
        RuleFor(x => GetLatencyMs(x))
            .InclusiveBetween(0, 10000)
            .WithMessage("Latency must be between 0 and 10000 milliseconds.");
    }

    /// <summary>
    /// Extract the latency value from the command. Must be implemented by derived validators.
    /// </summary>
    protected abstract int GetLatencyMs(T command);
}

/// <summary>
/// Composite validator for client zone assignment commands.
/// </summary>
public abstract class CompositeClientZoneAssignmentValidator<T> : BaseClientCommandValidator<T>
    where T : class
{
    protected CompositeClientZoneAssignmentValidator()
    {
        RuleFor(x => GetZoneIndex(x)).GreaterThan(0).WithMessage("Zone ID must be a positive integer.");
    }

    /// <summary>
    /// Extract the zone ID from the command. Must be implemented by derived validators.
    /// </summary>
    protected abstract int GetZoneIndex(T command);
}
