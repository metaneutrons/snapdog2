namespace SnapDog2.Server.Features.Clients.Validators;

using FluentValidation;
using SnapDog2.Server.Features.Clients.Commands;

/// <summary>
/// Validator for the SetClientVolumeCommand.
/// </summary>
public class SetClientVolumeCommandValidator : AbstractValidator<SetClientVolumeCommand>
{
    public SetClientVolumeCommandValidator()
    {
        RuleFor(x => x.ClientId)
            .GreaterThan(0)
            .WithMessage("Client ID must be a positive integer.");

        RuleFor(x => x.Volume)
            .InclusiveBetween(0, 100)
            .WithMessage("Volume must be between 0 and 100.");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Invalid command source specified.");
    }
}

/// <summary>
/// Validator for the SetClientMuteCommand.
/// </summary>
public class SetClientMuteCommandValidator : AbstractValidator<SetClientMuteCommand>
{
    public SetClientMuteCommandValidator()
    {
        RuleFor(x => x.ClientId)
            .GreaterThan(0)
            .WithMessage("Client ID must be a positive integer.");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Invalid command source specified.");
    }
}

/// <summary>
/// Validator for the ToggleClientMuteCommand.
/// </summary>
public class ToggleClientMuteCommandValidator : AbstractValidator<ToggleClientMuteCommand>
{
    public ToggleClientMuteCommandValidator()
    {
        RuleFor(x => x.ClientId)
            .GreaterThan(0)
            .WithMessage("Client ID must be a positive integer.");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Invalid command source specified.");
    }
}

/// <summary>
/// Validator for the SetClientLatencyCommand.
/// </summary>
public class SetClientLatencyCommandValidator : AbstractValidator<SetClientLatencyCommand>
{
    public SetClientLatencyCommandValidator()
    {
        RuleFor(x => x.ClientId)
            .GreaterThan(0)
            .WithMessage("Client ID must be a positive integer.");

        RuleFor(x => x.LatencyMs)
            .InclusiveBetween(0, 10000)
            .WithMessage("Latency must be between 0 and 10000 milliseconds.");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Invalid command source specified.");
    }
}

/// <summary>
/// Validator for the AssignClientToZoneCommand.
/// </summary>
public class AssignClientToZoneCommandValidator : AbstractValidator<AssignClientToZoneCommand>
{
    public AssignClientToZoneCommandValidator()
    {
        RuleFor(x => x.ClientId)
            .GreaterThan(0)
            .WithMessage("Client ID must be a positive integer.");

        RuleFor(x => x.ZoneId)
            .GreaterThan(0)
            .WithMessage("Zone ID must be a positive integer.");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Invalid command source specified.");
    }
}
