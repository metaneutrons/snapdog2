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
/// Handles the ToggleClientMuteCommand.
/// </summary>
public partial class ToggleClientMuteCommandHandler : ICommandHandler<ToggleClientMuteCommand, Result>
{
    private readonly IClientManager _clientManager;
    private readonly ILogger<ToggleClientMuteCommandHandler> _logger;

    [LoggerMessage(3021, LogLevel.Information, "Toggling mute for Client {ClientId} from {Source}")]
    private partial void LogHandling(int clientId, CommandSource source);

    [LoggerMessage(3022, LogLevel.Warning, "Client {ClientId} not found for ToggleClientMuteCommand")]
    private partial void LogClientNotFound(int clientId);

    [LoggerMessage(3023, LogLevel.Information, "Toggled mute for Client {ClientId} to {NewMuteState}")]
    private partial void LogToggleResult(int clientId, bool newMuteState);

    public ToggleClientMuteCommandHandler(IClientManager clientManager, ILogger<ToggleClientMuteCommandHandler> logger)
    {
        this._clientManager = clientManager;
        this._logger = logger;
    }

    public async Task<Result> Handle(ToggleClientMuteCommand request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ClientId, request.Source);

        // Get the current client state
        var clientStateResult = await this._clientManager.GetClientStateAsync(request.ClientId).ConfigureAwait(false);
        if (clientStateResult.IsFailure)
        {
            this.LogClientNotFound(request.ClientId);
            return clientStateResult;
        }

        var clientState = clientStateResult.Value!;
        var newMuteState = !clientState.Mute;

        // Get the client for operations
        var clientResult = await this._clientManager.GetClientAsync(request.ClientId).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            this.LogClientNotFound(request.ClientId);
            return clientResult;
        }

        var client = clientResult.Value!;

        // Toggle the mute state
        var result = await client.SetMuteAsync(newMuteState).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            this.LogToggleResult(request.ClientId, newMuteState);
        }

        return result;
    }
}
