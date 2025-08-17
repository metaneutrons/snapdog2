namespace SnapDog2.Server.Features.Snapcast.Handlers;

using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Snapcast.Commands;

/// <summary>
/// Handler for setting Snapcast client latency.
/// </summary>
public partial class SetSnapcastClientLatencyCommandHandler(
    ISnapcastService snapcastService,
    ILogger<SetSnapcastClientLatencyCommandHandler> logger
) : ICommandHandler<SetSnapcastClientLatencyCommand, Result>
{
    private readonly ISnapcastService _snapcastService = snapcastService;
    private readonly ILogger<SetSnapcastClientLatencyCommandHandler> _logger = logger;

    [LoggerMessage(
        3005,
        LogLevel.Information,
        "Setting latency for Snapcast client {ClientIndex} to {LatencyMs}ms (Source: {Source})"
    )]
    private partial void LogSettingClientLatency(string clientIndex, int latencyMs, string source);

    [LoggerMessage(3006, LogLevel.Error, "Failed to set latency for Snapcast client {ClientIndex}")]
    private partial void LogSetClientLatencyFailed(string clientIndex, Exception ex);

    public async Task<Result> Handle(SetSnapcastClientLatencyCommand command, CancellationToken cancellationToken)
    {
        this.LogSettingClientLatency(command.ClientIndex, command.LatencyMs, command.Source.ToString());

        try
        {
            var result = await this
                ._snapcastService.SetClientLatencyAsync(command.ClientIndex, command.LatencyMs, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                this.LogSetClientLatencyFailed(
                    command.ClientIndex,
                    new InvalidOperationException(result.ErrorMessage ?? "Unknown error")
                );
                return Result.Failure(result.ErrorMessage ?? "Unknown error");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogSetClientLatencyFailed(command.ClientIndex, ex);
            return Result.Failure(ex);
        }
    }
}
