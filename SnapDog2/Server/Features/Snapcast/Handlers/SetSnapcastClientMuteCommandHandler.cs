namespace SnapDog2.Server.Features.Snapcast.Handlers;

using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Snapcast.Commands;

/// <summary>
/// Handler for setting Snapcast client mute state.
/// </summary>
public partial class SetSnapcastClientMuteCommandHandler(
    ISnapcastService snapcastService,
    ILogger<SetSnapcastClientMuteCommandHandler> logger
) : ICommandHandler<SetSnapcastClientMuteCommand, Result>
{
    private readonly ISnapcastService _snapcastService = snapcastService;
    private readonly ILogger<SetSnapcastClientMuteCommandHandler> _logger = logger;

    [LoggerMessage(
        3003,
        LogLevel.Information,
        "Setting mute for Snapcast client {ClientIndex} to {Muted} (Source: {Source})"
    )]
    private partial void LogSettingClientMute(string clientIndex, bool muted, string source);

    [LoggerMessage(3004, LogLevel.Error, "Failed to set mute for Snapcast client {ClientIndex}")]
    private partial void LogSetClientMuteFailed(string clientIndex, Exception ex);

    public async Task<Result> Handle(SetSnapcastClientMuteCommand command, CancellationToken cancellationToken)
    {
        this.LogSettingClientMute(command.ClientIndex, command.Muted, command.Source.ToString());

        try
        {
            var result = await this
                ._snapcastService.SetClientMuteAsync(command.ClientIndex, command.Muted, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                this.LogSetClientMuteFailed(
                    command.ClientIndex,
                    new InvalidOperationException(result.ErrorMessage ?? "Unknown error")
                );
                return Result.Failure(result.ErrorMessage ?? "Unknown error");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogSetClientMuteFailed(command.ClientIndex, ex);
            return Result.Failure(ex);
        }
    }
}
