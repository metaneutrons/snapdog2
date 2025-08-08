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
using SnapDog2.Server.Features.Clients.Commands.Config;
using SnapDog2.Server.Features.Clients.Commands.Volume;
using SnapDog2.Server.Features.Clients.Queries;

/// <summary>
/// Controller for client management operations (API v1).
/// </summary>
[ApiController]
[Route("api/v1/clients")]
[Authorize]
[Produces("application/json")]
public partial class ClientsController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ClientsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientsController"/> class.
    /// </summary>
    public ClientsController(IServiceProvider serviceProvider, ILogger<ClientsController> logger)
    {
        this._serviceProvider = serviceProvider;
        this._logger = logger;
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
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            this.LogGettingClientsList(page, pageSize);

            var handler =
                this._serviceProvider.GetService<Server.Features.Clients.Handlers.GetAllClientsQueryHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound("GetAllClientsQueryHandler");
                return this.StatusCode(
                    500,
                    ApiResponse<PaginatedResponse<ClientInfo>>.CreateError(
                        "HANDLER_NOT_FOUND",
                        "Client handler not available"
                    )
                );
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
                        TotalPages = totalPages,
                    },
                };

                return this.Ok(ApiResponse<PaginatedResponse<ClientInfo>>.CreateSuccess(paginatedResponse));
            }

            this.LogFailedToGetClients(result.ErrorMessage);
            return this.StatusCode(
                500,
                ApiResponse<PaginatedResponse<ClientInfo>>.CreateError(
                    "CLIENTS_ERROR",
                    result.ErrorMessage ?? "Failed to retrieve clients"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingClients(ex);
            return this.StatusCode(
                500,
                ApiResponse<PaginatedResponse<ClientInfo>>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
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
    public async Task<ActionResult<ApiResponse<ClientState>>> GetClient(
        [Range(1, int.MaxValue)] int clientId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogGettingClient(clientId);

            var handler = this._serviceProvider.GetService<Server.Features.Clients.Handlers.GetClientQueryHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound("GetClientQueryHandler");
                return this.StatusCode(
                    500,
                    ApiResponse<ClientState>.CreateError("HANDLER_NOT_FOUND", "Client handler not available")
                );
            }

            var result = await handler.Handle(new GetClientQuery { ClientId = clientId }, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<ClientState>.CreateSuccess(result.Value));
            }

            this.LogClientNotFound(clientId);
            return this.NotFound(
                ApiResponse<ClientState>.CreateError("CLIENT_NOT_FOUND", $"Client {clientId} not found")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingClient(clientId, ex);
            return this.StatusCode(
                500,
                ApiResponse<ClientState>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
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
    public async Task<ActionResult<ApiResponse<ClientState>>> GetClientState(
        [Range(1, int.MaxValue)] int clientId,
        CancellationToken cancellationToken
    )
    {
        return await this.GetClient(clientId, cancellationToken);
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
    public async Task<ActionResult<ApiResponse<VolumeResponse>>> SetClientVolume(
        [Range(1, int.MaxValue)] int clientId,
        [FromBody] VolumeSetRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogSettingClientVolume(clientId, request.Level);

            var handler =
                this._serviceProvider.GetService<Server.Features.Clients.Handlers.SetClientVolumeCommandHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound("SetClientVolumeCommandHandler");
                return this.StatusCode(
                    500,
                    ApiResponse<VolumeResponse>.CreateError("HANDLER_NOT_FOUND", "Client volume handler not available")
                );
            }

            var command = new SetClientVolumeCommand
            {
                ClientId = clientId,
                Volume = request.Level,
                Source = Core.Enums.CommandSource.Api,
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse<VolumeResponse>.CreateSuccess(new VolumeResponse(request.Level)));
            }

            this.LogFailedToSetClientVolume(clientId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse<VolumeResponse>.CreateError(
                    "VOLUME_ERROR",
                    result.ErrorMessage ?? "Failed to set client volume"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorSettingClientVolume(clientId, ex);
            return this.StatusCode(
                500,
                ApiResponse<VolumeResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
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
    public async Task<ActionResult<ApiResponse<VolumeResponse>>> GetClientVolume(
        [Range(1, int.MaxValue)] int clientId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogGettingClientVolume(clientId);

            var handler = this._serviceProvider.GetService<Server.Features.Clients.Handlers.GetClientQueryHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound("GetClientQueryHandler");
                return this.StatusCode(
                    500,
                    ApiResponse<VolumeResponse>.CreateError("HANDLER_NOT_FOUND", "Client handler not available")
                );
            }

            var result = await handler.Handle(new GetClientQuery { ClientId = clientId }, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<VolumeResponse>.CreateSuccess(new VolumeResponse(result.Value.Volume)));
            }

            this.LogFailedToGetClientVolume(clientId, result.ErrorMessage);
            return this.NotFound(
                ApiResponse<VolumeResponse>.CreateError("CLIENT_NOT_FOUND", result.ErrorMessage ?? "Client not found")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingClientVolume(clientId, ex);
            return this.StatusCode(
                500,
                ApiResponse<VolumeResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
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
    public async Task<ActionResult<ApiResponse<MuteResponse>>> SetClientMute(
        [Range(1, int.MaxValue)] int clientId,
        [FromBody] MuteSetRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogSettingClientMute(clientId, request.Enabled);

            var handler =
                this._serviceProvider.GetService<Server.Features.Clients.Handlers.SetClientMuteCommandHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound("SetClientMuteCommandHandler");
                return this.StatusCode(
                    500,
                    ApiResponse<MuteResponse>.CreateError("HANDLER_NOT_FOUND", "Client mute handler not available")
                );
            }

            var command = new SetClientMuteCommand
            {
                ClientId = clientId,
                Enabled = request.Enabled,
                Source = Core.Enums.CommandSource.Api,
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse<MuteResponse>.CreateSuccess(new MuteResponse(request.Enabled)));
            }

            this.LogFailedToSetClientMute(clientId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse<MuteResponse>.CreateError("MUTE_ERROR", result.ErrorMessage ?? "Failed to set client mute")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorSettingClientMute(clientId, ex);
            return this.StatusCode(
                500,
                ApiResponse<MuteResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
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
    public async Task<ActionResult<ApiResponse<MuteResponse>>> GetClientMute(
        [Range(1, int.MaxValue)] int clientId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogGettingClientMute(clientId);

            var handler = this._serviceProvider.GetService<Server.Features.Clients.Handlers.GetClientQueryHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound("GetClientQueryHandler");
                return this.StatusCode(
                    500,
                    ApiResponse<MuteResponse>.CreateError("HANDLER_NOT_FOUND", "Client handler not available")
                );
            }

            var result = await handler.Handle(new GetClientQuery { ClientId = clientId }, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<MuteResponse>.CreateSuccess(new MuteResponse(result.Value.Mute)));
            }

            this.LogFailedToGetClientMute(clientId, result.ErrorMessage);
            return this.NotFound(
                ApiResponse<MuteResponse>.CreateError("CLIENT_NOT_FOUND", result.ErrorMessage ?? "Client not found")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingClientMute(clientId, ex);
            return this.StatusCode(
                500,
                ApiResponse<MuteResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
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
    public async Task<ActionResult<ApiResponse<MuteResponse>>> ToggleClientMute(
        [Range(1, int.MaxValue)] int clientId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogTogglingClientMute(clientId);

            var handler =
                this._serviceProvider.GetService<Server.Features.Clients.Handlers.ToggleClientMuteCommandHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound("ToggleClientMuteCommandHandler");
                return this.StatusCode(
                    500,
                    ApiResponse<MuteResponse>.CreateError(
                        "HANDLER_NOT_FOUND",
                        "Toggle client mute handler not available"
                    )
                );
            }

            var command = new ToggleClientMuteCommand { ClientId = clientId, Source = Core.Enums.CommandSource.Api };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                // Get the new mute state
                var stateHandler =
                    this._serviceProvider.GetService<Server.Features.Clients.Handlers.GetClientQueryHandler>();
                if (stateHandler != null)
                {
                    var stateResult = await stateHandler.Handle(
                        new GetClientQuery { ClientId = clientId },
                        cancellationToken
                    );
                    if (stateResult.IsSuccess && stateResult.Value != null)
                    {
                        return this.Ok(
                            ApiResponse<MuteResponse>.CreateSuccess(new MuteResponse(stateResult.Value.Mute))
                        );
                    }
                }

                return this.Ok(ApiResponse<MuteResponse>.CreateSuccess(new MuteResponse(true)));
            }

            this.LogFailedToToggleClientMute(clientId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse<MuteResponse>.CreateError(
                    "TOGGLE_MUTE_ERROR",
                    result.ErrorMessage ?? "Failed to toggle client mute"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorTogglingClientMute(clientId, ex);
            return this.StatusCode(
                500,
                ApiResponse<MuteResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
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
    public async Task<ActionResult<ApiResponse<LatencyResponse>>> SetClientLatency(
        [Range(1, int.MaxValue)] int clientId,
        [FromBody] LatencySetRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogSettingClientLatency(clientId, request.Milliseconds);

            var handler =
                this._serviceProvider.GetService<Server.Features.Clients.Handlers.SetClientLatencyCommandHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound("SetClientLatencyCommandHandler");
                return this.StatusCode(
                    500,
                    ApiResponse<LatencyResponse>.CreateError(
                        "HANDLER_NOT_FOUND",
                        "Client latency handler not available"
                    )
                );
            }

            var command = new SetClientLatencyCommand
            {
                ClientId = clientId,
                LatencyMs = request.Milliseconds,
                Source = Core.Enums.CommandSource.Api,
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse<LatencyResponse>.CreateSuccess(new LatencyResponse(request.Milliseconds)));
            }

            this.LogFailedToSetClientLatency(clientId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse<LatencyResponse>.CreateError(
                    "LATENCY_ERROR",
                    result.ErrorMessage ?? "Failed to set client latency"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorSettingClientLatency(clientId, ex);
            return this.StatusCode(
                500,
                ApiResponse<LatencyResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
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
    public async Task<ActionResult<ApiResponse<LatencyResponse>>> GetClientLatency(
        [Range(1, int.MaxValue)] int clientId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogGettingClientLatency(clientId);

            var handler = this._serviceProvider.GetService<Server.Features.Clients.Handlers.GetClientQueryHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound("GetClientQueryHandler");
                return this.StatusCode(
                    500,
                    ApiResponse<LatencyResponse>.CreateError("HANDLER_NOT_FOUND", "Client handler not available")
                );
            }

            var result = await handler.Handle(new GetClientQuery { ClientId = clientId }, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<LatencyResponse>.CreateSuccess(new LatencyResponse(result.Value.LatencyMs)));
            }

            this.LogFailedToGetClientLatency(clientId, result.ErrorMessage);
            return this.NotFound(
                ApiResponse<LatencyResponse>.CreateError("CLIENT_NOT_FOUND", result.ErrorMessage ?? "Client not found")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingClientLatency(clientId, ex);
            return this.StatusCode(
                500,
                ApiResponse<LatencyResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
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
    public async Task<ActionResult<ApiResponse>> AssignClientToZone(
        [Range(1, int.MaxValue)] int clientId,
        [FromBody] AssignZoneRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogSettingClientZone(clientId, request.ZoneId);

            var handler =
                this._serviceProvider.GetService<Server.Features.Clients.Handlers.AssignClientToZoneCommandHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound("AssignClientToZoneCommandHandler");
                return this.StatusCode(
                    500,
                    ApiResponse.CreateError("HANDLER_NOT_FOUND", "Client zone assignment handler not available")
                );
            }

            var command = new AssignClientToZoneCommand
            {
                ClientId = clientId,
                ZoneId = request.ZoneId,
                Source = Core.Enums.CommandSource.Api,
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Accepted(ApiResponse.CreateSuccess());
            }

            this.LogFailedToSetClientZone(clientId, request.ZoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError(
                    "ZONE_ASSIGNMENT_ERROR",
                    result.ErrorMessage ?? "Failed to assign client to zone"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorSettingClientZone(clientId, request.ZoneId, ex);
            return this.StatusCode(500, ApiResponse.CreateError("INTERNAL_ERROR", "Internal server error"));
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
    public async Task<ActionResult<ApiResponse<ZoneAssignmentResponse>>> GetClientZoneAssignment(
        [Range(1, int.MaxValue)] int clientId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogGettingClientZone(clientId);

            var handler = this._serviceProvider.GetService<Server.Features.Clients.Handlers.GetClientQueryHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound("GetClientQueryHandler");
                return this.StatusCode(
                    500,
                    ApiResponse<ZoneAssignmentResponse>.CreateError("HANDLER_NOT_FOUND", "Client handler not available")
                );
            }

            var result = await handler.Handle(new GetClientQuery { ClientId = clientId }, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(
                    ApiResponse<ZoneAssignmentResponse>.CreateSuccess(new ZoneAssignmentResponse(result.Value.ZoneId))
                );
            }

            this.LogFailedToGetClientZone(clientId, result.ErrorMessage);
            return this.NotFound(
                ApiResponse<ZoneAssignmentResponse>.CreateError(
                    "CLIENT_NOT_FOUND",
                    result.ErrorMessage ?? "Client not found"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingClientZone(clientId, ex);
            return this.StatusCode(
                500,
                ApiResponse<ZoneAssignmentResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    #region Logging

    // ARCHITECTURAL PROBLEM - This should never happen in production
    [LoggerMessage(
        2501,
        LogLevel.Critical,
        "🚨 CRITICAL: Handler {HandlerType} not found in DI container - This is a configuration BUG!"
    )]
    private partial void LogCriticalHandlerNotFound(string handlerType);

    // Get All Clients (2510-2519)
    [LoggerMessage(2510, LogLevel.Debug, "Getting clients list - Page: {Page}, PageSize: {PageSize}")]
    private partial void LogGettingClientsList(int page, int pageSize);

    [LoggerMessage(2511, LogLevel.Warning, "Failed to get clients: {Error}")]
    private partial void LogFailedToGetClients(string? error);

    [LoggerMessage(2512, LogLevel.Error, "Error getting clients")]
    private partial void LogErrorGettingClients(Exception exception);

    // Get Specific Client (2520-2529)
    [LoggerMessage(2520, LogLevel.Debug, "Getting client {ClientId}")]
    private partial void LogGettingClient(int clientId);

    [LoggerMessage(2521, LogLevel.Warning, "Failed to get client {ClientId}: {Error}")]
    private partial void LogFailedToGetClient(int clientId, string error);

    [LoggerMessage(2523, LogLevel.Warning, "Client {ClientId} not found")]
    private partial void LogClientNotFound(int clientId);

    [LoggerMessage(2522, LogLevel.Error, "Error getting client {ClientId}")]
    private partial void LogErrorGettingClient(int clientId, Exception exception);

    // Set Client Volume (2540-2549)
    [LoggerMessage(2540, LogLevel.Debug, "Setting volume for client {ClientId} to {Volume}")]
    private partial void LogSettingClientVolume(int clientId, int volume);

    [LoggerMessage(2541, LogLevel.Warning, "Failed to set volume for client {ClientId}: {Error}")]
    private partial void LogFailedToSetClientVolume(int clientId, string? error);

    [LoggerMessage(2542, LogLevel.Error, "Error setting volume for client {ClientId}")]
    private partial void LogErrorSettingClientVolume(int clientId, Exception exception);

    // Get Client Volume (2550-2559)
    [LoggerMessage(2550, LogLevel.Debug, "Getting volume for client {ClientId}")]
    private partial void LogGettingClientVolume(int clientId);

    [LoggerMessage(2551, LogLevel.Warning, "Failed to get volume for client {ClientId}: {Error}")]
    private partial void LogFailedToGetClientVolume(int clientId, string? error);

    [LoggerMessage(2552, LogLevel.Error, "Error getting volume for client {ClientId}")]
    private partial void LogErrorGettingClientVolume(int clientId, Exception exception);

    // Set Client Mute (2560-2569)
    [LoggerMessage(2560, LogLevel.Debug, "Setting mute for client {ClientId} to {Muted}")]
    private partial void LogSettingClientMute(int clientId, bool muted);

    [LoggerMessage(2561, LogLevel.Warning, "Failed to set mute for client {ClientId}: {Error}")]
    private partial void LogFailedToSetClientMute(int clientId, string? error);

    [LoggerMessage(2562, LogLevel.Error, "Error setting mute for client {ClientId}")]
    private partial void LogErrorSettingClientMute(int clientId, Exception exception);

    // Get Client Mute (2570-2579)
    [LoggerMessage(2570, LogLevel.Debug, "Getting mute status for client {ClientId}")]
    private partial void LogGettingClientMute(int clientId);

    [LoggerMessage(2571, LogLevel.Warning, "Failed to get mute status for client {ClientId}: {Error}")]
    private partial void LogFailedToGetClientMute(int clientId, string? error);

    [LoggerMessage(2572, LogLevel.Error, "Error getting mute status for client {ClientId}")]
    private partial void LogErrorGettingClientMute(int clientId, Exception exception);

    // Toggle Client Mute (2580-2589)
    [LoggerMessage(2580, LogLevel.Debug, "Toggling mute for client {ClientId}")]
    private partial void LogTogglingClientMute(int clientId);

    [LoggerMessage(2581, LogLevel.Warning, "Failed to toggle mute for client {ClientId}: {Error}")]
    private partial void LogFailedToToggleClientMute(int clientId, string? error);

    [LoggerMessage(2582, LogLevel.Error, "Error toggling mute for client {ClientId}")]
    private partial void LogErrorTogglingClientMute(int clientId, Exception exception);

    // Set Client Latency (2590-2599)
    [LoggerMessage(2590, LogLevel.Debug, "Setting latency for client {ClientId} to {Latency}ms")]
    private partial void LogSettingClientLatency(int clientId, int latency);

    [LoggerMessage(2591, LogLevel.Warning, "Failed to set latency for client {ClientId}: {Error}")]
    private partial void LogFailedToSetClientLatency(int clientId, string? error);

    [LoggerMessage(2592, LogLevel.Error, "Error setting latency for client {ClientId}")]
    private partial void LogErrorSettingClientLatency(int clientId, Exception exception);

    // Get Client Latency (2600-2609)
    [LoggerMessage(2600, LogLevel.Debug, "Getting latency for client {ClientId}")]
    private partial void LogGettingClientLatency(int clientId);

    [LoggerMessage(2601, LogLevel.Warning, "Failed to get latency for client {ClientId}: {Error}")]
    private partial void LogFailedToGetClientLatency(int clientId, string? error);

    [LoggerMessage(2602, LogLevel.Error, "Error getting latency for client {ClientId}")]
    private partial void LogErrorGettingClientLatency(int clientId, Exception exception);

    // Set Client Zone (2610-2619)
    [LoggerMessage(2610, LogLevel.Debug, "Setting zone for client {ClientId} to {ZoneId}")]
    private partial void LogSettingClientZone(int clientId, int zoneId);

    [LoggerMessage(2611, LogLevel.Warning, "Failed to assign client {ClientId} to zone {ZoneId}: {Error}")]
    private partial void LogFailedToSetClientZone(int clientId, int zoneId, string? error);

    [LoggerMessage(2612, LogLevel.Error, "Error assigning client {ClientId} to zone {ZoneId}")]
    private partial void LogErrorSettingClientZone(int clientId, int zoneId, Exception exception);

    // Get Client Zone (2620-2629)
    [LoggerMessage(2620, LogLevel.Debug, "Getting zone for client {ClientId}")]
    private partial void LogGettingClientZone(int clientId);

    [LoggerMessage(2621, LogLevel.Warning, "Failed to get zone for client {ClientId}: {Error}")]
    private partial void LogFailedToGetClientZone(int clientId, string? error);

    [LoggerMessage(2622, LogLevel.Error, "Error getting zone for client {ClientId}")]
    private partial void LogErrorGettingClientZone(int clientId, Exception exception);

    #endregion
}
