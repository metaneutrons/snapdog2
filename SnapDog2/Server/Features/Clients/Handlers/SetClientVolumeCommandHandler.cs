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
public partial class SetClientVolumeCommandHandler(
    IClientManager clientManager,
    ILogger<SetClientVolumeCommandHandler> logger
) : ICommandHandler<SetClientVolumeCommand, Result>
{
    private readonly IClientManager _clientManager = clientManager;
    private readonly ILogger<SetClientVolumeCommandHandler> _logger = logger;

    [LoggerMessage(3001, LogLevel.Information, "Setting volume for Client {ClientIndex} to {Volume} from {Source}")]
    private partial void LogHandling(int clientIndex, int volume, CommandSource source);

    [LoggerMessage(3002, LogLevel.Warning, "Client {ClientIndex} not found for SetClientVolumeCommand")]
    private partial void LogClientNotFound(int clientIndex);

    public async Task<Result> Handle(SetClientVolumeCommand request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ClientIndex, request.Volume, request.Source);

        // Get the client
        var clientResult = await this._clientManager.GetClientAsync(request.ClientIndex).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            this.LogClientNotFound(request.ClientIndex);
            return clientResult;
        }

        var client = clientResult.Value!;

        // Set the volume
        var result = await client.SetVolumeAsync(request.Volume).ConfigureAwait(false);

        return result;
    }
}
