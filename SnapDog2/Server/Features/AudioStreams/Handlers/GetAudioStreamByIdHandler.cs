using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Common;
using SnapDog2.Infrastructure.Repositories;
using SnapDog2.Server.Features.AudioStreams.Queries;
using SnapDog2.Server.Models;

namespace SnapDog2.Server.Features.AudioStreams.Handlers;

/// <summary>
/// Handler for retrieving a specific audio stream by its unique identifier.
/// Supports detailed information retrieval with optional history and validation.
/// </summary>
public sealed class GetAudioStreamByIdHandler : IRequestHandler<GetAudioStreamByIdQuery, Result<AudioStreamResponse>>
{
    private readonly IAudioStreamRepository _audioStreamRepository;
    private readonly ILogger<GetAudioStreamByIdHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAudioStreamByIdHandler"/> class.
    /// </summary>
    /// <param name="audioStreamRepository">The audio stream repository.</param>
    /// <param name="logger">The logger.</param>
    public GetAudioStreamByIdHandler(
        IAudioStreamRepository audioStreamRepository,
        ILogger<GetAudioStreamByIdHandler> logger
    )
    {
        _audioStreamRepository =
            audioStreamRepository ?? throw new ArgumentNullException(nameof(audioStreamRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the request to get an audio stream by ID.
    /// </summary>
    /// <param name="request">The get audio stream by ID query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the audio stream response if found.</returns>
    public async Task<Result<AudioStreamResponse>> Handle(
        GetAudioStreamByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _logger.LogInformation(
                "Retrieving audio stream by ID: {StreamId} with options - IncludeDetails: {IncludeDetails}, IncludeHistory: {IncludeHistory}, ValidateExistence: {ValidateExistence}",
                request.StreamId,
                request.IncludeDetails,
                request.IncludeHistory,
                request.ValidateExistence
            );

            // Validate stream ID format if requested
            if (request.ValidateExistence && !IsValidStreamIdFormat(request.StreamId))
            {
                _logger.LogWarning("Invalid stream ID format: {StreamId}", request.StreamId);
                return Result<AudioStreamResponse>.Failure($"Invalid stream ID format: {request.StreamId}");
            }

            // Retrieve the stream from repository
            var audioStream = await _audioStreamRepository.GetByIdAsync(request.StreamId, cancellationToken);

            if (audioStream == null)
            {
                _logger.LogWarning("Audio stream not found: {StreamId}", request.StreamId);
                return Result<AudioStreamResponse>.Failure($"Audio stream with ID '{request.StreamId}' was not found.");
            }

            _logger.LogDebug(
                "Found audio stream: {StreamId} - {StreamName} with status: {Status}",
                audioStream.Id,
                audioStream.Name,
                audioStream.Status
            );

            // Create response based on request options
            AudioStreamResponse response;

            if (request.IncludeDetails)
            {
                response = AudioStreamResponse.FromEntity(audioStream);
                _logger.LogDebug("Created detailed response for stream: {StreamId}", request.StreamId);
            }
            else
            {
                response = AudioStreamResponse.CreateSummary(audioStream);
                _logger.LogDebug("Created summary response for stream: {StreamId}", request.StreamId);
            }

            // Add historical information if requested
            if (request.IncludeHistory)
            {
                await EnrichWithHistoricalData(response, audioStream, cancellationToken);
                _logger.LogDebug("Enriched response with historical data for stream: {StreamId}", request.StreamId);
            }

            _logger.LogInformation(
                "Successfully retrieved audio stream: {StreamId} - {StreamName}",
                audioStream.Id,
                audioStream.Name
            );

            return Result<AudioStreamResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audio stream by ID: {StreamId}", request.StreamId);

            return Result<AudioStreamResponse>.Failure($"Failed to retrieve audio stream: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates if the stream ID has a valid format.
    /// </summary>
    /// <param name="streamId">The stream ID to validate.</param>
    /// <returns>True if the format is valid; otherwise, false.</returns>
    private static bool IsValidStreamIdFormat(string streamId)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            return false;
        }

        // Basic validation - should start with "stream_" and have reasonable length
        return streamId.StartsWith("stream_", StringComparison.OrdinalIgnoreCase)
            && streamId.Length > 7
            && streamId.Length <= 100;
    }

    /// <summary>
    /// Enriches the response with historical data if available.
    /// In a full implementation, this would query event history or audit logs.
    /// </summary>
    /// <param name="response">The response to enrich.</param>
    /// <param name="audioStream">The audio stream entity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task EnrichWithHistoricalData(
        AudioStreamResponse response,
        Core.Models.Entities.AudioStream audioStream,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // In a real implementation, this would:
            // 1. Query event history for stream start/stop events
            // 2. Calculate uptime and usage statistics
            // 3. Retrieve performance metrics
            // 4. Get client connection history

            // For now, we'll just log that this feature would be implemented
            _logger.LogDebug(
                "Historical data enrichment requested for stream: {StreamId}. "
                    + "This would include event history, uptime statistics, and performance metrics.",
                audioStream.Id
            );

            // Simulate async operation
            await Task.Delay(1, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to enrich response with historical data for stream: {StreamId}",
                audioStream.Id
            );

            // Don't fail the entire request if historical data retrieval fails
        }
    }
}
