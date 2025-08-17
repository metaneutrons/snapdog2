namespace SnapDog2.Server.Features.Clients.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Commands.Volume;

/// <summary>
/// Handles the ToggleClientMuteCommand.
/// </summary>
public partial class ToggleClientMuteCommandHandler(
    IClientManager clientManager,
    ILogger<ToggleClientMuteCommandHandler> logger
) : ICommandHandler<ToggleClientMuteCommand, Result>
{
    private readonly IClientManager _clientManager = clientManager;
    private readonly ILogger<ToggleClientMuteCommandHandler> _logger = logger;

    [LoggerMessage(3021, LogLevel.Information, "Toggling mute for Client {ClientIndex} from {Source}")]
    private partial void LogHandling(int clientIndex, CommandSource source);

    [LoggerMessage(3022, LogLevel.Warning, "Client {ClientIndex} not found for ToggleClientMuteCommand")]
    private partial void LogClientNotFound(int clientIndex);

    [LoggerMessage(3023, LogLevel.Information, "Toggled mute for Client {ClientIndex} to {NewMuteState}")]
    private partial void LogToggleResult(int clientIndex, bool newMuteState);

    public async Task<Result> Handle(ToggleClientMuteCommand request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ClientIndex, request.Source);

        // Get the client for operations
        var clientResult = await this._clientManager.GetClientAsync(request.ClientIndex).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            this.LogClientNotFound(request.ClientIndex);
            return clientResult;
        }

        var client = clientResult.Value!;

        // Use the IClient method directly
        var result = await client.ToggleMuteAsync().ConfigureAwait(false);

        if (result.IsSuccess)
        {
            // Note: The actual new mute state will be logged by the IClient method
            // We could get the current state if needed for logging
        }

        return result;
    }
}
