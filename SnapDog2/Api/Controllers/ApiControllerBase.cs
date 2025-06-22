using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
public abstract class ApiControllerBase<T> : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<T> _logger;

    protected ApiControllerBase(IMediator mediator, ILogger<T> logger)
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
    protected ILogger<T> Logger => _logger;

    /// <summary>
    /// Handles a MediatR request and returns a standard ActionResult.
    /// </summary>
    /// <typeparam name="TResult">The type of data being returned.</typeparam>
    /// <param name="request">The MediatR request to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An ActionResult with the result or error.</returns>
    protected async Task<ActionResult<TResult>> HandleRequestAsync<TResult>(
        IRequest<Result<TResult>> request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var result = await _mediator.Send(request, cancellationToken);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return BadRequest(result.Error ?? "Unknown error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in {Controller}", GetType().Name);
            return StatusCode(500, "An internal server error occurred");
        }
    }

    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>An error API response.</returns>
}
