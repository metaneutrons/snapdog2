using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Api.Models;
using SnapDog2.Core.Common;

namespace SnapDog2.Api.Controllers;

/// <summary>
/// Base controller class for all API controllers.
/// Provides common functionality including MediatR integration, standardized response handling, and error mapping.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    protected ApiControllerBase(IMediator mediator, ILogger logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the MediatR instance for sending commands and queries.
    /// </summary>
    protected IMediator Mediator => _mediator;

    /// <summary>
    /// Gets the logger instance for this controller.
    /// </summary>
    protected ILogger Logger => _logger;

    /// <summary>
    /// Handles a MediatR request and returns a standardized API response.
    /// </summary>
    /// <typeparam name="T">The type of data being returned.</typeparam>
    /// <param name="request">The MediatR request to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A standardized API response.</returns>
    protected async Task<ActionResult<ApiResponse<T>>> HandleRequestAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var result = await _mediator.Send(request, cancellationToken);
            return MapResultToResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in {Controller}", GetType().Name);
            return StatusCode(500, ApiResponse<T>.Fail("An internal server error occurred"));
        }
    }

    /// <summary>
    /// Handles a MediatR request that doesn't return data and returns a standardized API response.
    /// </summary>
    /// <param name="request">The MediatR request to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A standardized API response.</returns>
    protected async Task<ActionResult<ApiResponse>> HandleRequestAsync(
        IRequest<Result> request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var result = await _mediator.Send(request, cancellationToken);
            return MapResultToResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in {Controller}", GetType().Name);
            return StatusCode(500, ApiResponse.Fail("An internal server error occurred"));
        }
    }

    /// <summary>
    /// Maps a Result to an ActionResult with appropriate HTTP status codes.
    /// </summary>
    /// <typeparam name="T">The type of data in the result.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <returns>An ActionResult with the appropriate status code and response.</returns>
    protected ActionResult<ApiResponse<T>> MapResultToResponse<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<T>.Ok(result.Value!));
        }

        return result.Error switch
        {
            "NotFound" => NotFound(ApiResponse<T>.Fail(result.Error)),
            "Validation" => BadRequest(ApiResponse<T>.Fail(result.Error)),
            "Unauthorized" => Unauthorized(ApiResponse<T>.Fail(result.Error)),
            "Forbidden" => Forbid(),
            "Conflict" => Conflict(ApiResponse<T>.Fail(result.Error)),
            _ => BadRequest(ApiResponse<T>.Fail(result.Error ?? "Unknown error")),
        };
    }

    /// <summary>
    /// Maps a Result to an ActionResult with appropriate HTTP status codes.
    /// </summary>
    /// <param name="result">The result to map.</param>
    /// <returns>An ActionResult with the appropriate status code and response.</returns>
    protected ActionResult<ApiResponse> MapResultToResponse(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok(ApiResponse.Ok());
        }

        return result.Error switch
        {
            "NotFound" => NotFound(ApiResponse.Fail(result.Error)),
            "Validation" => BadRequest(ApiResponse.Fail(result.Error)),
            "Unauthorized" => Unauthorized(ApiResponse.Fail(result.Error)),
            "Forbidden" => Forbid(),
            "Conflict" => Conflict(ApiResponse.Fail(result.Error)),
            _ => BadRequest(ApiResponse.Fail(result.Error ?? "Unknown error")),
        };
    }

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    /// <typeparam name="T">The type of data being returned.</typeparam>
    /// <param name="data">The data to return.</param>
    /// <returns>A successful API response.</returns>
    protected ActionResult<ApiResponse<T>> SuccessResponse<T>(T data)
    {
        return Ok(ApiResponse<T>.Ok(data));
    }

    /// <summary>
    /// Creates a successful response without data.
    /// </summary>
    /// <returns>A successful API response.</returns>
    protected ActionResult<ApiResponse> SuccessResponse()
    {
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Creates a successful response with a message.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <returns>A successful API response.</returns>
    protected ActionResult<ApiResponse> SuccessResponse(string message)
    {
        return Ok(ApiResponse.Ok(message));
    }

    /// <summary>
    /// Creates an error response.
    /// </summary>
    /// <typeparam name="T">The type of data being returned.</typeparam>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>An error API response.</returns>
    protected ActionResult<ApiResponse<T>> ErrorResponse<T>(string message, int statusCode = 400)
    {
        return StatusCode(statusCode, ApiResponse<T>.Fail(message));
    }

    /// <summary>
    /// Creates an error response without data.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>An error API response.</returns>
    protected ActionResult<ApiResponse> ErrorResponse(string message, int statusCode = 400)
    {
        return StatusCode(statusCode, ApiResponse.Fail(message));
    }
}
