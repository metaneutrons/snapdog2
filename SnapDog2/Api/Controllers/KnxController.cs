using System; // For ArgumentOutOfRangeException
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Infrastructure.Services.Models;
using SnapDog2.Server.Features.Knx.Commands;
using SnapDog2.Server.Features.Knx.Queries;

namespace SnapDog2.Api.Controllers;

/// <summary>
/// Controller for KNX operations including group value read/write, subscription management, and connection status monitoring.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KnxController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<KnxController> _logger;

    public KnxController(IMediator mediator, ILogger<KnxController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current KNX connection status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The current KNX connection status.</returns>
    /// <response code="200">Connection status retrieved successfully</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="500">Internal server error or KNX service unavailable</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(KnxConnectionStatus), 200)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 500)]
    public async Task<ActionResult<KnxConnectionStatus>> GetConnectionStatus(
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Getting KNX connection status");
        try
        {
            var query = new GetKnxConnectionStatusQuery();
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting KNX connection status.");
            return StatusCode(500, "An unexpected error occurred while getting KNX connection status.");
        }
    }

    /// <summary>
    /// Gets information about KNX devices and group addresses.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>List of KNX device information.</returns>
    /// <response code="200">Device information retrieved successfully</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="500">Internal server error or KNX service unavailable</response>
    [HttpGet("devices")]
    [ProducesResponseType(typeof(IEnumerable<KnxDeviceInfo>), 200)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 500)]
    public async Task<ActionResult<IEnumerable<KnxDeviceInfo>>> GetDevices(
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Getting KNX devices");
        try
        {
            var query = new GetKnxDevicesQuery();
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting KNX devices.");
            return StatusCode(500, "An unexpected error occurred while getting KNX devices.");
        }
    }

    /// <summary>
    /// Writes a value to a specific KNX group address.
    /// </summary>
    /// <param name="request">The write group value request containing address, value, and options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Success response if value was written successfully.</returns>
    /// <response code="200">Value written successfully</response>
    /// <response code="400">Invalid request parameters or KNX address format</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="500">Internal server error or KNX service unavailable</response>
    [HttpPost("write")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 500)]
    public async Task<ActionResult<bool>> WriteGroupValue(
        [FromBody] WriteKnxValueRequest request,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Writing KNX group value to address: {Address}", request.Address);
        try
        {
            var command = new WriteGroupValueCommand
            {
                Address = KnxAddress.Parse(request.Address),
                Value = Convert.FromBase64String(request.Value),
                Description = request.Description,
            };
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(
                ex,
                "Invalid format for KNX write request. Address: {Address}, Value: {Value}",
                request.Address,
                request.Value
            );
            return BadRequest($"Invalid request format: {ex.Message}");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning(
                ex,
                "Argument out of range for KNX write request. Address: {Address}, Value: {Value}. Error: {ErrorMessage}",
                request.Address,
                request.Value,
                ex.Message
            );
            return BadRequest($"Invalid request argument: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                ex,
                "Invalid argument for KNX write request. Address: {Address}, Value: {Value}",
                request.Address,
                request.Value
            );
            return BadRequest($"Invalid request argument: {ex.Message}");
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning(
                "Validation failed for KNX write request: {Address}, Errors: {Errors}",
                request.Address,
                ex.Errors
            );
            return BadRequest(string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing KNX group value for address: {Address}", request.Address);
            return StatusCode(500, "An unexpected error occurred while writing KNX value.");
        }
    }

    /// <summary>
    /// Reads a value from a specific KNX group address.
    /// </summary>
    /// <param name="request">The read group value request containing address and options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The value read from the group address as byte array.</returns>
    /// <response code="200">Value read successfully</response>
    /// <response code="400">Invalid request parameters or KNX address format</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="500">Internal server error or KNX service unavailable</response>
    [HttpPost("read")]
    [ProducesResponseType(typeof(byte[]), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 500)]
    public async Task<ActionResult<byte[]>> ReadGroupValue(
        [FromBody] ReadKnxValueRequest request,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Reading KNX group value from address: {Address}", request.Address);
        try
        {
            var command = new ReadGroupValueCommand
            {
                Address = KnxAddress.Parse(request.Address),
                Description = request.Description,
            };
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid KNX address format for read request: {Address}", request.Address);
            return BadRequest($"Invalid KNX address format: {ex.Message}");
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning(
                "Validation failed for KNX read request: {Address}, Errors: {Errors}",
                request.Address,
                ex.Errors
            );
            return BadRequest(string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading KNX group value for address: {Address}", request.Address);
            return StatusCode(500, "An unexpected error occurred while reading KNX value.");
        }
    }

    /// <summary>
    /// Subscribes to value changes on a specific KNX group address.
    /// </summary>
    /// <param name="request">The subscribe request containing address and options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Success response if subscription was created successfully.</returns>
    /// <response code="200">Subscription created successfully</response>
    /// <response code="400">Invalid request parameters or KNX address format</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="500">Internal server error or KNX service unavailable</response>
    [HttpPost("subscribe")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 500)]
    public async Task<ActionResult<bool>> SubscribeToGroup(
        [FromBody] SubscribeKnxRequest request,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Subscribing to KNX group address: {Address}", request.Address);
        try
        {
            var command = new SubscribeToGroupCommand
            {
                Address = KnxAddress.Parse(request.Address),
                Description = request.Description,
            };
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid KNX address format for subscribe request: {Address}", request.Address);
            return BadRequest($"Invalid KNX address format: {ex.Message}");
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning(
                "Validation failed for KNX subscribe request: {Address}, Errors: {Errors}",
                request.Address,
                ex.Errors
            );
            return BadRequest(string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to KNX group address: {Address}", request.Address);
            return StatusCode(500, "An unexpected error occurred while subscribing to KNX address.");
        }
    }

    /// <summary>
    /// Unsubscribes from value changes on a specific KNX group address.
    /// </summary>
    /// <param name="request">The unsubscribe request containing address and options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Success response if unsubscription was successful.</returns>
    /// <response code="200">Unsubscription successful</response>
    /// <response code="400">Invalid request parameters or KNX address format</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="500">Internal server error or KNX service unavailable</response>
    [HttpPost("unsubscribe")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 500)]
    public async Task<ActionResult<bool>> UnsubscribeFromGroup(
        [FromBody] UnsubscribeKnxRequest request,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Unsubscribing from KNX group address: {Address}", request.Address);
        try
        {
            var command = new UnsubscribeFromGroupCommand
            {
                Address = KnxAddress.Parse(request.Address),
                Description = request.Description,
            };
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid KNX address format for unsubscribe request: {Address}", request.Address);
            return BadRequest($"Invalid KNX address format: {ex.Message}");
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning(
                "Validation failed for KNX unsubscribe request: {Address}, Errors: {Errors}",
                request.Address,
                ex.Errors
            );
            return BadRequest(string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from KNX group address: {Address}", request.Address);
            return StatusCode(500, "An unexpected error occurred while unsubscribing from KNX address.");
        }
    }
}
// Ensure there's a closing brace for the namespace if it was accidentally removed or not part of the original snippet.
