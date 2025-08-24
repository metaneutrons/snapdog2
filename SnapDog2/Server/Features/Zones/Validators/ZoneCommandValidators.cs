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
namespace SnapDog2.Server.Features.Zones.Validators;

using FluentValidation;
using SnapDog2.Core.Enums;
using SnapDog2.Server.Features.Shared.Validators;
using SnapDog2.Server.Features.Zones.Commands.Playback;
using SnapDog2.Server.Features.Zones.Commands.Playlist;
using SnapDog2.Server.Features.Zones.Commands.Track;
using SnapDog2.Server.Features.Zones.Commands.Volume;

/// <summary>
/// Validator for SetZoneVolumeCommand using base class.
/// </summary>
public class SetZoneVolumeCommandValidator : CompositeZoneVolumeCommandValidator<SetZoneVolumeCommand>
{
    protected override int GetZoneIndex(SetZoneVolumeCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(SetZoneVolumeCommand command) => command.Source;

    protected override int GetVolume(SetZoneVolumeCommand command) => command.Volume;
}

/// <summary>
/// Validator for VolumeUpCommand using base class.
/// </summary>
public class VolumeUpCommandValidator : CompositeZoneVolumeStepCommandValidator<VolumeUpCommand>
{
    protected override int GetZoneIndex(VolumeUpCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(VolumeUpCommand command) => command.Source;

    protected override int GetStep(VolumeUpCommand command) => command.Step;
}

/// <summary>
/// Validator for VolumeDownCommand using base class.
/// </summary>
public class VolumeDownCommandValidator : CompositeZoneVolumeStepCommandValidator<VolumeDownCommand>
{
    protected override int GetZoneIndex(VolumeDownCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(VolumeDownCommand command) => command.Source;

    protected override int GetStep(VolumeDownCommand command) => command.Step;
}

/// <summary>
/// Validator for SetZoneMuteCommand using base class.
/// </summary>
public class SetZoneMuteCommandValidator : BaseZoneCommandValidator<SetZoneMuteCommand>
{
    protected override int GetZoneIndex(SetZoneMuteCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(SetZoneMuteCommand command) => command.Source;
}

/// <summary>
/// Validator for ToggleZoneMuteCommand using base class.
/// </summary>
public class ToggleZoneMuteCommandValidator : BaseZoneCommandValidator<ToggleZoneMuteCommand>
{
    protected override int GetZoneIndex(ToggleZoneMuteCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(ToggleZoneMuteCommand command) => command.Source;
}

/// <summary>
/// Validator for SetTrackCommand using base class.
/// </summary>
public class SetTrackCommandValidator : CompositeZoneTrackCommandValidator<SetTrackCommand>
{
    protected override int GetZoneIndex(SetTrackCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(SetTrackCommand command) => command.Source;

    protected override int GetTrackIndex(SetTrackCommand command) => command.TrackIndex;
}

/// <summary>
/// Validator for NextTrackCommand using base class.
/// </summary>
public class NextTrackCommandValidator : BaseZoneCommandValidator<NextTrackCommand>
{
    protected override int GetZoneIndex(NextTrackCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(NextTrackCommand command) => command.Source;
}

/// <summary>
/// Validator for PreviousTrackCommand using base class.
/// </summary>
public class PreviousTrackCommandValidator : BaseZoneCommandValidator<PreviousTrackCommand>
{
    protected override int GetZoneIndex(PreviousTrackCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(PreviousTrackCommand command) => command.Source;
}

/// <summary>
/// Validator for SetTrackRepeatCommand using base class.
/// </summary>
public class SetTrackRepeatCommandValidator : BaseZoneCommandValidator<SetTrackRepeatCommand>
{
    protected override int GetZoneIndex(SetTrackRepeatCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(SetTrackRepeatCommand command) => command.Source;
}

/// <summary>
/// Validator for ToggleTrackRepeatCommand using base class.
/// </summary>
public class ToggleTrackRepeatCommandValidator : BaseZoneCommandValidator<ToggleTrackRepeatCommand>
{
    protected override int GetZoneIndex(ToggleTrackRepeatCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(ToggleTrackRepeatCommand command) => command.Source;
}

/// <summary>
/// Validator for SetPlaylistCommand using base class.
/// </summary>
public class SetPlaylistCommandValidator : CompositeZonePlaylistCommandValidator<SetPlaylistCommand>
{
    protected override int GetZoneIndex(SetPlaylistCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(SetPlaylistCommand command) => command.Source;

    protected override int GetPlaylistIndex(SetPlaylistCommand command) => command.PlaylistIndex;
}

/// <summary>
/// Validator for NextPlaylistCommand using base class.
/// </summary>
public class NextPlaylistCommandValidator : BaseZoneCommandValidator<NextPlaylistCommand>
{
    protected override int GetZoneIndex(NextPlaylistCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(NextPlaylistCommand command) => command.Source;
}

/// <summary>
/// Validator for PreviousPlaylistCommand using base class.
/// </summary>
public class PreviousPlaylistCommandValidator : BaseZoneCommandValidator<PreviousPlaylistCommand>
{
    protected override int GetZoneIndex(PreviousPlaylistCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(PreviousPlaylistCommand command) => command.Source;
}

/// <summary>
/// Validator for SetPlaylistShuffleCommand using base class.
/// </summary>
public class SetPlaylistShuffleCommandValidator : BaseZoneCommandValidator<SetPlaylistShuffleCommand>
{
    protected override int GetZoneIndex(SetPlaylistShuffleCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(SetPlaylistShuffleCommand command) => command.Source;
}

/// <summary>
/// Validator for TogglePlaylistShuffleCommand using base class.
/// </summary>
public class TogglePlaylistShuffleCommandValidator : BaseZoneCommandValidator<TogglePlaylistShuffleCommand>
{
    protected override int GetZoneIndex(TogglePlaylistShuffleCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(TogglePlaylistShuffleCommand command) => command.Source;
}

/// <summary>
/// Validator for SetPlaylistRepeatCommand using base class.
/// </summary>
public class SetPlaylistRepeatCommandValidator : BaseZoneCommandValidator<SetPlaylistRepeatCommand>
{
    protected override int GetZoneIndex(SetPlaylistRepeatCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(SetPlaylistRepeatCommand command) => command.Source;
}

/// <summary>
/// Validator for TogglePlaylistRepeatCommand using base class.
/// </summary>
public class TogglePlaylistRepeatCommandValidator : BaseZoneCommandValidator<TogglePlaylistRepeatCommand>
{
    protected override int GetZoneIndex(TogglePlaylistRepeatCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(TogglePlaylistRepeatCommand command) => command.Source;
}

/// <summary>
/// Validator for PlayCommand with additional validation for optional parameters.
/// </summary>
public class PlayCommandValidator : BaseZoneCommandValidator<PlayCommand>
{
    public PlayCommandValidator()
    {
        // Additional validation for optional track index
        When(
            x => x.TrackIndex.HasValue,
            () =>
            {
                RuleFor(x => x.TrackIndex!.Value)
                    .GreaterThan(0)
                    .WithMessage("Track index must be a positive integer (1-based) when specified.");
            }
        );

        // Additional validation for optional media URL
        When(
            x => !string.IsNullOrEmpty(x.MediaUrl),
            () =>
            {
                RuleFor(x => x.MediaUrl).Must(BeValidUrl).WithMessage("Media URL must be a valid URL when specified.");
            }
        );
    }

    protected override int GetZoneIndex(PlayCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(PlayCommand command) => command.Source;

    private static bool BeValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
            && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

/// <summary>
/// Validator for PauseCommand using base class.
/// </summary>
public class PauseCommandValidator : BaseZoneCommandValidator<PauseCommand>
{
    protected override int GetZoneIndex(PauseCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(PauseCommand command) => command.Source;
}

/// <summary>
/// Validator for StopCommand using base class.
/// </summary>
public class StopCommandValidator : BaseZoneCommandValidator<StopCommand>
{
    protected override int GetZoneIndex(StopCommand command) => command.ZoneIndex;

    protected override CommandSource GetSource(StopCommand command) => command.Source;
}
