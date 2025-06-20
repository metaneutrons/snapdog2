using FluentValidation;
using SnapDog2.Server.Features.Knx.Commands;

namespace SnapDog2.Server.Features.Knx.Validators;

/// <summary>
/// Validator for WriteGroupValueCommand.
/// </summary>
public class WriteGroupValueValidator : AbstractValidator<WriteGroupValueCommand>
{
    public WriteGroupValueValidator()
    {
        RuleFor(x => x.Address).NotNull().WithMessage("KNX address is required");

        RuleFor(x => x.Value)
            .NotNull()
            .WithMessage("Value is required")
            .Must(value => value.Length > 0)
            .WithMessage("Value cannot be empty")
            .Must(value => value.Length <= 14)
            .WithMessage("Value cannot exceed 14 bytes for KNX group communication");
    }
}
