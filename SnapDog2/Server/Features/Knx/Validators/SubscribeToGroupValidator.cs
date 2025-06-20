using FluentValidation;
using SnapDog2.Server.Features.Knx.Commands;

namespace SnapDog2.Server.Features.Knx.Validators;

/// <summary>
/// Validator for SubscribeToGroupCommand.
/// </summary>
public class SubscribeToGroupValidator : AbstractValidator<SubscribeToGroupCommand>
{
    public SubscribeToGroupValidator()
    {
        RuleFor(x => x.Address).NotNull().WithMessage("KNX address is required");
    }
}
