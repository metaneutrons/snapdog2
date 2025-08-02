namespace SnapDog2.Server.Features.Snapcast.Validators;

using FluentValidation;
using SnapDog2.Server.Features.Snapcast.Commands;

/// <summary>
/// Validator for SetSnapcastClientVolumeCommand.
/// </summary>
public class SetSnapcastClientVolumeCommandValidator : AbstractValidator<SetSnapcastClientVolumeCommand>
{
    public SetSnapcastClientVolumeCommandValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty().WithMessage("Client ID is required");

        RuleFor(x => x.Volume).InclusiveBetween(0, 100).WithMessage("Volume must be between 0 and 100");
    }
}
