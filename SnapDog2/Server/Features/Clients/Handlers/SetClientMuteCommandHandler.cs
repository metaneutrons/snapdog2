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
/// Handles the SetClientMuteCommand.
/// </summary>
public partial class SetClientMuteCommandHandler : ICommandHandler<SetClientMuteCommand, Result>
{
    private readonly IClientManager _clientManager;
    private readonly ILogger<SetClientMuteCommandHandler> _logger;

    [LoggerMessage(3011, LogLevel.Information, "Setting mute for Client {ClientIndex} to {Enabled} from {Source}")]
    private partial void LogHandling(int clientIndex, bool enabled, CommandSource source);

    [LoggerMessage(3012, LogLevel.Warning, "Client {ClientIndex} not found for SetClientMuteCommand")]
    private partial void LogClientNotFound(int clientIndex);

    public SetClientMuteCommandHandler(IClientManager clientManager, ILogger<SetClientMuteCommandHandler> logger)
    {
        this._clientManager = clientManager;
        this._logger = logger;
    }

    public async Task<Result> Handle(SetClientMuteCommand request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ClientIndex, request.Enabled, request.Source);

        // Get the client
        var clientResult = await this._clientManager.GetClientAsync(request.ClientIndex).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            this.LogClientNotFound(request.ClientIndex);
            return clientResult;
        }

        var client = clientResult.Value!;

        // Set the mute state
        var result = await client.SetMuteAsync(request.Enabled).ConfigureAwait(false);

        return result;
    }
}
