using FluentValidation;
using SnapDog2.Server.Features.Knx.Commands;

namespace SnapDog2.Server.Features.Knx.Validators;

/// <summary>
/// Validator for ReadGroupValueCommand.
/// </summary>
public class ReadGroupValueValidator : AbstractValidator<ReadGroupValueCommand>
{
    public ReadGroupValueValidator()
    {
        RuleFor(static x => x.Address).NotNull().WithMessage("KNX address is required");
    }
}
