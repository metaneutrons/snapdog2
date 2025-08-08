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
using SnapDog2.Server.Features.Clients.Commands.Config;
using SnapDog2.Server.Features.Clients.Commands.Volume;
using SnapDog2.Server.Features.Clients.Queries;

/// <summary>
/// Controller for client management operations.
/// Follows CQRS pattern using Cortex.Mediator for enterprise-grade architecture compliance.
/// </summary>
[ApiController]
[Route("api/clients")]
[Produces("application/json")]
public partial class ClientController : ControllerBase
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
            this.LogGettingClientState(clientId);

            var query = new GetClientQuery { ClientId = clientId };
            var result = await this._mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(
                query,
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<ClientState>.CreateSuccess(result.Value));
            }

            this.LogFailedToGetClientState(clientId, result.ErrorMessage);
            return this.NotFound(
                ApiResponse<ClientState>.CreateError("CLIENT_NOT_FOUND", result.ErrorMessage ?? "Client not found")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingClientState(ex, clientId);
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
            this.LogGettingAllClientStates();

            var query = new GetAllClientsQuery();
            var result = await this._mediator.SendQueryAsync<GetAllClientsQuery, Result<List<ClientState>>>(
                query,
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<IEnumerable<ClientState>>.CreateSuccess(result.Value));
            }

            this.LogFailedToGetAllClientStates(result.ErrorMessage);
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
            this.LogErrorGettingAllClientStates(ex);
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
            this.LogGettingClientsByZone(zoneId);

            var query = new GetClientsByZoneQuery { ZoneId = zoneId };
            var result = await this._mediator.SendQueryAsync<GetClientsByZoneQuery, Result<List<ClientState>>>(
                query,
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<IEnumerable<ClientState>>.CreateSuccess(result.Value));
            }

            this.LogFailedToGetClientsByZone(zoneId, result.ErrorMessage);
            return this.NotFound(
                ApiResponse<IEnumerable<ClientState>>.CreateError(
                    "ZONE_NOT_FOUND",
                    result.ErrorMessage ?? "Zone not found or no clients assigned"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingClientsByZone(ex, zoneId);
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
            this.LogSettingClientVolume(clientId, request.Volume);

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

            this.LogFailedToSetClientVolume(clientId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("VOLUME_SET_ERROR", result.ErrorMessage ?? "Failed to set volume")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorSettingClientVolume(ex, clientId);
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
            this.LogSettingClientMute(clientId, request.Enabled);

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

            this.LogFailedToSetClientMute(clientId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("MUTE_SET_ERROR", result.ErrorMessage ?? "Failed to set mute state")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorSettingClientMute(ex, clientId);
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
            this.LogTogglingClientMute(clientId);

            var command = new ToggleClientMuteCommand { ClientId = clientId, Source = CommandSource.Api };

            var result = await this._mediator.SendCommandAsync<ToggleClientMuteCommand, Result>(
                command,
                cancellationToken
            );

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this.LogFailedToToggleClientMute(clientId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("MUTE_TOGGLE_ERROR", result.ErrorMessage ?? "Failed to toggle mute state")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorTogglingClientMute(ex, clientId);
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
            this.LogSettingClientLatency(clientId, request.LatencyMs);

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

            this.LogFailedToSetClientLatency(clientId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("LATENCY_SET_ERROR", result.ErrorMessage ?? "Failed to set latency")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorSettingClientLatency(ex, clientId);
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
            this.LogAssigningClientToZone(clientId, request.ZoneId);

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

            this.LogFailedToAssignClientToZone(clientId, request.ZoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError(
                    "ZONE_ASSIGNMENT_ERROR",
                    result.ErrorMessage ?? "Failed to assign client to zone"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorAssigningClientToZone(ex, clientId, request.ZoneId);
            return this.StatusCode(
                500,
                ApiResponse.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    // LoggerMessage definitions for high-performance logging (ID range: 2760-2783)

    // Client state operations
    [LoggerMessage(
        EventId = 2760,
        Level = LogLevel.Debug,
        Message = "Getting client state for client {clientId} via CQRS mediator"
    )]
    private partial void LogGettingClientState(int clientId);

    [LoggerMessage(
        EventId = 2761,
        Level = LogLevel.Warning,
        Message = "Failed to get client state for client {clientId}: {error}"
    )]
    private partial void LogFailedToGetClientState(int clientId, string? error);

    [LoggerMessage(
        EventId = 2762,
        Level = LogLevel.Error,
        Message = "Error getting client state for client {clientId}"
    )]
    private partial void LogErrorGettingClientState(Exception ex, int clientId);

    [LoggerMessage(EventId = 2763, Level = LogLevel.Debug, Message = "Getting all client states via CQRS mediator")]
    private partial void LogGettingAllClientStates();

    [LoggerMessage(EventId = 2764, Level = LogLevel.Warning, Message = "Failed to get all client states: {error}")]
    private partial void LogFailedToGetAllClientStates(string? error);

    [LoggerMessage(EventId = 2765, Level = LogLevel.Error, Message = "Error getting all client states")]
    private partial void LogErrorGettingAllClientStates(Exception ex);

    [LoggerMessage(
        EventId = 2766,
        Level = LogLevel.Debug,
        Message = "Getting clients for zone {zoneId} via CQRS mediator"
    )]
    private partial void LogGettingClientsByZone(int zoneId);

    [LoggerMessage(
        EventId = 2767,
        Level = LogLevel.Warning,
        Message = "Failed to get clients for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToGetClientsByZone(int zoneId, string? error);

    [LoggerMessage(EventId = 2768, Level = LogLevel.Error, Message = "Error getting clients for zone {zoneId}")]
    private partial void LogErrorGettingClientsByZone(Exception ex, int zoneId);

    // Client volume operations
    [LoggerMessage(
        EventId = 2769,
        Level = LogLevel.Debug,
        Message = "Setting volume for client {clientId} to {volume} via CQRS mediator"
    )]
    private partial void LogSettingClientVolume(int clientId, int volume);

    [LoggerMessage(
        EventId = 2770,
        Level = LogLevel.Warning,
        Message = "Failed to set volume for client {clientId}: {error}"
    )]
    private partial void LogFailedToSetClientVolume(int clientId, string? error);

    [LoggerMessage(EventId = 2771, Level = LogLevel.Error, Message = "Error setting volume for client {clientId}")]
    private partial void LogErrorSettingClientVolume(Exception ex, int clientId);

    // Client mute operations
    [LoggerMessage(
        EventId = 2772,
        Level = LogLevel.Debug,
        Message = "Setting mute for client {clientId} to {enabled} via CQRS mediator"
    )]
    private partial void LogSettingClientMute(int clientId, bool enabled);

    [LoggerMessage(
        EventId = 2773,
        Level = LogLevel.Warning,
        Message = "Failed to set mute for client {clientId}: {error}"
    )]
    private partial void LogFailedToSetClientMute(int clientId, string? error);

    [LoggerMessage(EventId = 2774, Level = LogLevel.Error, Message = "Error setting mute for client {clientId}")]
    private partial void LogErrorSettingClientMute(Exception ex, int clientId);

    [LoggerMessage(
        EventId = 2775,
        Level = LogLevel.Debug,
        Message = "Toggling mute for client {clientId} via CQRS mediator"
    )]
    private partial void LogTogglingClientMute(int clientId);

    [LoggerMessage(
        EventId = 2776,
        Level = LogLevel.Warning,
        Message = "Failed to toggle mute for client {clientId}: {error}"
    )]
    private partial void LogFailedToToggleClientMute(int clientId, string? error);

    [LoggerMessage(EventId = 2777, Level = LogLevel.Error, Message = "Error toggling mute for client {clientId}")]
    private partial void LogErrorTogglingClientMute(Exception ex, int clientId);

    // Client latency operations
    [LoggerMessage(
        EventId = 2778,
        Level = LogLevel.Debug,
        Message = "Setting latency for client {clientId} to {latencyMs}ms via CQRS mediator"
    )]
    private partial void LogSettingClientLatency(int clientId, int latencyMs);

    [LoggerMessage(
        EventId = 2779,
        Level = LogLevel.Warning,
        Message = "Failed to set latency for client {clientId}: {error}"
    )]
    private partial void LogFailedToSetClientLatency(int clientId, string? error);

    [LoggerMessage(EventId = 2780, Level = LogLevel.Error, Message = "Error setting latency for client {clientId}")]
    private partial void LogErrorSettingClientLatency(Exception ex, int clientId);

    // Client zone assignment operations
    [LoggerMessage(
        EventId = 2781,
        Level = LogLevel.Debug,
        Message = "Assigning client {clientId} to zone {zoneId} via CQRS mediator"
    )]
    private partial void LogAssigningClientToZone(int clientId, int zoneId);

    [LoggerMessage(
        EventId = 2782,
        Level = LogLevel.Warning,
        Message = "Failed to assign client {clientId} to zone {zoneId}: {error}"
    )]
    private partial void LogFailedToAssignClientToZone(int clientId, int zoneId, string? error);

    [LoggerMessage(
        EventId = 2783,
        Level = LogLevel.Error,
        Message = "Error assigning client {clientId} to zone {zoneId}"
    )]
    private partial void LogErrorAssigningClientToZone(Exception ex, int clientId, int zoneId);
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
