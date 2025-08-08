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
/// Handles the SetClientVolumeCommand.
/// </summary>
public partial class SetClientVolumeCommandHandler : ICommandHandler<SetClientVolumeCommand, Result>
{
    private readonly IClientManager _clientManager;
    private readonly ILogger<SetClientVolumeCommandHandler> _logger;

    [LoggerMessage(3001, LogLevel.Information, "Setting volume for Client {ClientId} to {Volume} from {Source}")]
    private partial void LogHandling(int clientId, int volume, CommandSource source);

    [LoggerMessage(3002, LogLevel.Warning, "Client {ClientId} not found for SetClientVolumeCommand")]
    private partial void LogClientNotFound(int clientId);

    public SetClientVolumeCommandHandler(IClientManager clientManager, ILogger<SetClientVolumeCommandHandler> logger)
    {
        this._clientManager = clientManager;
        this._logger = logger;
    }

    public async Task<Result> Handle(SetClientVolumeCommand request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ClientId, request.Volume, request.Source);

        // Get the client
        var clientResult = await this._clientManager.GetClientAsync(request.ClientId).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            this.LogClientNotFound(request.ClientId);
            return clientResult;
        }

        var client = clientResult.Value!;

        // Set the volume
        var result = await client.SetVolumeAsync(request.Volume).ConfigureAwait(false);

        return result;
    }
}
