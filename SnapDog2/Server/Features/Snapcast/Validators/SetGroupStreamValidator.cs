using FluentValidation;
using SnapDog2.Server.Features.Snapcast.Commands;

namespace SnapDog2.Server.Features.Snapcast.Validators;

/// <summary>
/// Validator for SetGroupStreamCommand.
/// </summary>
public class SetGroupStreamValidator : AbstractValidator<SetGroupStreamCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetGroupStreamValidator"/> class.
    /// </summary>
    public SetGroupStreamValidator()
    {
        RuleFor(static x => x.GroupId)
            .NotEmpty()
            .WithMessage("Group ID is required")
            .MaximumLength(100)
            .WithMessage("Group ID must not exceed 100 characters");

        RuleFor(static x => x.StreamId)
            .NotEmpty()
            .WithMessage("Stream ID is required")
            .MaximumLength(100)
            .WithMessage("Stream ID must not exceed 100 characters");
    }
}
