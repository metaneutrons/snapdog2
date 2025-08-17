namespace SnapDog2.Server.Features.Snapcast.Validators;

using FluentValidation;
using SnapDog2.Server.Features.Snapcast.Commands;

/// <summary>
/// Validator for SetSnapcastClientMuteCommand.
/// </summary>
public class SetSnapcastClientMuteCommandValidator : AbstractValidator<SetSnapcastClientMuteCommand>
{
    public SetSnapcastClientMuteCommandValidator()
    {
        RuleFor(x => x.ClientIndex).NotEmpty().WithMessage("Client ID is required");
    }
}
