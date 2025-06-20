using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Api.Models;
using SnapDog2.Core.Configuration;
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
public class KnxController : ApiControllerBase
{
    /// <summary>
    /// Initializes a new instance of the KnxController.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance.</param>
    /// <param name="logger">The logger instance.</param>
    public KnxController(IMediator mediator, ILogger<KnxController> logger)
        : base(mediator, logger) { }

    /// <summary>
    /// Gets the current KNX connection status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The current KNX connection status.</returns>
    /// <response code="200">Connection status retrieved successfully</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="500">Internal server error or KNX service unavailable</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<KnxConnectionStatus>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<KnxConnectionStatus>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<KnxConnectionStatus>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<KnxConnectionStatus>>> GetConnectionStatus(
        CancellationToken cancellationToken = default
    )
    {
        Logger.LogInformation("Getting KNX connection status");
        try
        {
            var query = new GetKnxConnectionStatusQuery();
            var result = await Mediator.Send(query, cancellationToken);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting KNX connection status.");
            return ErrorResponse<KnxConnectionStatus>(
                "An unexpected error occurred while getting KNX connection status.",
                (int)System.Net.HttpStatusCode.InternalServerError
            );
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
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<KnxDeviceInfo>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<KnxDeviceInfo>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<KnxDeviceInfo>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IEnumerable<KnxDeviceInfo>>>> GetDevices(
        CancellationToken cancellationToken = default
    )
    {
        Logger.LogInformation("Getting KNX devices");
        try
        {
            var query = new GetKnxDevicesQuery();
            var result = await Mediator.Send(query, cancellationToken);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting KNX devices.");
            return ErrorResponse<IEnumerable<KnxDeviceInfo>>( // Corrected generic type argument
                "An unexpected error occurred while getting KNX devices.",
                (int)System.Net.HttpStatusCode.InternalServerError
            );
        }
    }

    /// <summary>
    /// Writes a value to a specific KNX group address.
    /// </summary>
    /// <param name="command">The write group value command containing address, value, and options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Success response if value was written successfully.</returns>
    /// <response code="200">Value written successfully</response>
    /// <response code="400">Invalid request parameters or KNX address format</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="500">Internal server error or KNX service unavailable</response>
    [HttpPost("write")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> WriteGroupValue(
        [FromBody] WriteKnxValueRequest request,
        CancellationToken cancellationToken = default
    )
    {
        Logger.LogInformation("Writing KNX group value to address: {Address}", request.Address);
        try
        {
            var command = new WriteGroupValueCommand
            {
                Address = KnxAddress.Parse(request.Address),
                Value = Convert.FromBase64String(request.Value),
                Description = request.Description,
            };
            var result = await Mediator.Send(command, cancellationToken);
            return SuccessResponse(result);
        }
        catch (FormatException ex) // Catches KnxAddress.Parse and Convert.FromBase64String errors
        {
            Logger.LogWarning(
                ex,
                "Invalid format for KNX write request. Address: {Address}, Value: {Value}",
                request.Address,
                request.Value
            );
            return ErrorResponse<bool>(
                $"Invalid request format: {ex.Message}",
                (int)System.Net.HttpStatusCode.BadRequest
            );
        }
        catch (ArgumentException ex) // Catches other argument errors from parsing or command creation
        {
            Logger.LogWarning(
                ex,
                "Invalid argument for KNX write request. Address: {Address}, Value: {Value}",
                request.Address,
                request.Value
            );
            return ErrorResponse<bool>(
                $"Invalid request argument: {ex.Message}",
                (int)System.Net.HttpStatusCode.BadRequest
            );
        }
        catch (FluentValidation.ValidationException ex)
        {
            Logger.LogWarning(
                "Validation failed for KNX write request: {Address}, Errors: {Errors}",
                request.Address,
                ex.Errors
            );
            return ErrorResponse<bool>(
                string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)),
                (int)System.Net.HttpStatusCode.BadRequest
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error writing KNX group value for address: {Address}", request.Address);
            return ErrorResponse<bool>(
                "An unexpected error occurred while writing KNX value.",
                (int)System.Net.HttpStatusCode.InternalServerError
            );
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
    [ProducesResponseType(typeof(ApiResponse<byte[]?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<byte[]?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<byte[]?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<byte[]?>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<byte[]?>>> ReadGroupValue(
        [FromBody] ReadKnxValueRequest request,
        CancellationToken cancellationToken = default
    )
    {
        Logger.LogInformation("Reading KNX group value from address: {Address}", request.Address);
        try
        {
            var command = new ReadGroupValueCommand
            {
                Address = KnxAddress.Parse(request.Address),
                Description = request.Description,
            };
            var result = await Mediator.Send(command, cancellationToken);
            return SuccessResponse(result);
        }
        catch (FormatException ex)
        {
            Logger.LogWarning(ex, "Invalid KNX address format for read request: {Address}", request.Address);
            return ErrorResponse<byte[]?>(
                $"Invalid KNX address format: {ex.Message}",
                (int)System.Net.HttpStatusCode.BadRequest
            );
        }
        catch (FluentValidation.ValidationException ex)
        {
            Logger.LogWarning(
                "Validation failed for KNX read request: {Address}, Errors: {Errors}",
                request.Address,
                ex.Errors
            );
            return ErrorResponse<byte[]?>(
                string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)),
                (int)System.Net.HttpStatusCode.BadRequest
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error reading KNX group value for address: {Address}", request.Address);
            return ErrorResponse<byte[]?>(
                "An unexpected error occurred while reading KNX value.",
                (int)System.Net.HttpStatusCode.InternalServerError
            );
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
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> SubscribeToGroup(
        [FromBody] SubscribeKnxRequest request,
        CancellationToken cancellationToken = default
    )
    {
        Logger.LogInformation("Subscribing to KNX group address: {Address}", request.Address);
        try
        {
            var command = new SubscribeToGroupCommand
            {
                Address = KnxAddress.Parse(request.Address),
                Description = request.Description,
            };
            var result = await Mediator.Send(command, cancellationToken);
            return SuccessResponse(result);
        }
        catch (FormatException ex)
        {
            Logger.LogWarning(ex, "Invalid KNX address format for subscribe request: {Address}", request.Address);
            return ErrorResponse<bool>(
                $"Invalid KNX address format: {ex.Message}",
                (int)System.Net.HttpStatusCode.BadRequest
            );
        }
        catch (FluentValidation.ValidationException ex)
        {
            Logger.LogWarning(
                "Validation failed for KNX subscribe request: {Address}, Errors: {Errors}",
                request.Address,
                ex.Errors
            );
            return ErrorResponse<bool>(
                string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)),
                (int)System.Net.HttpStatusCode.BadRequest
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error subscribing to KNX group address: {Address}", request.Address);
            return ErrorResponse<bool>(
                "An unexpected error occurred while subscribing to KNX address.",
                (int)System.Net.HttpStatusCode.InternalServerError
            );
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
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> UnsubscribeFromGroup(
        [FromBody] UnsubscribeKnxRequest request,
        CancellationToken cancellationToken = default
    )
    {
        Logger.LogInformation("Unsubscribing from KNX group address: {Address}", request.Address);
        try
        {
            var command = new UnsubscribeFromGroupCommand
            {
                Address = KnxAddress.Parse(request.Address),
                Description = request.Description,
            };
            var result = await Mediator.Send(command, cancellationToken);
            return SuccessResponse(result);
        }
        catch (FormatException ex)
        {
            Logger.LogWarning(ex, "Invalid KNX address format for unsubscribe request: {Address}", request.Address);
            return ErrorResponse<bool>(
                $"Invalid KNX address format: {ex.Message}",
                (int)System.Net.HttpStatusCode.BadRequest
            );
        }
        catch (FluentValidation.ValidationException ex)
        {
            Logger.LogWarning(
                "Validation failed for KNX unsubscribe request: {Address}, Errors: {Errors}",
                request.Address,
                ex.Errors
            );
            return ErrorResponse<bool>(
                string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)),
                (int)System.Net.HttpStatusCode.BadRequest
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error unsubscribing from KNX group address: {Address}", request.Address);
            return ErrorResponse<bool>(
                "An unexpected error occurred while unsubscribing from KNX address.",
                (int)System.Net.HttpStatusCode.InternalServerError
            );
        }
    }
}
// Ensure there's a closing brace for the namespace if it was accidentally removed or not part of the original snippet.
