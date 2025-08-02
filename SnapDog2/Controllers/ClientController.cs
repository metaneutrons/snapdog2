namespace SnapDog2.Controllers;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Commands;
using SnapDog2.Server.Features.Clients.Queries;

/// <summary>
/// Controller for client management operations.
/// </summary>
[ApiController]
[Route("api/clients")]
[Produces("application/json")]
public class ClientController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ClientController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientController"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger instance.</param>
    public ClientController(IServiceProvider serviceProvider, ILogger<ClientController> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current state of a specific client.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The client state.</returns>
    [HttpGet("{clientId:int}/state")]
    [ProducesResponseType(typeof(ClientState), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ClientState>> GetClientState([Range(1, int.MaxValue)] int clientId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting client state for client {ClientId}", clientId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.GetClientQueryHandler>();
            if (handler == null)
            {
                _logger.LogError("GetClientQueryHandler not found in DI container");
                return StatusCode(500, new { error = "Handler not available" });
            }

            var result = await handler.Handle(new GetClientQuery { ClientId = clientId }, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Ok(result.Value);
            }

            _logger.LogWarning("Failed to get client state for client {ClientId}: {Error}", clientId, result.ErrorMessage);
            return NotFound(new { error = result.ErrorMessage ?? "Client not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting client state for client {ClientId}", clientId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets the states of all clients.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Collection of client states.</returns>
    [HttpGet("states")]
    [ProducesResponseType(typeof(IEnumerable<ClientState>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IEnumerable<ClientState>>> GetAllClientStates(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting all client states");

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.GetAllClientsQueryHandler>();
            if (handler == null)
            {
                _logger.LogError("GetAllClientsQueryHandler not found in DI container");
                return StatusCode(500, new { error = "Handler not available" });
            }

            var result = await handler.Handle(new GetAllClientsQuery(), cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Ok(result.Value);
            }

            _logger.LogWarning("Failed to get all client states: {Error}", result.ErrorMessage);
            return StatusCode(500, new { error = result.ErrorMessage ?? "Failed to retrieve client states" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all client states");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets clients assigned to a specific zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Collection of client states for the zone.</returns>
    [HttpGet("by-zone/{zoneId:int}")]
    [ProducesResponseType(typeof(IEnumerable<ClientState>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IEnumerable<ClientState>>> GetClientsByZone([Range(1, int.MaxValue)] int zoneId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting clients for zone {ZoneId}", zoneId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.GetClientsByZoneQueryHandler>();
            if (handler == null)
            {
                _logger.LogError("GetClientsByZoneQueryHandler not found in DI container");
                return StatusCode(500, new { error = "Handler not available" });
            }

            var result = await handler.Handle(new GetClientsByZoneQuery { ZoneId = zoneId }, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Ok(result.Value);
            }

            _logger.LogWarning("Failed to get clients for zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return NotFound(new { error = result.ErrorMessage ?? "Zone not found or no clients assigned" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting clients for zone {ZoneId}", zoneId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Sets the volume for a specific client.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="request">The volume request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success or error response.</returns>
    [HttpPost("{clientId:int}/volume")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> SetClientVolume([Range(1, int.MaxValue)] int clientId, [FromBody] ClientVolumeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Setting volume for client {ClientId} to {Volume}", clientId, request.Volume);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.SetClientVolumeCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("SetClientVolumeCommandHandler not found in DI container");
                return StatusCode(500, new { error = "Handler not available" });
            }

            var command = new SetClientVolumeCommand
            {
                ClientId = clientId,
                Volume = request.Volume,
                Source = CommandSource.Api
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(new { message = "Volume set successfully" });
            }

            _logger.LogWarning("Failed to set volume for client {ClientId}: {Error}", clientId, result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage ?? "Failed to set volume" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting volume for client {ClientId}", clientId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Sets the mute state for a specific client.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="request">The mute request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success or error response.</returns>
    [HttpPost("{clientId:int}/mute")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> SetClientMute([Range(1, int.MaxValue)] int clientId, [FromBody] ClientMuteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Setting mute for client {ClientId} to {Enabled}", clientId, request.Enabled);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.SetClientMuteCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("SetClientMuteCommandHandler not found in DI container");
                return StatusCode(500, new { error = "Handler not available" });
            }

            var command = new SetClientMuteCommand
            {
                ClientId = clientId,
                Enabled = request.Enabled,
                Source = CommandSource.Api
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(new { message = "Mute state set successfully" });
            }

            _logger.LogWarning("Failed to set mute for client {ClientId}: {Error}", clientId, result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage ?? "Failed to set mute state" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting mute for client {ClientId}", clientId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Toggles the mute state for a specific client.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success or error response.</returns>
    [HttpPost("{clientId:int}/toggle-mute")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> ToggleClientMute([Range(1, int.MaxValue)] int clientId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Toggling mute for client {ClientId}", clientId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.ToggleClientMuteCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("ToggleClientMuteCommandHandler not found in DI container");
                return StatusCode(500, new { error = "Handler not available" });
            }

            var command = new ToggleClientMuteCommand
            {
                ClientId = clientId,
                Source = CommandSource.Api
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(new { message = "Mute state toggled successfully" });
            }

            _logger.LogWarning("Failed to toggle mute for client {ClientId}: {Error}", clientId, result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage ?? "Failed to toggle mute state" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling mute for client {ClientId}", clientId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Sets the latency for a specific client.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="request">The latency request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success or error response.</returns>
    [HttpPost("{clientId:int}/latency")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> SetClientLatency([Range(1, int.MaxValue)] int clientId, [FromBody] ClientLatencyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Setting latency for client {ClientId} to {LatencyMs}ms", clientId, request.LatencyMs);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.SetClientLatencyCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("SetClientLatencyCommandHandler not found in DI container");
                return StatusCode(500, new { error = "Handler not available" });
            }

            var command = new SetClientLatencyCommand
            {
                ClientId = clientId,
                LatencyMs = request.LatencyMs,
                Source = CommandSource.Api
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(new { message = "Latency set successfully" });
            }

            _logger.LogWarning("Failed to set latency for client {ClientId}: {Error}", clientId, result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage ?? "Failed to set latency" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting latency for client {ClientId}", clientId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Assigns a client to a zone.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="request">The zone assignment request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success or error response.</returns>
    [HttpPost("{clientId:int}/assign-zone")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> AssignClientToZone([Range(1, int.MaxValue)] int clientId, [FromBody] ZoneAssignmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Assigning client {ClientId} to zone {ZoneId}", clientId, request.ZoneId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.AssignClientToZoneCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("AssignClientToZoneCommandHandler not found in DI container");
                return StatusCode(500, new { error = "Handler not available" });
            }

            var command = new AssignClientToZoneCommand
            {
                ClientId = clientId,
                ZoneId = request.ZoneId,
                Source = CommandSource.Api
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(new { message = "Client assigned to zone successfully" });
            }

            _logger.LogWarning("Failed to assign client {ClientId} to zone {ZoneId}: {Error}", clientId, request.ZoneId, result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage ?? "Failed to assign client to zone" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning client {ClientId} to zone {ZoneId}", clientId, request.ZoneId);
            return StatusCode(500, new { error = "Internal server error" });
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
