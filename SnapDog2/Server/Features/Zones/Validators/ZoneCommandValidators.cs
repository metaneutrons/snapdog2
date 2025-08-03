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
        this.RuleFor(x => x.ZoneId).GreaterThan(0).WithMessage("Zone ID must be a positive integer.");

        this.RuleFor(x => x.Volume).InclusiveBetween(0, 100).WithMessage("Volume must be between 0 and 100.");

        this.RuleFor(x => x.Source).IsInEnum().WithMessage("Invalid command source specified.");
    }
}

/// <summary>
/// Validator for the VolumeUpCommand.
/// </summary>
public class VolumeUpCommandValidator : AbstractValidator<VolumeUpCommand>
{
    public VolumeUpCommandValidator()
    {
        this.RuleFor(x => x.ZoneId).GreaterThan(0).WithMessage("Zone ID must be a positive integer.");

        this.RuleFor(x => x.Step).InclusiveBetween(1, 50).WithMessage("Volume step must be between 1 and 50.");

        this.RuleFor(x => x.Source).IsInEnum().WithMessage("Invalid command source specified.");
    }
}

/// <summary>
/// Validator for the VolumeDownCommand.
/// </summary>
public class VolumeDownCommandValidator : AbstractValidator<VolumeDownCommand>
{
    public VolumeDownCommandValidator()
    {
        this.RuleFor(x => x.ZoneId).GreaterThan(0).WithMessage("Zone ID must be a positive integer.");

        this.RuleFor(x => x.Step).InclusiveBetween(1, 50).WithMessage("Volume step must be between 1 and 50.");

        this.RuleFor(x => x.Source).IsInEnum().WithMessage("Invalid command source specified.");
    }
}

/// <summary>
/// Validator for the SetTrackCommand.
/// </summary>
public class SetTrackCommandValidator : AbstractValidator<SetTrackCommand>
{
    public SetTrackCommandValidator()
    {
        this.RuleFor(x => x.ZoneId).GreaterThan(0).WithMessage("Zone ID must be a positive integer.");

        this.RuleFor(x => x.TrackIndex).GreaterThan(0).WithMessage("Track index must be a positive integer (1-based).");

        this.RuleFor(x => x.Source).IsInEnum().WithMessage("Invalid command source specified.");
    }
}

/// <summary>
/// Validator for the SetPlaylistCommand.
/// </summary>
public class SetPlaylistCommandValidator : AbstractValidator<SetPlaylistCommand>
{
    public SetPlaylistCommandValidator()
    {
        this.RuleFor(x => x.ZoneId).GreaterThan(0).WithMessage("Zone ID must be a positive integer.");

        this.RuleFor(x => x)
            .Must(x => x.PlaylistIndex.HasValue || !string.IsNullOrEmpty(x.PlaylistId))
            .WithMessage("Either PlaylistIndex or PlaylistId must be specified.");

        this.RuleFor(x => x.PlaylistIndex)
            .GreaterThan(0)
            .When(x => x.PlaylistIndex.HasValue)
            .WithMessage("Playlist index must be a positive integer (1-based).");

        this.RuleFor(x => x.PlaylistId)
            .NotEmpty()
            .When(x => !x.PlaylistIndex.HasValue)
            .WithMessage("Playlist ID must not be empty when specified.");

        this.RuleFor(x => x.Source).IsInEnum().WithMessage("Invalid command source specified.");
    }
}

/// <summary>
/// Base validator for zone commands that only require a zone ID.
/// </summary>
public abstract class BaseZoneCommandValidator<T> : AbstractValidator<T>
    where T : class
{
    protected BaseZoneCommandValidator()
    {
        this.RuleFor(x => this.GetZoneId(x)).GreaterThan(0).WithMessage("Zone ID must be a positive integer.");

        this.RuleFor(x => this.GetSource(x)).IsInEnum().WithMessage("Invalid command source specified.");
    }

    protected abstract int GetZoneId(T command);
    protected abstract Core.Enums.CommandSource GetSource(T command);
}

/// <summary>
/// Validator for the PlayCommand.
/// </summary>
public class PlayCommandValidator : BaseZoneCommandValidator<PlayCommand>
{
    public PlayCommandValidator()
    {
        this.RuleFor(x => x.TrackIndex)
            .GreaterThan(0)
            .When(x => x.TrackIndex.HasValue)
            .WithMessage("Track index must be a positive integer (1-based) when specified.");

        this.RuleFor(x => x.MediaUrl)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.MediaUrl))
            .WithMessage("Media URL must not be empty when specified.");
    }

    protected override int GetZoneId(PlayCommand command)
    {
        return command.ZoneId;
    }

    protected override Core.Enums.CommandSource GetSource(PlayCommand command)
    {
        return command.Source;
    }
}

/// <summary>
/// Validator for the PauseCommand.
/// </summary>
public class PauseCommandValidator : BaseZoneCommandValidator<PauseCommand>
{
    protected override int GetZoneId(PauseCommand command)
    {
        return command.ZoneId;
    }

    protected override Core.Enums.CommandSource GetSource(PauseCommand command)
    {
        return command.Source;
    }
}

/// <summary>
/// Validator for the StopCommand.
/// </summary>
public class StopCommandValidator : BaseZoneCommandValidator<StopCommand>
{
    protected override int GetZoneId(StopCommand command)
    {
        return command.ZoneId;
    }

    protected override Core.Enums.CommandSource GetSource(StopCommand command)
    {
        return command.Source;
    }
}

/// <summary>
/// Validator for the SetZoneMuteCommand.
/// </summary>
public class SetZoneMuteCommandValidator : BaseZoneCommandValidator<SetZoneMuteCommand>
{
    protected override int GetZoneId(SetZoneMuteCommand command)
    {
        return command.ZoneId;
    }

    protected override Core.Enums.CommandSource GetSource(SetZoneMuteCommand command)
    {
        return command.Source;
    }
}

/// <summary>
/// Validator for the ToggleZoneMuteCommand.
/// </summary>
public class ToggleZoneMuteCommandValidator : BaseZoneCommandValidator<ToggleZoneMuteCommand>
{
    protected override int GetZoneId(ToggleZoneMuteCommand command)
    {
        return command.ZoneId;
    }

    protected override Core.Enums.CommandSource GetSource(ToggleZoneMuteCommand command)
    {
        return command.Source;
    }
}

/// <summary>
/// Validator for the NextTrackCommand.
/// </summary>
public class NextTrackCommandValidator : BaseZoneCommandValidator<NextTrackCommand>
{
    protected override int GetZoneId(NextTrackCommand command)
    {
        return command.ZoneId;
    }

    protected override Core.Enums.CommandSource GetSource(NextTrackCommand command)
    {
        return command.Source;
    }
}

/// <summary>
/// Validator for the PreviousTrackCommand.
/// </summary>
public class PreviousTrackCommandValidator : BaseZoneCommandValidator<PreviousTrackCommand>
{
    protected override int GetZoneId(PreviousTrackCommand command)
    {
        return command.ZoneId;
    }

    protected override Core.Enums.CommandSource GetSource(PreviousTrackCommand command)
    {
        return command.Source;
    }
}

/// <summary>
/// Validator for the NextPlaylistCommand.
/// </summary>
public class NextPlaylistCommandValidator : BaseZoneCommandValidator<NextPlaylistCommand>
{
    protected override int GetZoneId(NextPlaylistCommand command)
    {
        return command.ZoneId;
    }

    protected override Core.Enums.CommandSource GetSource(NextPlaylistCommand command)
    {
        return command.Source;
    }
}

/// <summary>
/// Validator for the PreviousPlaylistCommand.
/// </summary>
public class PreviousPlaylistCommandValidator : BaseZoneCommandValidator<PreviousPlaylistCommand>
{
    protected override int GetZoneId(PreviousPlaylistCommand command)
    {
        return command.ZoneId;
    }

    protected override Core.Enums.CommandSource GetSource(PreviousPlaylistCommand command)
    {
        return command.Source;
    }
}

/// <summary>
/// Validator for the SetTrackRepeatCommand.
/// </summary>
public class SetTrackRepeatCommandValidator : BaseZoneCommandValidator<SetTrackRepeatCommand>
{
    protected override int GetZoneId(SetTrackRepeatCommand command)
    {
        return command.ZoneId;
    }

    protected override Core.Enums.CommandSource GetSource(SetTrackRepeatCommand command)
    {
        return command.Source;
    }
}

/// <summary>
/// Validator for the ToggleTrackRepeatCommand.
/// </summary>
public class ToggleTrackRepeatCommandValidator : BaseZoneCommandValidator<ToggleTrackRepeatCommand>
{
    protected override int GetZoneId(ToggleTrackRepeatCommand command)
    {
        return command.ZoneId;
    }

    protected override Core.Enums.CommandSource GetSource(ToggleTrackRepeatCommand command)
    {
        return command.Source;
    }
}

/// <summary>
/// Validator for the SetPlaylistShuffleCommand.
/// </summary>
public class SetPlaylistShuffleCommandValidator : BaseZoneCommandValidator<SetPlaylistShuffleCommand>
{
    protected override int GetZoneId(SetPlaylistShuffleCommand command)
    {
        return command.ZoneId;
    }

    protected override Core.Enums.CommandSource GetSource(SetPlaylistShuffleCommand command)
    {
        return command.Source;
    }
}

/// <summary>
/// Validator for the TogglePlaylistShuffleCommand.
/// </summary>
public class TogglePlaylistShuffleCommandValidator : BaseZoneCommandValidator<TogglePlaylistShuffleCommand>
{
    protected override int GetZoneId(TogglePlaylistShuffleCommand command)
    {
        return command.ZoneId;
    }

    protected override Core.Enums.CommandSource GetSource(TogglePlaylistShuffleCommand command)
    {
        return command.Source;
    }
}

/// <summary>
/// Validator for the SetPlaylistRepeatCommand.
/// </summary>
public class SetPlaylistRepeatCommandValidator : BaseZoneCommandValidator<SetPlaylistRepeatCommand>
{
    protected override int GetZoneId(SetPlaylistRepeatCommand command)
    {
        return command.ZoneId;
    }

    protected override Core.Enums.CommandSource GetSource(SetPlaylistRepeatCommand command)
    {
        return command.Source;
    }
}

/// <summary>
/// Validator for the TogglePlaylistRepeatCommand.
/// </summary>
public class TogglePlaylistRepeatCommandValidator : BaseZoneCommandValidator<TogglePlaylistRepeatCommand>
{
    protected override int GetZoneId(TogglePlaylistRepeatCommand command)
    {
        return command.ZoneId;
    }

    protected override Core.Enums.CommandSource GetSource(TogglePlaylistRepeatCommand command)
    {
        return command.Source;
    }
}
