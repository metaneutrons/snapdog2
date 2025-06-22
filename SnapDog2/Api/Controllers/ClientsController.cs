using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;

namespace SnapDog2.Api.Controllers;

/// <summary>
/// Clients management endpoints for configuring and controlling Snapcast clients.
/// Provides CRUD operations and client control functionality for the SnapDog2 multi-room audio system.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class ClientsController : ControllerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClientsController"/> class.
    /// </summary>
    /// <param name="mediator">The MediatR instance for handling commands and queries.</param>
    /// <param name="logger">The logger instance for this controller.</param>
    private readonly IMediator _mediator;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(IMediator mediator, ILogger<ClientsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all clients in the system.
    /// </summary>
    /// <param name="includeDisconnected">Whether to include disconnected clients in the results.</param>
    /// <param name="includeDetails">Whether to include detailed information in the response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all clients.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ClientResponse>), 200)]
    [ProducesResponseType(typeof(void), 400)]
    public ActionResult<IEnumerable<ClientResponse>> GetAllClients(
        [FromQuery] bool includeDisconnected = true,
        [FromQuery] bool includeDetails = true,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Getting all clients with filters: includeDisconnected={IncludeDisconnected}, includeDetails={IncludeDetails}",
            includeDisconnected,
            includeDetails
        );

        // TODO: Implement GetAllClientsQuery when Server layer features are created
        // var query = new GetAllClientsQuery { IncludeDisconnected = includeDisconnected, IncludeDetails = includeDetails };
        // return await HandleRequestAsync(query, cancellationToken);

        // Temporary implementation - return empty list
        var emptyClients = new List<ClientResponse>();
        return Ok(emptyClients.AsEnumerable());
    }

    /// <summary>
    /// Gets a specific client by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The client with the specified ID.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ClientResponse), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(void), 400)]
    public ActionResult<ClientResponse> GetClientById(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Client ID cannot be empty.");
        }

        _logger.LogInformation("Getting client by ID: {ClientId}", id);

        // TODO: Implement GetClientByIdQuery when Server layer features are created
        // var query = new GetClientByIdQuery(id);
        // return await HandleRequestAsync(query, cancellationToken);

        // Temporary implementation - return not found
        return NotFound();
    }

    /// <summary>
    /// Gets clients by their connection status.
    /// </summary>
    /// <param name="status">The client status to filter by (Connected, Disconnected, Error).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of clients with the specified status.</returns>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<ClientResponse>), 200)]
    [ProducesResponseType(typeof(void), 400)]
    public ActionResult<IEnumerable<ClientResponse>> GetClientsByStatus(
        string status,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return BadRequest("Status cannot be empty.");
        }

        if (!Enum.TryParse<ClientStatus>(status, true, out var clientStatus))
        {
            return BadRequest($"Invalid status: {status}. Valid values are: Connected, Disconnected, Error.");
        }

        _logger.LogInformation("Getting clients by status: {Status}", clientStatus);

        // TODO: Implement GetClientsByStatusQuery when Server layer features are created
        // var query = new GetClientsByStatusQuery(clientStatus);
        // return await HandleRequestAsync(query, cancellationToken);

        // Temporary implementation - return empty list
        var emptyClients = new List<ClientResponse>();
        return Ok(emptyClients.AsEnumerable());
    }

    /// <summary>
    /// Sets the volume level for a specific client.
    /// </summary>
    /// <param name="id">The unique identifier of the client.</param>
    /// <param name="request">The volume update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response if the volume was updated.</returns>
    [HttpPut("{id}/volume")]
    [ProducesResponseType(typeof(ClientResponse), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(void), 400)]
    public ActionResult<ClientResponse> SetClientVolume(
        string id,
        [FromBody] SetVolumeRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Client ID cannot be empty.");
        }

        if (request == null)
        {
            return BadRequest("Request body cannot be null.");
        }

        if (request.Volume < 0 || request.Volume > 100)
        {
            return BadRequest("Volume must be between 0 and 100.");
        }

        _logger.LogInformation("Setting volume for client {ClientId} to {Volume}", id, request.Volume);

        // TODO: Implement SetClientVolumeCommand when Server layer features are created
        // var command = new SetClientVolumeCommand(id, request.Volume)
        // {
        //     Muted = request.Muted,
        //     RequestedBy = HttpContext.User?.Identity?.Name ?? "API"
        // };
        // return await HandleRequestAsync(command, cancellationToken);

        // Temporary implementation - return not found
        return NotFound();
    }

    /// <summary>
    /// Assigns a client to a specific zone.
    /// </summary>
    /// <param name="id">The unique identifier of the client.</param>
    /// <param name="request">The zone assignment request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response if the client was assigned to the zone.</returns>
    [HttpPut("{id}/zone")]
    [ProducesResponseType(typeof(ClientResponse), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(void), 400)]
    public ActionResult<ClientResponse> AssignClientToZone(
        string id,
        [FromBody] AssignZoneRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Client ID cannot be empty.");
        }

        if (request == null)
        {
            return BadRequest("Request body cannot be null.");
        }

        _logger.LogInformation("Assigning client {ClientId} to zone {ZoneId}", id, request.ZoneId ?? "none");

        // TODO: Implement AssignClientToZoneCommand when Server layer features are created
        // var command = new AssignClientToZoneCommand(id, request.ZoneId)
        // {
        //     RequestedBy = HttpContext.User?.Identity?.Name ?? "API"
        // };
        // return await HandleRequestAsync(command, cancellationToken);

        // Temporary implementation - return not found
        return NotFound();
    }
}

/// <summary>
/// Request model for setting client volume.
/// </summary>
public class SetVolumeRequest
{
    /// <summary>
    /// Gets or sets the volume level (0-100).
    /// </summary>
    public required int Volume { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the client should be muted.
    /// </summary>
    public bool? Muted { get; set; }
}

/// <summary>
/// Request model for assigning a client to a zone.
/// </summary>
public class AssignZoneRequest
{
    /// <summary>
    /// Gets or sets the zone ID to assign the client to. Set to null to unassign from any zone.
    /// </summary>
    public string? ZoneId { get; set; }
}
