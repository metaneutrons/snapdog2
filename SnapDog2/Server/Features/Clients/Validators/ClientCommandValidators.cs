namespace SnapDog2.Server.Features.Clients.Validators;

using FluentValidation;
using SnapDog2.Server.Features.Clients.Commands.Config;
using SnapDog2.Server.Features.Clients.Commands.Volume;

/// <summary>
/// Validator for the SetClientVolumeCommand.
/// </summary>
public class SetClientVolumeCommandValidator : AbstractValidator<SetClientVolumeCommand>
{
    public SetClientVolumeCommandValidator()
    {
        this.RuleFor(x => x.ClientId).GreaterThan(0).WithMessage("Client ID must be a positive integer.");

        this.RuleFor(x => x.Volume).InclusiveBetween(0, 100).WithMessage("Volume must be between 0 and 100.");

        this.RuleFor(x => x.Source).IsInEnum().WithMessage("Invalid command source specified.");
    }
}

/// <summary>
/// Validator for the SetClientMuteCommand.
/// </summary>
public class SetClientMuteCommandValidator : AbstractValidator<SetClientMuteCommand>
{
    public SetClientMuteCommandValidator()
    {
        this.RuleFor(x => x.ClientId).GreaterThan(0).WithMessage("Client ID must be a positive integer.");

        this.RuleFor(x => x.Source).IsInEnum().WithMessage("Invalid command source specified.");
    }
}

/// <summary>
/// Validator for the ToggleClientMuteCommand.
/// </summary>
public class ToggleClientMuteCommandValidator : AbstractValidator<ToggleClientMuteCommand>
{
    public ToggleClientMuteCommandValidator()
    {
        this.RuleFor(x => x.ClientId).GreaterThan(0).WithMessage("Client ID must be a positive integer.");

        this.RuleFor(x => x.Source).IsInEnum().WithMessage("Invalid command source specified.");
    }
}

/// <summary>
/// Validator for the SetClientLatencyCommand.
/// </summary>
public class SetClientLatencyCommandValidator : AbstractValidator<SetClientLatencyCommand>
{
    public SetClientLatencyCommandValidator()
    {
        this.RuleFor(x => x.ClientId).GreaterThan(0).WithMessage("Client ID must be a positive integer.");

        this.RuleFor(x => x.LatencyMs)
            .InclusiveBetween(0, 10000)
            .WithMessage("Latency must be between 0 and 10000 milliseconds.");

        this.RuleFor(x => x.Source).IsInEnum().WithMessage("Invalid command source specified.");
    }
}

/// <summary>
/// Validator for the AssignClientToZoneCommand.
/// </summary>
public class AssignClientToZoneCommandValidator : AbstractValidator<AssignClientToZoneCommand>
{
    public AssignClientToZoneCommandValidator()
    {
        this.RuleFor(x => x.ClientId).GreaterThan(0).WithMessage("Client ID must be a positive integer.");

        this.RuleFor(x => x.ZoneId).GreaterThan(0).WithMessage("Zone ID must be a positive integer.");

        this.RuleFor(x => x.Source).IsInEnum().WithMessage("Invalid command source specified.");
    }
}
