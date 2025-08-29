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
namespace SnapDog2.Server.Features.Snapcast.Validators;

using FluentValidation;
using SnapDog2.Server.Features.Snapcast.Commands;

/// <summary>
/// Validator for SetSnapcastClientVolumeCommand.
/// </summary>
public class SetSnapcastClientVolumeCommandValidator : AbstractValidator<SetSnapcastClientVolumeCommand>
{
    public SetSnapcastClientVolumeCommandValidator()
    {
        this.RuleFor(x => x.ClientIndex).NotEmpty().WithMessage("Client Index is required");

        this.RuleFor(x => x.Volume).InclusiveBetween(0, 100).WithMessage("Volume must be between 0 and 100");
    }
}
