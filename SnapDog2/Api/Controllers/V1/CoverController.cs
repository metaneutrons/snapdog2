using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Models;

namespace SnapDog2.Api.Controllers.V1;

/// <summary>
/// Cover art controller for retrieving album/track cover images.
/// </summary>
[ApiController]
[Route("api/v1/cover")]
[Authorize]
public partial class CoverController : ControllerBase
{
    private readonly ISubsonicService _subsonicService;
    private readonly ILogger<CoverController> _logger;

    public CoverController(ISubsonicService subsonicService, ILogger<CoverController> logger)
    {
        _subsonicService = subsonicService;
        _logger = logger;
    }

    [LoggerMessage(EventId = 113100, Level = LogLevel.Warning, Message = "Cover art not found: {CoverId}"
)]
    private partial void LogCoverNotFound(string coverId);

    [LoggerMessage(EventId = 113101, Level = LogLevel.Error, Message = "Failed → retrieve cover art: {CoverId}"
)]
    private partial void LogCoverRetrievalFailed(Exception ex, string coverId);

    /// <summary>
    /// Gets cover art image by cover ID.
    /// </summary>
    /// <param name="coverId">Cover art identifier from Subsonic.</param>
    /// <returns>Binary image data with appropriate content type.</returns>
    /// <response code="200">Cover art retrieved successfully.</response>
    /// <response code="404">Cover art not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("{coverId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCover(string coverId)
    {
        if (string.IsNullOrWhiteSpace(coverId))
        {
            return BadRequest("Cover ID is required");
        }

        try
        {
            var coverResult = await _subsonicService.GetCoverArtAsync(coverId);

            if (!coverResult.IsSuccess || coverResult.Value == null)
            {
                LogCoverNotFound(coverId);
                return NotFound();
            }

            var coverData = coverResult.Value;

            // Add caching headers for performance
            Response.Headers.CacheControl = "public, max-age=3600";
            Response.Headers.ETag = $"\"{coverId}\"";

            return File(coverData.Data, coverData.ContentType);
        }
        catch (Exception ex)
        {
            LogCoverRetrievalFailed(ex, coverId);
            return StatusCode(500, "Failed to retrieve cover art");
        }
    }
}
