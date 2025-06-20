using FluentValidation;
using SnapDog2.Server.Features.Snapcast.Commands;

namespace SnapDog2.Server.Features.Snapcast.Validators;

/// <summary>
/// Validator for SetClientMuteCommand.
/// </summary>
public class SetClientMuteValidator : AbstractValidator<SetClientMuteCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetClientMuteValidator"/> class.
    /// </summary>
    public SetClientMuteValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty()
            .WithMessage("Client ID is required")
            .MaximumLength(100)
            .WithMessage("Client ID must not exceed 100 characters");
    }
}
