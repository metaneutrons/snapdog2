namespace SnapDog2.Server.Features.Clients.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Commands.Config;

/// <summary>
/// Handles the SetClientLatencyCommand.
/// </summary>
public partial class SetClientLatencyCommandHandler(
    IClientManager clientManager,
    ILogger<SetClientLatencyCommandHandler> logger
) : ICommandHandler<SetClientLatencyCommand, Result>
{
    private readonly IClientManager _clientManager = clientManager;
    private readonly ILogger<SetClientLatencyCommandHandler> _logger = logger;

    [LoggerMessage(
        3201,
        LogLevel.Information,
        "Setting latency for Client {ClientIndex} to {LatencyMs}ms from {Source}"
    )]
    private partial void LogHandling(int clientIndex, int latencyMs, CommandSource source);

    [LoggerMessage(3202, LogLevel.Warning, "Client {ClientIndex} not found for SetClientLatencyCommand")]
    private partial void LogClientNotFound(int clientIndex);

    public async Task<Result> Handle(SetClientLatencyCommand request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ClientIndex, request.LatencyMs, request.Source);

        // Get the client
        var clientResult = await this._clientManager.GetClientAsync(request.ClientIndex).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            this.LogClientNotFound(request.ClientIndex);
            return clientResult;
        }

        var client = clientResult.Value!;

        // Set the latency
        var result = await client.SetLatencyAsync(request.LatencyMs).ConfigureAwait(false);

        return result;
    }
}
