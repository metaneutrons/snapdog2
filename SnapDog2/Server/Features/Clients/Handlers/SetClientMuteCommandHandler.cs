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
/// Handles the SetClientMuteCommand.
/// </summary>
public partial class SetClientMuteCommandHandler : ICommandHandler<SetClientMuteCommand, Result>
{
    private readonly IClientManager _clientManager;
    private readonly ILogger<SetClientMuteCommandHandler> _logger;

    [LoggerMessage(3011, LogLevel.Information, "Setting mute for Client {ClientId} to {Enabled} from {Source}")]
    private partial void LogHandling(int clientId, bool enabled, CommandSource source);

    [LoggerMessage(3012, LogLevel.Warning, "Client {ClientId} not found for SetClientMuteCommand")]
    private partial void LogClientNotFound(int clientId);

    public SetClientMuteCommandHandler(
        IClientManager clientManager,
        ILogger<SetClientMuteCommandHandler> logger)
    {
        _clientManager = clientManager;
        _logger = logger;
    }

    public async Task<Result> Handle(SetClientMuteCommand request, CancellationToken cancellationToken)
    {
        LogHandling(request.ClientId, request.Enabled, request.Source);

        // Get the client
        var clientResult = await _clientManager.GetClientAsync(request.ClientId).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            LogClientNotFound(request.ClientId);
            return clientResult;
        }

        var client = clientResult.Value!;

        // Set the mute state
        var result = await client.SetMuteAsync(request.Enabled).ConfigureAwait(false);

        return result;
    }
}
