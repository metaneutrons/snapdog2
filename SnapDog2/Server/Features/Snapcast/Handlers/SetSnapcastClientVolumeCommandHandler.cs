namespace SnapDog2.Server.Features.Snapcast.Handlers;

using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Snapcast.Commands;

/// <summary>
/// Handler for setting Snapcast client volume.
/// </summary>
public partial class SetSnapcastClientVolumeCommandHandler : ICommandHandler<SetSnapcastClientVolumeCommand, Result>
{
    private readonly ISnapcastService _snapcastService;
    private readonly ILogger<SetSnapcastClientVolumeCommandHandler> _logger;

    public SetSnapcastClientVolumeCommandHandler(
        ISnapcastService snapcastService,
        ILogger<SetSnapcastClientVolumeCommandHandler> logger
    )
    {
        this._snapcastService = snapcastService;
        this._logger = logger;
    }

    [LoggerMessage(
        3001,
        LogLevel.Information,
        "Setting volume for Snapcast client {ClientId} to {Volume}% (Source: {Source})"
    )]
    private partial void LogSettingClientVolume(string clientId, int volume, string source);

    [LoggerMessage(3002, LogLevel.Error, "Failed to set volume for Snapcast client {ClientId}")]
    private partial void LogSetClientVolumeFailed(string clientId, Exception ex);

    public async Task<Result> Handle(SetSnapcastClientVolumeCommand command, CancellationToken cancellationToken)
    {
        this.LogSettingClientVolume(command.ClientId, command.Volume, command.Source.ToString());

        try
        {
            var result = await this
                ._snapcastService.SetClientVolumeAsync(command.ClientId, command.Volume, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                this.LogSetClientVolumeFailed(
                    command.ClientId,
                    new InvalidOperationException(result.ErrorMessage ?? "Unknown error")
                );
                return Result.Failure(result.ErrorMessage ?? "Unknown error");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogSetClientVolumeFailed(command.ClientId, ex);
            return Result.Failure(ex);
        }
    }
}
