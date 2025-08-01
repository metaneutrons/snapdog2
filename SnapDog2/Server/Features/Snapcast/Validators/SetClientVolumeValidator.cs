using FluentValidation;
using SnapDog2.Server.Features.Snapcast.Commands;

namespace SnapDog2.Server.Features.Snapcast.Validators;

/// <summary>
/// Validator for SetClientVolumeCommand.
/// </summary>
public class SetClientVolumeValidator : AbstractValidator<SetClientVolumeCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetClientVolumeValidator"/> class.
    /// </summary>
    public SetClientVolumeValidator()
    {
        RuleFor(static x => x.ClientId)
            .NotEmpty()
            .WithMessage("Client ID is required")
            .MaximumLength(100)
            .WithMessage("Client ID must not exceed 100 characters");

        RuleFor(static x => x.Volume).InclusiveBetween(0, 100).WithMessage("Volume must be between 0 and 100");
    }
}
