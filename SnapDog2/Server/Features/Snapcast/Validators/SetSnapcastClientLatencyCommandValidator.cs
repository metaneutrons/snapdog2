namespace SnapDog2.Server.Features.Snapcast.Validators;

using FluentValidation;
using SnapDog2.Server.Features.Snapcast.Commands;

/// <summary>
/// Validator for SetSnapcastClientLatencyCommand.
/// </summary>
public class SetSnapcastClientLatencyCommandValidator : AbstractValidator<SetSnapcastClientLatencyCommand>
{
    public SetSnapcastClientLatencyCommandValidator()
    {
        RuleFor(x => x.ClientIndex).NotEmpty().WithMessage("Client ID is required");

        RuleFor(x => x.LatencyMs)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Latency must be non-negative")
            .LessThanOrEqualTo(10000)
            .WithMessage("Latency must be less than 10 seconds");
    }
}
