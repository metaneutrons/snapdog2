using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;
using SnapDog2.Core.State;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Server.Features.AudioStreams.Commands;
using SnapDog2.Server.Features.AudioStreams.Queries;
using SnapDog2.Server.Models;

/// <summary>
/// Audio streams management endpoints for creating, configuring, and controlling audio streams.
/// Provides CRUD operations and stream control functionality for the SnapDog2 multi-room audio system.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class AudioStreamsController : ControllerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AudioStreamsController"/> class.
    /// </summary>
    /// <param name="mediator">The MediatR instance for handling commands and queries.</param>
    /// <param name="logger">The logger instance for this controller.</param>
    private readonly IKnxService _knxService;
    private readonly IMqttService _mqttService;
    private readonly ISnapcastService _snapcastService;
    private readonly IStateManager _stateManager;
    private readonly IMediator _mediator;
    private readonly ILogger<AudioStreamsController> _logger;

    public AudioStreamsController(
        IMediator mediator,
        ILogger<AudioStreamsController> logger,
        IKnxService knxService,
        IMqttService mqttService,
        ISnapcastService snapcastService,
        IStateManager stateManager
    )
    {
        _mediator = mediator;
        _logger = logger;
        _knxService = knxService;
        _mqttService = mqttService;
        _snapcastService = snapcastService;
        _stateManager = stateManager;
    }

    /// <summary>
    /// Gets all audio streams in the system.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive/stopped streams in the results.</param>
    /// <param name="includeDetails">Whether to include detailed information in the response.</param>
    /// <param name="limit">Maximum number of streams to return (for pagination).</param>
    /// <param name="skip">Number of streams to skip (for pagination).</param>
    /// <param name="orderBy">Field to order the results by (name, created, status, codec).</param>
    /// <param name="descending">Whether to sort in descending order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all audio streams.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AudioStreamResponse>), 200)]
    [ProducesResponseType(typeof(void), 400)]
    public async Task<ActionResult<IEnumerable<AudioStreamResponse>>> GetAllAudioStreams(
        [FromQuery] bool includeInactive = true,
        [FromQuery] bool includeDetails = true,
        [FromQuery] int? limit = null,
        [FromQuery] int? skip = null,
        [FromQuery] string? orderBy = null,
        [FromQuery] bool descending = false,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Getting all audio streams with filters: includeInactive={IncludeInactive}, includeDetails={IncludeDetails}",
            includeInactive,
            includeDetails
        );

        var query = new GetAllAudioStreamsQuery
        {
            IncludeInactive = includeInactive,
            IncludeDetails = includeDetails,
            Limit = limit,
            Skip = skip,
            OrderBy = orderBy,
            Descending = descending,
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a specific audio stream by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the audio stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The audio stream with the specified ID.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AudioStreamResponse), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(void), 400)]
    public async Task<ActionResult<AudioStreamResponse>> GetAudioStreamById(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Stream ID cannot be empty.");
        }

        _logger.LogInformation("Getting audio stream by ID: {StreamId}", id);

        var query = new GetAudioStreamByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets all active (playing or starting) audio streams.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active audio streams.</returns>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<AudioStreamResponse>), 200)]
    [ProducesResponseType(typeof(void), 400)]
    public async Task<ActionResult<IEnumerable<AudioStreamResponse>>> GetActiveAudioStreams(
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Getting active audio streams");

        var query = new GetActiveAudioStreamsQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets audio streams by codec type.
    /// </summary>
    /// <param name="codec">The audio codec to filter by (PCM, FLAC, MP3, AAC, OGG).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of audio streams using the specified codec.</returns>
    [HttpGet("codec/{codec}")]
    [ProducesResponseType(typeof(IEnumerable<AudioStreamResponse>), 200)]
    [ProducesResponseType(typeof(void), 400)]
    public async Task<ActionResult<IEnumerable<AudioStreamResponse>>> GetStreamsByCodec(
        string codec,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(codec))
        {
            return BadRequest("Codec cannot be empty.");
        }

        if (!Enum.TryParse<AudioCodec>(codec, true, out var audioCodec))
        {
            return BadRequest($"Invalid codec: {codec}. Valid values are: PCM, FLAC, MP3, AAC, OGG.");
        }

        _logger.LogInformation("Getting audio streams by codec: {Codec}", audioCodec);

        var query = new GetStreamsByCodecQuery(audioCodec);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new audio stream.
    /// </summary>
    /// <param name="request">The audio stream creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created audio stream.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AudioStreamResponse), 201)]
    [ProducesResponseType(typeof(void), 400)]
    public async Task<ActionResult<AudioStreamResponse>> CreateAudioStream(
        [FromBody] CreateAudioStreamRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (request == null)
        {
            return BadRequest("Request body cannot be null.");
        }

        _logger.LogInformation("Creating new audio stream: {StreamName}", request.Name);

        try
        {
            var streamUrl = new StreamUrl(request.Url);
            var command = new CreateAudioStreamCommand(
                request.Name,
                streamUrl,
                request.Codec,
                request.SampleRate,
                request.Description ?? ""
            )
            {
                BitrateKbps = request.BitrateKbps,
                Channels = request.Channels,
                Tags = request.Tags,
                RequestedBy = HttpContext.User?.Identity?.Name ?? "API",
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                var response = AudioStreamResponse.FromEntity(result.Value!);
                return CreatedAtAction(nameof(GetAudioStreamById), new { id = result.Value!.Id }, response);
            }

            return BadRequest(result.Error ?? "Failed to create audio stream");
        }
        catch (ArgumentException ex)
        {
            return BadRequest($"Invalid request: {ex.Message}");
        }
    }

    /// <summary>
    /// Starts an audio stream.
    /// </summary>
    /// <param name="id">The unique identifier of the audio stream to start.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response if the stream was started.</returns>
    [HttpPut("{id}/start")]
    [ProducesResponseType(typeof(void), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(void), 400)]
    public async Task<IActionResult> StartAudioStream(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Stream ID cannot be empty.");
        }

        _logger.LogInformation("Starting audio stream: {StreamId}", id);

        var command = new StartAudioStreamCommand(id) { RequestedBy = HttpContext.User?.Identity?.Name ?? "API" };

        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok();
        }
        return BadRequest(result.Error ?? "Failed to start audio stream");
    }

    /// <summary>
    /// Stops an audio stream.
    /// </summary>
    /// <param name="id">The unique identifier of the audio stream to stop.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response if the stream was stopped.</returns>
    [HttpPut("{id}/stop")]
    [ProducesResponseType(typeof(void), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(void), 400)]
    public async Task<IActionResult> StopAudioStream(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Stream ID cannot be empty.");
        }

        _logger.LogInformation("Stopping audio stream: {StreamId}", id);

        var command = new StopAudioStreamCommand(id) { RequestedBy = HttpContext.User?.Identity?.Name ?? "API" };

        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok();
        }
        return BadRequest(result.Error ?? "Failed to stop audio stream");
    }

    /// <summary>
    /// Deletes an audio stream.
    /// </summary>
    /// <param name="id">The unique identifier of the audio stream to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response if the stream was deleted.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(void), 204)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(void), 400)]
    public async Task<IActionResult> DeleteAudioStream(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Stream ID cannot be empty.");
        }

        _logger.LogInformation("Deleting audio stream: {StreamId}", id);

        var command = new DeleteAudioStreamCommand(id) { RequestedBy = HttpContext.User?.Identity?.Name ?? "API" };

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return BadRequest(result.Error ?? "Failed to delete audio stream");
    }
}

/// <summary>
/// Request model for creating a new audio stream.
/// </summary>
public class CreateAudioStreamRequest
{
    /// <summary>
    /// Gets or sets the display name for the audio stream.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the URL of the audio stream source.
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Gets or sets the audio codec used by the stream.
    /// </summary>
    public required AudioCodec Codec { get; set; }

    /// <summary>
    /// Gets or sets the sample rate of the audio stream in Hz.
    /// </summary>
    public required int SampleRate { get; set; }

    /// <summary>
    /// Gets or sets the description of the audio stream.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the bitrate in kilobits per second (optional, will be auto-detected if not specified).
    /// </summary>
    public int? BitrateKbps { get; set; }

    /// <summary>
    /// Gets or sets the number of audio channels (optional, will be auto-detected if not specified).
    /// </summary>
    public int? Channels { get; set; }

    /// <summary>
    /// Gets or sets additional metadata or tags for the stream (optional).
    /// </summary>
    public string? Tags { get; set; }
}
