namespace SnapDog2.Server.Features.Clients.Commands.Volume;

using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Handler for ClientVolumeDownCommand.
/// </summary>
public partial class ClientVolumeDownCommandHandler : ICommandHandler<ClientVolumeDownCommand, Result>
{
    private readonly IClientManager _clientManager;
    private readonly ILogger<ClientVolumeDownCommandHandler> _logger;

    [LoggerMessage(3021, LogLevel.Information, "Decreasing volume for Client {ClientIndex} by {Step} from {Source}")]
    private partial void LogHandling(int clientIndex, int step, CommandSource source);

    [LoggerMessage(3022, LogLevel.Warning, "Client {ClientIndex} not found for ClientVolumeDownCommand")]
    private partial void LogClientNotFound(int clientIndex);

    public ClientVolumeDownCommandHandler(IClientManager clientManager, ILogger<ClientVolumeDownCommandHandler> logger)
    {
        _clientManager = clientManager;
        _logger = logger;
    }

    public async Task<Result> Handle(ClientVolumeDownCommand request, CancellationToken cancellationToken)
    {
        LogHandling(request.ClientIndex, request.Step, request.Source);

        // Get the current client state
        var clientStateResult = await _clientManager.GetClientStateAsync(request.ClientIndex).ConfigureAwait(false);
        if (clientStateResult.IsFailure)
        {
            LogClientNotFound(request.ClientIndex);
            return clientStateResult;
        }

        var clientState = clientStateResult.Value!;
        var newVolume = Math.Max(0, clientState.Volume - request.Step);

        // Get the client for operations
        var clientResult = await _clientManager.GetClientAsync(request.ClientIndex).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            LogClientNotFound(request.ClientIndex);
            return clientResult;
        }

        var client = clientResult.Value!;

        // Set the new volume
        var result = await client.SetVolumeAsync(newVolume).ConfigureAwait(false);

        return result;
    }
}
