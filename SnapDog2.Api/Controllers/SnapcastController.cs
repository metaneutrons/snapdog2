using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Api.Models;
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
public class SnapcastController : ApiControllerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnapcastController"/> class.
    /// </summary>
    /// <param name="mediator">The MediatR mediator.</param>
    /// <param name="logger">The logger.</param>
    public SnapcastController(IMediator mediator, ILogger<SnapcastController> logger)
        : base(mediator, logger) { }

    /// <summary>
    /// Gets the current status of the Snapcast server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server status information.</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> GetServerStatus(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Getting Snapcast server status");

        var query = new GetSnapcastServerStatusQuery();
        var result = await Mediator.Send(query, cancellationToken);

        // Parse the JSON string to return as object
        var statusObject = System.Text.Json.JsonSerializer.Deserialize<object>(result ?? string.Empty);

        if (statusObject == null)
        {
            // Handle the case where deserialization results in null,
            // perhaps by returning an error or a default object.
            // For now, let's assume an empty object is acceptable if result was null or empty.
            return Ok(ApiResponse<object>.Ok(new object()));
        }

        return Ok(ApiResponse<object>.Ok(statusObject));
    }

    /// <summary>
    /// Gets all groups configured on the Snapcast server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of group identifiers.</returns>
    [HttpGet("groups")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IEnumerable<string>>>> GetGroups(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Getting Snapcast groups");

        var query = new GetSnapcastGroupsQuery();
        var result = await Mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<IEnumerable<string>>.Ok(result));
    }

    /// <summary>
    /// Gets all clients connected to the Snapcast server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of client identifiers.</returns>
    [HttpGet("clients")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IEnumerable<string>>>> GetClients(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Getting Snapcast clients");

        var query = new GetSnapcastClientsQuery();
        var result = await Mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<IEnumerable<string>>.Ok(result));
    }

    /// <summary>
    /// Sets the volume level for a specific Snapcast client.
    /// </summary>
    /// <param name="clientId">Unique identifier of the client.</param>
    /// <param name="request">Volume setting request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    [HttpPut("clients/{clientId}/volume")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> SetClientVolume(
        string clientId,
        [FromBody] VolumeRequest request,
        CancellationToken cancellationToken
    )
    {
        Logger.LogInformation("Setting volume for client {ClientId} to {Volume}", clientId, request.Volume);

        try
        {
            var command = new SetClientVolumeCommand(clientId, request.Volume);
            var result = await Mediator.Send(command, cancellationToken);

            // Assuming 'result' indicates success from the handler.
            // If the handler itself is supposed to throw for domain errors not caught by validation,
            // then this structure might need adjustment, but for ArgumentOutOfRangeException, this is fine.
            if (result)
            {
                return Ok(ApiResponse<bool>.Ok(true));
            }
            // If MediatR returns false without an exception, treat as a general failure.
            return ErrorResponse<bool>("Failed to set client volume. Operation returned false.", 500);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Logger.LogWarning(ex, "Invalid volume specified for client {ClientId}: {Volume}", clientId, request.Volume);
            return BadRequest(ApiResponse<bool>.Fail(ex.Message, new { ClientId = clientId, request.Volume }));
        }
        // Other unhandled exceptions would ideally be caught by global exception handlers.
    }

    /// <summary>
    /// Sets the mute state for a specific Snapcast client.
    /// </summary>
    /// <param name="clientId">Unique identifier of the client.</param>
    /// <param name="request">Mute setting request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    [HttpPut("clients/{clientId}/mute")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> SetClientMute(
        string clientId,
        [FromBody] MuteRequest request,
        CancellationToken cancellationToken
    )
    {
        Logger.LogInformation("Setting mute state for client {ClientId} to {Muted}", clientId, request.Muted);

        var command = new SetClientMuteCommand(clientId, request.Muted);
        var result = await Mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<bool>.Ok(result));
    }

    /// <summary>
    /// Assigns a specific stream to a group.
    /// </summary>
    /// <param name="groupId">Unique identifier of the group.</param>
    /// <param name="request">Stream assignment request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    [HttpPut("groups/{groupId}/stream")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> SetGroupStream(
        string groupId,
        [FromBody] StreamRequest request,
        CancellationToken cancellationToken
    )
    {
        Logger.LogInformation("Setting stream {StreamId} for group {GroupId}", request.StreamId, groupId);

        var command = new SetGroupStreamCommand(groupId, request.StreamId);
        var result = await Mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<bool>.Ok(result));
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
