namespace SnapDog2.Controllers;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SnapDog2.Api.Models;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Commands;
using SnapDog2.Server.Features.Clients.Queries;

/// <summary>
/// Controller for client management operations.
/// Follows CQRS pattern using Cortex.Mediator for enterprise-grade architecture compliance.
/// </summary>
[ApiController]
[Route("api/clients")]
[Produces("application/json")]
public class ClientController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ClientController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientController"/> class.
    /// </summary>
    /// <param name="mediator">The Cortex.Mediator instance for CQRS command/query dispatch.</param>
    /// <param name="logger">The logger instance.</param>
    public ClientController(IMediator mediator, ILogger<ClientController> logger)
    {
        this._mediator = mediator;
        this._logger = logger;
    }

    /// <summary>
    /// Gets the current state of a specific client.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The client state wrapped in ApiResponse.</returns>
    [HttpGet("{clientId:int}/state")]
    [ProducesResponseType(typeof(ApiResponse<ClientState>), 200)]
    [ProducesResponseType(typeof(ApiResponse<ClientState>), 404)]
    [ProducesResponseType(typeof(ApiResponse<ClientState>), 500)]
    public async Task<ActionResult<ApiResponse<ClientState>>> GetClientState(
        [Range(1, int.MaxValue)] int clientId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Getting client state for client {ClientId} via CQRS mediator", clientId);

            var query = new GetClientQuery { ClientId = clientId };
            var result = await this._mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(
                query,
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<ClientState>.CreateSuccess(result.Value));
            }

            this._logger.LogWarning(
                "Failed to get client state for client {ClientId}: {Error}",
                clientId,
                result.ErrorMessage
            );
            return this.NotFound(
                ApiResponse<ClientState>.CreateError("CLIENT_NOT_FOUND", result.ErrorMessage ?? "Client not found")
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error getting client state for client {ClientId}", clientId);
            return this.StatusCode(
                500,
                ApiResponse<ClientState>.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    /// <summary>
    /// Gets the states of all clients.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Collection of client states wrapped in ApiResponse.</returns>
    [HttpGet("states")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ClientState>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ClientState>>), 500)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ClientState>>>> GetAllClientStates(
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Getting all client states via CQRS mediator");

            var query = new GetAllClientsQuery();
            var result = await this._mediator.SendQueryAsync<GetAllClientsQuery, Result<List<ClientState>>>(
                query,
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<IEnumerable<ClientState>>.CreateSuccess(result.Value));
            }

            this._logger.LogWarning("Failed to get all client states: {Error}", result.ErrorMessage);
            return this.StatusCode(
                500,
                ApiResponse<IEnumerable<ClientState>>.CreateError(
                    "CLIENT_STATES_ERROR",
                    result.ErrorMessage ?? "Failed to retrieve client states"
                )
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error getting all client states");
            return this.StatusCode(
                500,
                ApiResponse<IEnumerable<ClientState>>.CreateError(
                    "INTERNAL_ERROR",
                    "An internal server error occurred",
                    ex.Message
                )
            );
        }
    }

    /// <summary>
    /// Gets clients assigned to a specific zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Collection of client states for the zone wrapped in ApiResponse.</returns>
    [HttpGet("by-zone/{zoneId:int}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ClientState>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ClientState>>), 404)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ClientState>>), 500)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ClientState>>>> GetClientsByZone(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Getting clients for zone {ZoneId} via CQRS mediator", zoneId);

            var query = new GetClientsByZoneQuery { ZoneId = zoneId };
            var result = await this._mediator.SendQueryAsync<GetClientsByZoneQuery, Result<List<ClientState>>>(
                query,
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<IEnumerable<ClientState>>.CreateSuccess(result.Value));
            }

            this._logger.LogWarning("Failed to get clients for zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return this.NotFound(
                ApiResponse<IEnumerable<ClientState>>.CreateError(
                    "ZONE_NOT_FOUND",
                    result.ErrorMessage ?? "Zone not found or no clients assigned"
                )
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error getting clients for zone {ZoneId}", zoneId);
            return this.StatusCode(
                500,
                ApiResponse<IEnumerable<ClientState>>.CreateError(
                    "INTERNAL_ERROR",
                    "An internal server error occurred",
                    ex.Message
                )
            );
        }
    }

    /// <summary>
    /// Sets the volume for a specific client.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="request">The volume request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success or error response wrapped in ApiResponse.</returns>
    [HttpPost("{clientId:int}/volume")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> SetClientVolume(
        [Range(1, int.MaxValue)] int clientId,
        [FromBody] ClientVolumeRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug(
                "Setting volume for client {ClientId} to {Volume} via CQRS mediator",
                clientId,
                request.Volume
            );

            var command = new SetClientVolumeCommand
            {
                ClientId = clientId,
                Volume = request.Volume,
                Source = CommandSource.Api,
            };

            var result = await this._mediator.SendCommandAsync<SetClientVolumeCommand, Result>(
                command,
                cancellationToken
            );

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this._logger.LogWarning(
                "Failed to set volume for client {ClientId}: {Error}",
                clientId,
                result.ErrorMessage
            );
            return this.BadRequest(
                ApiResponse.CreateError("VOLUME_SET_ERROR", result.ErrorMessage ?? "Failed to set volume")
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error setting volume for client {ClientId}", clientId);
            return this.StatusCode(
                500,
                ApiResponse.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    /// <summary>
    /// Sets the mute state for a specific client.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="request">The mute request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success or error response wrapped in ApiResponse.</returns>
    [HttpPost("{clientId:int}/mute")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> SetClientMute(
        [Range(1, int.MaxValue)] int clientId,
        [FromBody] ClientMuteRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug(
                "Setting mute for client {ClientId} to {Enabled} via CQRS mediator",
                clientId,
                request.Enabled
            );

            var command = new SetClientMuteCommand
            {
                ClientId = clientId,
                Enabled = request.Enabled,
                Source = CommandSource.Api,
            };

            var result = await this._mediator.SendCommandAsync<SetClientMuteCommand, Result>(
                command,
                cancellationToken
            );

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this._logger.LogWarning("Failed to set mute for client {ClientId}: {Error}", clientId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("MUTE_SET_ERROR", result.ErrorMessage ?? "Failed to set mute state")
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error setting mute for client {ClientId}", clientId);
            return this.StatusCode(
                500,
                ApiResponse.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    /// <summary>
    /// Toggles the mute state for a specific client.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success or error response wrapped in ApiResponse.</returns>
    [HttpPost("{clientId:int}/toggle-mute")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> ToggleClientMute(
        [Range(1, int.MaxValue)] int clientId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Toggling mute for client {ClientId} via CQRS mediator", clientId);

            var command = new ToggleClientMuteCommand { ClientId = clientId, Source = CommandSource.Api };

            var result = await this._mediator.SendCommandAsync<ToggleClientMuteCommand, Result>(
                command,
                cancellationToken
            );

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this._logger.LogWarning(
                "Failed to toggle mute for client {ClientId}: {Error}",
                clientId,
                result.ErrorMessage
            );
            return this.BadRequest(
                ApiResponse.CreateError("MUTE_TOGGLE_ERROR", result.ErrorMessage ?? "Failed to toggle mute state")
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error toggling mute for client {ClientId}", clientId);
            return this.StatusCode(
                500,
                ApiResponse.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    /// <summary>
    /// Sets the latency for a specific client.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="request">The latency request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success or error response wrapped in ApiResponse.</returns>
    [HttpPost("{clientId:int}/latency")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> SetClientLatency(
        [Range(1, int.MaxValue)] int clientId,
        [FromBody] ClientLatencyRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug(
                "Setting latency for client {ClientId} to {LatencyMs}ms via CQRS mediator",
                clientId,
                request.LatencyMs
            );

            var command = new SetClientLatencyCommand
            {
                ClientId = clientId,
                LatencyMs = request.LatencyMs,
                Source = CommandSource.Api,
            };

            var result = await this._mediator.SendCommandAsync<SetClientLatencyCommand, Result>(
                command,
                cancellationToken
            );

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this._logger.LogWarning(
                "Failed to set latency for client {ClientId}: {Error}",
                clientId,
                result.ErrorMessage
            );
            return this.BadRequest(
                ApiResponse.CreateError("LATENCY_SET_ERROR", result.ErrorMessage ?? "Failed to set latency")
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error setting latency for client {ClientId}", clientId);
            return this.StatusCode(
                500,
                ApiResponse.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    /// <summary>
    /// Assigns a client to a zone.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="request">The zone assignment request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success or error response wrapped in ApiResponse.</returns>
    [HttpPost("{clientId:int}/assign-zone")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> AssignClientToZone(
        [Range(1, int.MaxValue)] int clientId,
        [FromBody] ZoneAssignmentRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug(
                "Assigning client {ClientId} to zone {ZoneId} via CQRS mediator",
                clientId,
                request.ZoneId
            );

            var command = new AssignClientToZoneCommand
            {
                ClientId = clientId,
                ZoneId = request.ZoneId,
                Source = CommandSource.Api,
            };

            var result = await this._mediator.SendCommandAsync<AssignClientToZoneCommand, Result>(
                command,
                cancellationToken
            );

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this._logger.LogWarning(
                "Failed to assign client {ClientId} to zone {ZoneId}: {Error}",
                clientId,
                request.ZoneId,
                result.ErrorMessage
            );
            return this.BadRequest(
                ApiResponse.CreateError(
                    "ZONE_ASSIGNMENT_ERROR",
                    result.ErrorMessage ?? "Failed to assign client to zone"
                )
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error assigning client {ClientId} to zone {ZoneId}", clientId, request.ZoneId);
            return this.StatusCode(
                500,
                ApiResponse.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }
}

// Client-specific Request DTOs
public record ClientVolumeRequest
{
    [Range(0, 100)]
    public required int Volume { get; init; }
}

public record ClientMuteRequest
{
    public required bool Enabled { get; init; }
}

public record ClientLatencyRequest
{
    [Range(0, 10000)]
    public required int LatencyMs { get; init; }
}

public record ZoneAssignmentRequest
{
    [Range(1, int.MaxValue)]
    public required int ZoneId { get; init; }
}
