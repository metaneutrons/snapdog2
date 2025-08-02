namespace SnapDog2.Api.Controllers.V1;

using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Api.Models;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Commands;
using SnapDog2.Server.Features.Clients.Queries;

/// <summary>
/// Controller for client management operations (API v1).
/// </summary>
[ApiController]
[Route("api/v1/clients")]
[Authorize]
[Produces("application/json")]
public class ClientsController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ClientsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientsController"/> class.
    /// </summary>
    public ClientsController(IServiceProvider serviceProvider, ILogger<ClientsController> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Lists discovered clients.
    /// </summary>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Paginated list of clients.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<ClientInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ClientInfo>>>> GetClients(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting clients list - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.GetAllClientsQueryHandler>();
            if (handler == null)
            {
                _logger.LogError("GetAllClientsQueryHandler not found in DI container");
                return StatusCode(500, ApiResponse<PaginatedResponse<ClientInfo>>.CreateError("HANDLER_NOT_FOUND", "Client handler not available"));
            }

            var result = await handler.Handle(new GetAllClientsQuery(), cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                var clients = result.Value.Select(c => new ClientInfo(c.Id, c.Name, c.Connected, c.ZoneId)).ToList();
                
                // Apply pagination
                var totalItems = clients.Count;
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                var skip = (page - 1) * pageSize;
                var pagedClients = clients.Skip(skip).Take(pageSize).ToList();

                var paginatedResponse = new PaginatedResponse<ClientInfo>
                {
                    Items = pagedClients,
                    Pagination = new PaginationMetadata
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalItems = totalItems,
                        TotalPages = totalPages
                    }
                };

                return Ok(ApiResponse<PaginatedResponse<ClientInfo>>.CreateSuccess(paginatedResponse));
            }

            _logger.LogWarning("Failed to get clients: {Error}", result.ErrorMessage);
            return StatusCode(500, ApiResponse<PaginatedResponse<ClientInfo>>.CreateError("CLIENTS_ERROR", result.ErrorMessage ?? "Failed to retrieve clients"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting clients");
            return StatusCode(500, ApiResponse<PaginatedResponse<ClientInfo>>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Gets details for a client.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Client state information.</returns>
    [HttpGet("{clientId:int}")]
    [ProducesResponseType(typeof(ApiResponse<ClientState>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<ClientState>>> GetClient([Range(1, int.MaxValue)] int clientId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting client {ClientId}", clientId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.GetClientQueryHandler>();
            if (handler == null)
            {
                _logger.LogError("GetClientQueryHandler not found in DI container");
                return StatusCode(500, ApiResponse<ClientState>.CreateError("HANDLER_NOT_FOUND", "Client handler not available"));
            }

            var result = await handler.Handle(new GetClientQuery { ClientId = clientId }, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Ok(ApiResponse<ClientState>.CreateSuccess(result.Value));
            }

            _logger.LogWarning("Client {ClientId} not found", clientId);
            return NotFound(ApiResponse<ClientState>.CreateError("CLIENT_NOT_FOUND", $"Client {clientId} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting client {ClientId}", clientId);
            return StatusCode(500, ApiResponse<ClientState>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Gets full state JSON for a client (alias for GET /{clientId}).
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Client state information.</returns>
    [HttpGet("{clientId:int}/state")]
    [ProducesResponseType(typeof(ApiResponse<ClientState>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<ClientState>>> GetClientState([Range(1, int.MaxValue)] int clientId, CancellationToken cancellationToken)
    {
        return await GetClient(clientId, cancellationToken);
    }

    // CLIENT VOLUME SETTINGS

    /// <summary>
    /// Sets client volume.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="request">The volume request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Volume response.</returns>
    [HttpPut("{clientId:int}/settings/volume")]
    [ProducesResponseType(typeof(ApiResponse<VolumeResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<VolumeResponse>>> SetClientVolume([Range(1, int.MaxValue)] int clientId, [FromBody] VolumeSetRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Setting volume {Volume} for client {ClientId}", request.Level, clientId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.SetClientVolumeCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("SetClientVolumeCommandHandler not found in DI container");
                return StatusCode(500, ApiResponse<VolumeResponse>.CreateError("HANDLER_NOT_FOUND", "Client volume handler not available"));
            }

            var command = new SetClientVolumeCommand
            {
                ClientId = clientId,
                Volume = request.Level,
                Source = Core.Enums.CommandSource.Api
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(ApiResponse<VolumeResponse>.CreateSuccess(new VolumeResponse(request.Level)));
            }

            _logger.LogWarning("Failed to set volume for client {ClientId}: {Error}", clientId, result.ErrorMessage);
            return BadRequest(ApiResponse<VolumeResponse>.CreateError("VOLUME_ERROR", result.ErrorMessage ?? "Failed to set client volume"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting volume for client {ClientId}", clientId);
            return StatusCode(500, ApiResponse<VolumeResponse>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Gets client volume.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Volume response.</returns>
    [HttpGet("{clientId:int}/settings/volume")]
    [ProducesResponseType(typeof(ApiResponse<VolumeResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<VolumeResponse>>> GetClientVolume([Range(1, int.MaxValue)] int clientId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting volume for client {ClientId}", clientId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.GetClientQueryHandler>();
            if (handler == null)
            {
                _logger.LogError("GetClientQueryHandler not found in DI container");
                return StatusCode(500, ApiResponse<VolumeResponse>.CreateError("HANDLER_NOT_FOUND", "Client handler not available"));
            }

            var result = await handler.Handle(new GetClientQuery { ClientId = clientId }, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Ok(ApiResponse<VolumeResponse>.CreateSuccess(new VolumeResponse(result.Value.Volume)));
            }

            _logger.LogWarning("Failed to get volume for client {ClientId}: {Error}", clientId, result.ErrorMessage);
            return NotFound(ApiResponse<VolumeResponse>.CreateError("CLIENT_NOT_FOUND", result.ErrorMessage ?? "Client not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting volume for client {ClientId}", clientId);
            return StatusCode(500, ApiResponse<VolumeResponse>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    // CLIENT MUTE SETTINGS

    /// <summary>
    /// Sets client mute state.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="request">The mute request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Mute response.</returns>
    [HttpPut("{clientId:int}/settings/mute")]
    [ProducesResponseType(typeof(ApiResponse<MuteResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<MuteResponse>>> SetClientMute([Range(1, int.MaxValue)] int clientId, [FromBody] MuteSetRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Setting mute {Enabled} for client {ClientId}", request.Enabled, clientId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.SetClientMuteCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("SetClientMuteCommandHandler not found in DI container");
                return StatusCode(500, ApiResponse<MuteResponse>.CreateError("HANDLER_NOT_FOUND", "Client mute handler not available"));
            }

            var command = new SetClientMuteCommand
            {
                ClientId = clientId,
                Enabled = request.Enabled,
                Source = Core.Enums.CommandSource.Api
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(ApiResponse<MuteResponse>.CreateSuccess(new MuteResponse(request.Enabled)));
            }

            _logger.LogWarning("Failed to set mute for client {ClientId}: {Error}", clientId, result.ErrorMessage);
            return BadRequest(ApiResponse<MuteResponse>.CreateError("MUTE_ERROR", result.ErrorMessage ?? "Failed to set client mute"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting mute for client {ClientId}", clientId);
            return StatusCode(500, ApiResponse<MuteResponse>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Gets client mute state.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Mute response.</returns>
    [HttpGet("{clientId:int}/settings/mute")]
    [ProducesResponseType(typeof(ApiResponse<MuteResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<MuteResponse>>> GetClientMute([Range(1, int.MaxValue)] int clientId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting mute state for client {ClientId}", clientId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.GetClientQueryHandler>();
            if (handler == null)
            {
                _logger.LogError("GetClientQueryHandler not found in DI container");
                return StatusCode(500, ApiResponse<MuteResponse>.CreateError("HANDLER_NOT_FOUND", "Client handler not available"));
            }

            var result = await handler.Handle(new GetClientQuery { ClientId = clientId }, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Ok(ApiResponse<MuteResponse>.CreateSuccess(new MuteResponse(result.Value.Mute)));
            }

            _logger.LogWarning("Failed to get mute state for client {ClientId}: {Error}", clientId, result.ErrorMessage);
            return NotFound(ApiResponse<MuteResponse>.CreateError("CLIENT_NOT_FOUND", result.ErrorMessage ?? "Client not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mute state for client {ClientId}", clientId);
            return StatusCode(500, ApiResponse<MuteResponse>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Toggles client mute state.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>New mute response.</returns>
    [HttpPost("{clientId:int}/settings/mute/toggle")]
    [ProducesResponseType(typeof(ApiResponse<MuteResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<MuteResponse>>> ToggleClientMute([Range(1, int.MaxValue)] int clientId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Toggling mute for client {ClientId}", clientId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.ToggleClientMuteCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("ToggleClientMuteCommandHandler not found in DI container");
                return StatusCode(500, ApiResponse<MuteResponse>.CreateError("HANDLER_NOT_FOUND", "Toggle client mute handler not available"));
            }

            var command = new ToggleClientMuteCommand
            {
                ClientId = clientId,
                Source = Core.Enums.CommandSource.Api
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                // Get the new mute state
                var stateHandler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.GetClientQueryHandler>();
                if (stateHandler != null)
                {
                    var stateResult = await stateHandler.Handle(new GetClientQuery { ClientId = clientId }, cancellationToken);
                    if (stateResult.IsSuccess && stateResult.Value != null)
                    {
                        return Ok(ApiResponse<MuteResponse>.CreateSuccess(new MuteResponse(stateResult.Value.Mute)));
                    }
                }

                return Ok(ApiResponse<MuteResponse>.CreateSuccess(new MuteResponse(true)));
            }

            _logger.LogWarning("Failed to toggle mute for client {ClientId}: {Error}", clientId, result.ErrorMessage);
            return BadRequest(ApiResponse<MuteResponse>.CreateError("TOGGLE_MUTE_ERROR", result.ErrorMessage ?? "Failed to toggle client mute"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling mute for client {ClientId}", clientId);
            return StatusCode(500, ApiResponse<MuteResponse>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    // CLIENT LATENCY SETTINGS

    /// <summary>
    /// Sets client latency.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="request">The latency request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Latency response.</returns>
    [HttpPut("{clientId:int}/settings/latency")]
    [ProducesResponseType(typeof(ApiResponse<LatencyResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<LatencyResponse>>> SetClientLatency([Range(1, int.MaxValue)] int clientId, [FromBody] LatencySetRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Setting latency {Latency}ms for client {ClientId}", request.Milliseconds, clientId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.SetClientLatencyCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("SetClientLatencyCommandHandler not found in DI container");
                return StatusCode(500, ApiResponse<LatencyResponse>.CreateError("HANDLER_NOT_FOUND", "Client latency handler not available"));
            }

            var command = new SetClientLatencyCommand
            {
                ClientId = clientId,
                LatencyMs = request.Milliseconds,
                Source = Core.Enums.CommandSource.Api
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(ApiResponse<LatencyResponse>.CreateSuccess(new LatencyResponse(request.Milliseconds)));
            }

            _logger.LogWarning("Failed to set latency for client {ClientId}: {Error}", clientId, result.ErrorMessage);
            return BadRequest(ApiResponse<LatencyResponse>.CreateError("LATENCY_ERROR", result.ErrorMessage ?? "Failed to set client latency"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting latency for client {ClientId}", clientId);
            return StatusCode(500, ApiResponse<LatencyResponse>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Gets client latency.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Latency response.</returns>
    [HttpGet("{clientId:int}/settings/latency")]
    [ProducesResponseType(typeof(ApiResponse<LatencyResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<LatencyResponse>>> GetClientLatency([Range(1, int.MaxValue)] int clientId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting latency for client {ClientId}", clientId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.GetClientQueryHandler>();
            if (handler == null)
            {
                _logger.LogError("GetClientQueryHandler not found in DI container");
                return StatusCode(500, ApiResponse<LatencyResponse>.CreateError("HANDLER_NOT_FOUND", "Client handler not available"));
            }

            var result = await handler.Handle(new GetClientQuery { ClientId = clientId }, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Ok(ApiResponse<LatencyResponse>.CreateSuccess(new LatencyResponse(result.Value.LatencyMs)));
            }

            _logger.LogWarning("Failed to get latency for client {ClientId}: {Error}", clientId, result.ErrorMessage);
            return NotFound(ApiResponse<LatencyResponse>.CreateError("CLIENT_NOT_FOUND", result.ErrorMessage ?? "Client not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latency for client {ClientId}", clientId);
            return StatusCode(500, ApiResponse<LatencyResponse>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    // CLIENT ZONE ASSIGNMENT

    /// <summary>
    /// Assigns client to zone.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="request">The zone assignment request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success response.</returns>
    [HttpPut("{clientId:int}/settings/zone")]
    [ProducesResponseType(typeof(ApiResponse), 202)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> AssignClientToZone([Range(1, int.MaxValue)] int clientId, [FromBody] AssignZoneRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Assigning client {ClientId} to zone {ZoneId}", clientId, request.ZoneId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.AssignClientToZoneCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("AssignClientToZoneCommandHandler not found in DI container");
                return StatusCode(500, ApiResponse.CreateError("HANDLER_NOT_FOUND", "Client zone assignment handler not available"));
            }

            var command = new AssignClientToZoneCommand
            {
                ClientId = clientId,
                ZoneId = request.ZoneId,
                Source = Core.Enums.CommandSource.Api
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return Accepted(ApiResponse.CreateSuccess());
            }

            _logger.LogWarning("Failed to assign client {ClientId} to zone {ZoneId}: {Error}", clientId, request.ZoneId, result.ErrorMessage);
            return BadRequest(ApiResponse.CreateError("ZONE_ASSIGNMENT_ERROR", result.ErrorMessage ?? "Failed to assign client to zone"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning client {ClientId} to zone {ZoneId}", clientId, request.ZoneId);
            return StatusCode(500, ApiResponse.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Gets client assigned zone.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Zone assignment response.</returns>
    [HttpGet("{clientId:int}/settings/zone")]
    [ProducesResponseType(typeof(ApiResponse<ZoneAssignmentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<ZoneAssignmentResponse>>> GetClientZoneAssignment([Range(1, int.MaxValue)] int clientId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting zone assignment for client {ClientId}", clientId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Clients.Handlers.GetClientQueryHandler>();
            if (handler == null)
            {
                _logger.LogError("GetClientQueryHandler not found in DI container");
                return StatusCode(500, ApiResponse<ZoneAssignmentResponse>.CreateError("HANDLER_NOT_FOUND", "Client handler not available"));
            }

            var result = await handler.Handle(new GetClientQuery { ClientId = clientId }, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Ok(ApiResponse<ZoneAssignmentResponse>.CreateSuccess(new ZoneAssignmentResponse(result.Value.ZoneId)));
            }

            _logger.LogWarning("Failed to get zone assignment for client {ClientId}: {Error}", clientId, result.ErrorMessage);
            return NotFound(ApiResponse<ZoneAssignmentResponse>.CreateError("CLIENT_NOT_FOUND", result.ErrorMessage ?? "Client not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting zone assignment for client {ClientId}", clientId);
            return StatusCode(500, ApiResponse<ZoneAssignmentResponse>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }
}
