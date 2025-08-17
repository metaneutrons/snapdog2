namespace SnapDog2.Server.Features.Clients.Commands.Volume;

using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Handler for ClientVolumeUpCommand.
/// </summary>
public partial class ClientVolumeUpCommandHandler(
    IClientManager clientManager,
    ILogger<ClientVolumeUpCommandHandler> logger
) : ICommandHandler<ClientVolumeUpCommand, Result>
{
    private readonly IClientManager _clientManager = clientManager;
    private readonly ILogger<ClientVolumeUpCommandHandler> _logger = logger;

    [LoggerMessage(3011, LogLevel.Information, "Increasing volume for Client {ClientIndex} by {Step} from {Source}")]
    private partial void LogHandling(int clientIndex, int step, CommandSource source);

    [LoggerMessage(3012, LogLevel.Warning, "Client {ClientIndex} not found for ClientVolumeUpCommand")]
    private partial void LogClientNotFound(int clientIndex);

    public async Task<Result> Handle(ClientVolumeUpCommand request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ClientIndex, request.Step, request.Source);

        // Get the client for operations
        var clientResult = await this._clientManager.GetClientAsync(request.ClientIndex).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            this.LogClientNotFound(request.ClientIndex);
            return clientResult;
        }

        var client = clientResult.Value!;

        // Use the IClient method directly
        var result = await client.VolumeUpAsync(request.Step).ConfigureAwait(false);

        return result;
    }
}
