namespace SnapDog2.Server.Features.Clients.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Commands;

/// <summary>
/// Handles the SetClientLatencyCommand.
/// </summary>
public partial class SetClientLatencyCommandHandler : ICommandHandler<SetClientLatencyCommand, Result>
{
    private readonly IClientManager _clientManager;
    private readonly ILogger<SetClientLatencyCommandHandler> _logger;

    [LoggerMessage(3201, LogLevel.Information, "Setting latency for Client {ClientId} to {LatencyMs}ms from {Source}")]
    private partial void LogHandling(int clientId, int latencyMs, CommandSource source);

    [LoggerMessage(3202, LogLevel.Warning, "Client {ClientId} not found for SetClientLatencyCommand")]
    private partial void LogClientNotFound(int clientId);

    public SetClientLatencyCommandHandler(IClientManager clientManager, ILogger<SetClientLatencyCommandHandler> logger)
    {
        _clientManager = clientManager;
        _logger = logger;
    }

    public async Task<Result> Handle(SetClientLatencyCommand request, CancellationToken cancellationToken)
    {
        LogHandling(request.ClientId, request.LatencyMs, request.Source);

        // Get the client
        var clientResult = await _clientManager.GetClientAsync(request.ClientId).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            LogClientNotFound(request.ClientId);
            return clientResult;
        }

        var client = clientResult.Value!;

        // Set the latency
        var result = await client.SetLatencyAsync(request.LatencyMs).ConfigureAwait(false);

        return result;
    }
}
