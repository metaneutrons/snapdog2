namespace SnapDog2.Server.Features.Clients.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Commands.Config;
using SnapDog2.Server.Features.Clients.Notifications;

/// <summary>
/// Handles the AssignClientToZoneCommand.
/// </summary>
public partial class AssignClientToZoneCommandHandler(
    IClientManager clientManager,
    IZoneManager zoneManager,
    IMediator mediator,
    ILogger<AssignClientToZoneCommandHandler> logger
) : ICommandHandler<AssignClientToZoneCommand, Result>
{
    private readonly IClientManager _clientManager = clientManager;
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<AssignClientToZoneCommandHandler> _logger = logger;

    [LoggerMessage(3101, LogLevel.Information, "Assigning Client {ClientIndex} to Zone {ZoneIndex} from {Source}")]
    private partial void LogHandling(int clientIndex, int zoneIndex, CommandSource source);

    [LoggerMessage(3102, LogLevel.Warning, "Client {ClientIndex} not found for AssignClientToZoneCommand")]
    private partial void LogClientNotFound(int clientIndex);

    [LoggerMessage(3103, LogLevel.Warning, "Zone {ZoneIndex} not found for AssignClientToZoneCommand")]
    private partial void LogZoneNotFound(int zoneIndex);

    public async Task<Result> Handle(AssignClientToZoneCommand request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ClientIndex, request.ZoneIndex, request.Source);

        // Get the current client state to capture the previous zone assignment
        var clientStateResult = await this
            ._clientManager.GetClientAsync(request.ClientIndex, cancellationToken)
            .ConfigureAwait(false);
        if (clientStateResult.IsFailure)
        {
            this.LogClientNotFound(request.ClientIndex);
            return clientStateResult;
        }

        var previousZoneIndex = clientStateResult.Value!.ZoneIndex;

        // Validate client exists (get IClient for the assignment operation)
        var clientResult = await this._clientManager.GetClientAsync(request.ClientIndex).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            this.LogClientNotFound(request.ClientIndex);
            return clientResult;
        }

        var client = clientResult.Value!;

        // Validate zone exists
        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex);
            return zoneResult;
        }

        // Use the IClient method directly
        var result = await client.AssignToZoneAsync(request.ZoneIndex).ConfigureAwait(false);

        // If the assignment was successful, publish the notification for event-driven zone grouping
        if (result.IsSuccess)
        {
            var notification = new ClientZoneAssignmentChangedNotification
            {
                ClientIndex = request.ClientIndex,
                ZoneIndex = request.ZoneIndex,
                PreviousZoneIndex = previousZoneIndex,
            };

            // Publish the notification asynchronously to trigger immediate zone grouping
            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        await this._mediator.PublishAsync(notification, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogError(
                            ex,
                            "Failed to publish ClientZoneAssignmentChangedNotification for client {ClientIndex}",
                            request.ClientIndex
                        );
                    }
                },
                cancellationToken
            );
        }

        return result;
    }
}
