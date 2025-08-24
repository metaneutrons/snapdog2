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
namespace SnapDog2.Server.Features.Snapcast.Handlers;

using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Snapcast.Commands;

/// <summary>
/// Handler for setting Snapcast client volume.
/// </summary>
public partial class SetSnapcastClientVolumeCommandHandler(
    ISnapcastService snapcastService,
    ILogger<SetSnapcastClientVolumeCommandHandler> logger
) : ICommandHandler<SetSnapcastClientVolumeCommand, Result>
{
    private readonly ISnapcastService _snapcastService = snapcastService;
    private readonly ILogger<SetSnapcastClientVolumeCommandHandler> _logger = logger;

    [LoggerMessage(
        3001,
        LogLevel.Information,
        "Setting volume for Snapcast client {ClientIndex} to {Volume}% (Source: {Source})"
    )]
    private partial void LogSettingClientVolume(string clientIndex, int volume, string source);

    [LoggerMessage(3002, LogLevel.Error, "Failed to set volume for Snapcast client {ClientIndex}")]
    private partial void LogSetClientVolumeFailed(string clientIndex, Exception ex);

    public async Task<Result> Handle(SetSnapcastClientVolumeCommand command, CancellationToken cancellationToken)
    {
        this.LogSettingClientVolume(command.ClientIndex, command.Volume, command.Source.ToString());

        try
        {
            var result = await this
                ._snapcastService.SetClientVolumeAsync(command.ClientIndex, command.Volume, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                this.LogSetClientVolumeFailed(
                    command.ClientIndex,
                    new InvalidOperationException(result.ErrorMessage ?? "Unknown error")
                );
                return Result.Failure(result.ErrorMessage ?? "Unknown error");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogSetClientVolumeFailed(command.ClientIndex, ex);
            return Result.Failure(ex);
        }
    }
}
