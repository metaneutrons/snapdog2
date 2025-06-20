using FluentValidation;
using SnapDog2.Server.Features.Knx.Commands;

namespace SnapDog2.Server.Features.Knx.Validators;

/// <summary>
/// Validator for UnsubscribeFromGroupCommand.
/// </summary>
public class UnsubscribeFromGroupValidator : AbstractValidator<UnsubscribeFromGroupCommand>
{
    public UnsubscribeFromGroupValidator()
    {
        RuleFor(x => x.Address).NotNull().WithMessage("KNX address is required");
    }
}
