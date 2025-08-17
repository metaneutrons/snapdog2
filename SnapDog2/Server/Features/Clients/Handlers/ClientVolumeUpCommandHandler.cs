namespace SnapDog2.Server.Features.Clients.Commands.Volume;

using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Handler for ClientVolumeUpCommand.
/// </summary>
public partial class ClientVolumeUpCommandHandler : ICommandHandler<ClientVolumeUpCommand, Result>
{
    private readonly IClientManager _clientManager;
    private readonly ILogger<ClientVolumeUpCommandHandler> _logger;

    [LoggerMessage(3011, LogLevel.Information, "Increasing volume for Client {ClientIndex} by {Step} from {Source}")]
    private partial void LogHandling(int clientIndex, int step, CommandSource source);

    [LoggerMessage(3012, LogLevel.Warning, "Client {ClientIndex} not found for ClientVolumeUpCommand")]
    private partial void LogClientNotFound(int clientIndex);

    public ClientVolumeUpCommandHandler(IClientManager clientManager, ILogger<ClientVolumeUpCommandHandler> logger)
    {
        _clientManager = clientManager;
        _logger = logger;
    }

    public async Task<Result> Handle(ClientVolumeUpCommand request, CancellationToken cancellationToken)
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
        var newVolume = Math.Min(100, clientState.Volume + request.Step);

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
