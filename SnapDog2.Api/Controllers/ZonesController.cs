using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Api.Models;
using SnapDog2.Core.Models.Entities;

namespace SnapDog2.Api.Controllers;

/// <summary>
/// Zones management endpoints for creating, configuring, and managing audio zones.
/// Provides CRUD operations for zone configuration in the SnapDog2 multi-room audio system.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class ZonesController : ApiControllerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ZonesController"/> class.
    /// </summary>
    /// <param name="mediator">The MediatR instance for handling commands and queries.</param>
    /// <param name="logger">The logger instance for this controller.</param>
    public ZonesController(IMediator mediator, ILogger<ZonesController> logger)
        : base(mediator, logger) { }

    /// <summary>
    /// Gets all zones in the system.
    /// </summary>
    /// <param name="includeDisabled">Whether to include disabled zones in the results.</param>
    /// <param name="includeDetails">Whether to include detailed information in the response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all zones.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ZoneResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ZoneResponse>>>> GetAllZones(
        [FromQuery] bool includeDisabled = true,
        [FromQuery] bool includeDetails = true,
        CancellationToken cancellationToken = default
    )
    {
        Logger.LogInformation(
            "Getting all zones with filters: includeDisabled={IncludeDisabled}, includeDetails={IncludeDetails}",
            includeDisabled,
            includeDetails
        );

        // TODO: Implement GetAllZonesQuery when Server layer features are created
        // var query = new GetAllZonesQuery { IncludeDisabled = includeDisabled, IncludeDetails = includeDetails };
        // return await HandleRequestAsync(query, cancellationToken);

        // Temporary implementation - return empty list
        var emptyZones = new List<ZoneResponse>();
        return SuccessResponse(emptyZones.AsEnumerable());
    }

    /// <summary>
    /// Gets a specific zone by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the zone.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The zone with the specified ID.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ZoneResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<ZoneResponse>>> GetZoneById(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(ApiResponse<ZoneResponse>.Fail("Zone ID cannot be empty."));
        }

        Logger.LogInformation("Getting zone by ID: {ZoneId}", id);

        // TODO: Implement GetZoneByIdQuery when Server layer features are created
        // var query = new GetZoneByIdQuery(id);
        // return await HandleRequestAsync(query, cancellationToken);

        // Temporary implementation - return not found
        return NotFound(ApiResponse<ZoneResponse>.Fail("Zone not found."));
    }

    /// <summary>
    /// Gets all clients assigned to a specific zone.
    /// </summary>
    /// <param name="id">The unique identifier of the zone.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of clients in the specified zone.</returns>
    [HttpGet("{id}/clients")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ClientResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ClientResponse>>>> GetZoneClients(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(ApiResponse<IEnumerable<ClientResponse>>.Fail("Zone ID cannot be empty."));
        }

        Logger.LogInformation("Getting clients for zone: {ZoneId}", id);

        // TODO: Implement GetZoneClientsQuery when Server layer features are created
        // var query = new GetZoneClientsQuery(id);
        // return await HandleRequestAsync(query, cancellationToken);

        // Temporary implementation - return empty list
        var emptyClients = new List<ClientResponse>();
        return SuccessResponse(emptyClients.AsEnumerable());
    }

    /// <summary>
    /// Creates a new zone.
    /// </summary>
    /// <param name="request">The zone creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created zone.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ZoneResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<ZoneResponse>>> CreateZone(
        [FromBody] CreateZoneRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (request == null)
        {
            return BadRequest(ApiResponse<ZoneResponse>.Fail("Request body cannot be null."));
        }

        Logger.LogInformation("Creating new zone: {ZoneName}", request.Name);

        // TODO: Implement CreateZoneCommand when Server layer features are created
        // var command = new CreateZoneCommand(request.Name, request.Description)
        // {
        //     Location = request.Location,
        //     Color = request.Color ?? "#007bff",
        //     Icon = request.Icon ?? "speaker",
        //     DefaultVolume = request.DefaultVolume ?? 50,
        //     MaxVolume = request.MaxVolume ?? 100,
        //     MinVolume = request.MinVolume ?? 0,
        //     RequestedBy = HttpContext.User?.Identity?.Name ?? "API"
        // };
        // return await HandleRequestAsync(command, cancellationToken);

        // Temporary implementation - return bad request
        return BadRequest(ApiResponse<ZoneResponse>.Fail("Zone creation not yet implemented."));
    }

    /// <summary>
    /// Updates an existing zone configuration.
    /// </summary>
    /// <param name="id">The unique identifier of the zone to update.</param>
    /// <param name="request">The zone update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated zone.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ZoneResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<ZoneResponse>>> UpdateZone(
        string id,
        [FromBody] UpdateZoneRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(ApiResponse<ZoneResponse>.Fail("Zone ID cannot be empty."));
        }

        if (request == null)
        {
            return BadRequest(ApiResponse<ZoneResponse>.Fail("Request body cannot be null."));
        }

        Logger.LogInformation("Updating zone: {ZoneId}", id);

        // TODO: Implement UpdateZoneCommand when Server layer features are created
        // var command = new UpdateZoneCommand(id)
        // {
        //     Name = request.Name,
        //     Description = request.Description,
        //     Location = request.Location,
        //     Color = request.Color,
        //     Icon = request.Icon,
        //     DefaultVolume = request.DefaultVolume,
        //     MaxVolume = request.MaxVolume,
        //     MinVolume = request.MinVolume,
        //     IsEnabled = request.IsEnabled,
        //     RequestedBy = HttpContext.User?.Identity?.Name ?? "API"
        // };
        // return await HandleRequestAsync(command, cancellationToken);

        // Temporary implementation - return not found
        return NotFound(ApiResponse<ZoneResponse>.Fail("Zone not found."));
    }

    /// <summary>
    /// Deletes a zone.
    /// </summary>
    /// <param name="id">The unique identifier of the zone to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response if the zone was deleted.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), 204)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse>> DeleteZone(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(ApiResponse.Fail("Zone ID cannot be empty."));
        }

        Logger.LogInformation("Deleting zone: {ZoneId}", id);

        // TODO: Implement DeleteZoneCommand when Server layer features are created
        // var command = new DeleteZoneCommand(id)
        // {
        //     RequestedBy = HttpContext.User?.Identity?.Name ?? "API"
        // };
        // var result = await HandleRequestAsync(command, cancellationToken);
        // if (result.Value?.Success == true)
        // {
        //     return NoContent();
        // }
        // return result;

        // Temporary implementation - return not found
        return NotFound(ApiResponse.Fail("Zone not found."));
    }
}

/// <summary>
/// Response model for zone operations.
/// </summary>
public class ZoneResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the zone.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the zone.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the zone.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the physical location of the zone.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the zone color (hex code) for UI display.
    /// </summary>
    public string Color { get; set; } = "#007bff";

    /// <summary>
    /// Gets or sets the zone icon identifier for UI display.
    /// </summary>
    public string Icon { get; set; } = "speaker";

    /// <summary>
    /// Gets or sets the default volume level for this zone (0-100).
    /// </summary>
    public int DefaultVolume { get; set; }

    /// <summary>
    /// Gets or sets the maximum volume level for this zone (0-100).
    /// </summary>
    public int MaxVolume { get; set; }

    /// <summary>
    /// Gets or sets the minimum volume level for this zone (0-100).
    /// </summary>
    public int MinVolume { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the zone is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the number of clients assigned to this zone.
    /// </summary>
    public int ClientCount { get; set; }

    /// <summary>
    /// Gets or sets the ID of the current audio stream playing in this zone.
    /// </summary>
    public string? CurrentStreamId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the zone was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the zone was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Creates a ZoneResponse from a Zone entity.
    /// </summary>
    /// <param name="zone">The zone entity.</param>
    /// <returns>A new ZoneResponse instance.</returns>
    public static ZoneResponse FromEntity(Zone zone)
    {
        ArgumentNullException.ThrowIfNull(zone);

        return new ZoneResponse
        {
            Id = zone.Id,
            Name = zone.Name,
            Description = zone.Description,
            Location = zone.Location,
            Color = zone.Color,
            Icon = zone.Icon,
            DefaultVolume = zone.DefaultVolume,
            MaxVolume = zone.MaxVolume,
            MinVolume = zone.MinVolume,
            IsEnabled = zone.IsEnabled,
            ClientCount = zone.ClientCount,
            CurrentStreamId = zone.CurrentStreamId,
            CreatedAt = zone.CreatedAt,
            UpdatedAt = zone.UpdatedAt,
        };
    }
}

/// <summary>
/// Request model for creating a new zone.
/// </summary>
public class CreateZoneRequest
{
    /// <summary>
    /// Gets or sets the display name for the zone.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the zone.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the physical location of the zone.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the zone color (hex code) for UI display.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the zone icon identifier for UI display.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the default volume level for this zone (0-100).
    /// </summary>
    public int? DefaultVolume { get; set; }

    /// <summary>
    /// Gets or sets the maximum volume level for this zone (0-100).
    /// </summary>
    public int? MaxVolume { get; set; }

    /// <summary>
    /// Gets or sets the minimum volume level for this zone (0-100).
    /// </summary>
    public int? MinVolume { get; set; }
}

/// <summary>
/// Request model for updating an existing zone.
/// </summary>
public class UpdateZoneRequest
{
    /// <summary>
    /// Gets or sets the display name for the zone.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the zone.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the physical location of the zone.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the zone color (hex code) for UI display.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the zone icon identifier for UI display.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the default volume level for this zone (0-100).
    /// </summary>
    public int? DefaultVolume { get; set; }

    /// <summary>
    /// Gets or sets the maximum volume level for this zone (0-100).
    /// </summary>
    public int? MaxVolume { get; set; }

    /// <summary>
    /// Gets or sets the minimum volume level for this zone (0-100).
    /// </summary>
    public int? MinVolume { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the zone is enabled.
    /// </summary>
    public bool? IsEnabled { get; set; }
}

/// <summary>
/// Response model for client operations.
/// </summary>
public class ClientResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the client.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the client.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the MAC address of the client.
    /// </summary>
    public required string MacAddress { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the client.
    /// </summary>
    public required string IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the current status of the client.
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets the current volume level (0-100).
    /// </summary>
    public int Volume { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the client is muted.
    /// </summary>
    public bool IsMuted { get; set; }

    /// <summary>
    /// Gets or sets the zone ID that this client is currently assigned to.
    /// </summary>
    public string? ZoneId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the client was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last time the client was seen/connected.
    /// </summary>
    public DateTime? LastSeen { get; set; }

    /// <summary>
    /// Creates a ClientResponse from a Client entity.
    /// </summary>
    /// <param name="client">The client entity.</param>
    /// <returns>A new ClientResponse instance.</returns>
    public static ClientResponse FromEntity(Client client)
    {
        ArgumentNullException.ThrowIfNull(client);

        return new ClientResponse
        {
            Id = client.Id,
            Name = client.Name,
            MacAddress = client.MacAddress.ToString(),
            IpAddress = client.IpAddress.ToString(),
            Status = client.Status.ToString(),
            Volume = client.Volume,
            IsMuted = client.IsMuted,
            ZoneId = client.ZoneId,
            CreatedAt = client.CreatedAt,
            LastSeen = client.LastSeen,
        };
    }
}
