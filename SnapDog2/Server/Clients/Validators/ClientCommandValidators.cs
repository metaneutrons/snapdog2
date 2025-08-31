//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
namespace SnapDog2.Server.Clients.Validators;

using SnapDog2.Server.Clients.Commands.Config;
using SnapDog2.Server.Clients.Commands.Volume;
using SnapDog2.Server.Shared.Validators;
using SnapDog2.Shared.Enums;

/// <summary>
/// Validator for SetClientVolumeCommand using base class.
/// </summary>
public class SetClientVolumeCommandValidator : CompositeClientVolumeCommandValidator<SetClientVolumeCommand>
{
    protected override int GetClientIndex(SetClientVolumeCommand command) => command.ClientIndex;

    protected override CommandSource GetSource(SetClientVolumeCommand command) => command.Source;

    protected override int GetVolume(SetClientVolumeCommand command) => command.Volume;
}

/// <summary>
/// Validator for SetClientMuteCommand using base class.
/// </summary>
public class SetClientMuteCommandValidator : BaseClientCommandValidator<SetClientMuteCommand>
{
    protected override int GetClientIndex(SetClientMuteCommand command) => command.ClientIndex;

    protected override CommandSource GetSource(SetClientMuteCommand command) => command.Source;
}

/// <summary>
/// Validator for ToggleClientMuteCommand using base class.
/// </summary>
public class ToggleClientMuteCommandValidator : BaseClientCommandValidator<ToggleClientMuteCommand>
{
    protected override int GetClientIndex(ToggleClientMuteCommand command) => command.ClientIndex;

    protected override CommandSource GetSource(ToggleClientMuteCommand command) => command.Source;
}

/// <summary>
/// Validator for SetClientLatencyCommand using base class.
/// </summary>
public class SetClientLatencyCommandValidator : CompositeClientLatencyCommandValidator<SetClientLatencyCommand>
{
    protected override int GetClientIndex(SetClientLatencyCommand command) => command.ClientIndex;

    protected override CommandSource GetSource(SetClientLatencyCommand command) => command.Source;

    protected override int GetLatencyMs(SetClientLatencyCommand command) => command.LatencyMs;
}

/// <summary>
/// Validator for AssignClientToZoneCommand using base class.
/// </summary>
public class AssignClientToZoneCommandValidator : CompositeClientZoneAssignmentValidator<AssignClientToZoneCommand>
{
    protected override int GetClientIndex(AssignClientToZoneCommand command) => command.ClientIndex;

    protected override CommandSource GetSource(AssignClientToZoneCommand command) => command.Source;

    protected override int GetZoneIndex(AssignClientToZoneCommand command) => command.ZoneIndex;
}
