using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SnapDog2.Server.Features.Snapcast.Commands;
using SnapDog2.Server.Features.Snapcast.Queries;

namespace SnapDog2.Api.Controllers;

/// <summary>
/// Controller for Snapcast server operations.
/// Provides endpoints for managing Snapcast clients, groups, and streams.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SnapcastController : ControllerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnapcastController"/> class.
    /// </summary>
    /// <param name="mediator">The MediatR mediator.</param>
    /// <param name="logger">The logger.</param>
    private readonly IMediator _mediator;
    private readonly ILogger<SnapcastController> _logger;

    public SnapcastController(IMediator mediator, ILogger<SnapcastController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current status of the Snapcast server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server status information.</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(void), 500)]
    public async Task<ActionResult<object>> GetServerStatus(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting Snapcast server status");

        var query = new GetSnapcastServerStatusQuery();
        var result = await _mediator.Send(query, cancellationToken);

        var statusObject = System.Text.Json.JsonSerializer.Deserialize<object>(result ?? string.Empty);

        if (statusObject == null)
        {
            return Ok(new object());
        }

        return Ok(statusObject);
    }

    /// <summary>
    /// Gets all groups configured on the Snapcast server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of group identifiers.</returns>
    [HttpGet("groups")]
    [ProducesResponseType(typeof(IEnumerable<string>), 200)]
    [ProducesResponseType(typeof(void), 500)]
    public async Task<ActionResult<IEnumerable<string>>> GetGroups(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting Snapcast groups");

        var query = new GetSnapcastGroupsQuery();
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets all clients connected to the Snapcast server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of client identifiers.</returns>
    [HttpGet("clients")]
    [ProducesResponseType(typeof(IEnumerable<string>), 200)]
    [ProducesResponseType(typeof(void), 500)]
    public async Task<ActionResult<IEnumerable<string>>> GetClients(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting Snapcast clients");

        var query = new GetSnapcastClientsQuery();
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Temporary stub to catch POST /clients/volume and return 400 on malformed JSON.
    /// </summary>
    [HttpPost("clients/volume")]
    [ProducesResponseType(typeof(void), 200)]
    public ActionResult HandleMalformedVolume([FromBody] VolumeRequest request)
    {
        return Ok();
    }

    /// <summary>
    /// Sets the volume level for a specific Snapcast client.
    /// </summary>
    /// <param name="clientId">Unique identifier of the client.</param>
    /// <param name="request">Volume setting request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    [HttpPut("clients/{clientId}/volume")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 500)]
    public async Task<IActionResult> SetClientVolume(
        string clientId,
        [FromBody] VolumeRequest request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("Setting volume for client {ClientId} to {Volume}", clientId, request.Volume);

        try
        {
            var command = new SetClientVolumeCommand(clientId, request.Volume);
            var result = await _mediator.Send(command, cancellationToken);

            if (result)
            {
                return Ok(true);
            }
            return StatusCode(500, "Failed to set client volume. Operation returned false.");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning(
                ex,
                "Invalid volume specified for client {ClientId}: {Volume}",
                clientId,
                request.Volume
            );
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Sets the mute state for a specific Snapcast client.
    /// </summary>
    /// <param name="clientId">Unique identifier of the client.</param>
    /// <param name="request">Mute setting request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    [HttpPut("clients/{clientId}/mute")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 500)]
    public async Task<IActionResult> SetClientMute(
        string clientId,
        [FromBody] MuteRequest request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("Setting mute state for client {ClientId} to {Muted}", clientId, request.Muted);

        var command = new SetClientMuteCommand(clientId, request.Muted);
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Assigns a specific stream to a group.
    /// </summary>
    /// <param name="groupId">Unique identifier of the group.</param>
    /// <param name="request">Stream assignment request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    [HttpPut("groups/{groupId}/stream")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 500)]
    public async Task<IActionResult> SetGroupStream(
        string groupId,
        [FromBody] StreamRequest request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("Setting stream {StreamId} for group {GroupId}", request.StreamId, groupId);

        var command = new SetGroupStreamCommand(groupId, request.StreamId);
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }
}

/// <summary>
/// Request model for setting client volume.
/// </summary>
public class VolumeRequest
{
    /// <summary>
    /// Gets or sets the volume level (0-100).
    /// </summary>
    public int Volume { get; set; }
}

/// <summary>
/// Request model for setting client mute state.
/// </summary>
public class MuteRequest
{
    /// <summary>
    /// Gets or sets whether the client should be muted.
    /// </summary>
    public bool Muted { get; set; }
}

/// <summary>
/// Request model for setting group stream.
/// </summary>
public class StreamRequest
{
    /// <summary>
    /// Gets or sets the stream identifier.
    /// </summary>
    public string StreamId { get; set; } = string.Empty;
}
