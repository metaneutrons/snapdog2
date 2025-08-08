namespace SnapDog2.Server.Features.Clients.Validators;

using SnapDog2.Core.Enums;
using SnapDog2.Server.Features.Clients.Commands.Config;
using SnapDog2.Server.Features.Clients.Commands.Volume;
using SnapDog2.Server.Features.Shared.Validators;

/// <summary>
/// Validator for SetClientVolumeCommand using base class.
/// </summary>
public class SetClientVolumeCommandValidator : CompositeClientVolumeCommandValidator<SetClientVolumeCommand>
{
    protected override int GetClientId(SetClientVolumeCommand command) => command.ClientId;

    protected override CommandSource GetSource(SetClientVolumeCommand command) => command.Source;

    protected override int GetVolume(SetClientVolumeCommand command) => command.Volume;
}

/// <summary>
/// Validator for SetClientMuteCommand using base class.
/// </summary>
public class SetClientMuteCommandValidator : BaseClientCommandValidator<SetClientMuteCommand>
{
    protected override int GetClientId(SetClientMuteCommand command) => command.ClientId;

    protected override CommandSource GetSource(SetClientMuteCommand command) => command.Source;
}

/// <summary>
/// Validator for ToggleClientMuteCommand using base class.
/// </summary>
public class ToggleClientMuteCommandValidator : BaseClientCommandValidator<ToggleClientMuteCommand>
{
    protected override int GetClientId(ToggleClientMuteCommand command) => command.ClientId;

    protected override CommandSource GetSource(ToggleClientMuteCommand command) => command.Source;
}

/// <summary>
/// Validator for SetClientLatencyCommand using base class.
/// </summary>
public class SetClientLatencyCommandValidator : CompositeClientLatencyCommandValidator<SetClientLatencyCommand>
{
    protected override int GetClientId(SetClientLatencyCommand command) => command.ClientId;

    protected override CommandSource GetSource(SetClientLatencyCommand command) => command.Source;

    protected override int GetLatencyMs(SetClientLatencyCommand command) => command.LatencyMs;
}

/// <summary>
/// Validator for AssignClientToZoneCommand using base class.
/// </summary>
public class AssignClientToZoneCommandValidator : CompositeClientZoneAssignmentValidator<AssignClientToZoneCommand>
{
    protected override int GetClientId(AssignClientToZoneCommand command) => command.ClientId;

    protected override CommandSource GetSource(AssignClientToZoneCommand command) => command.Source;

    protected override int GetZoneId(AssignClientToZoneCommand command) => command.ZoneId;
}
