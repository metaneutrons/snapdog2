using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Common;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Infrastructure.Repositories;
using SnapDog2.Server.Features.AudioStreams.Queries;
using SnapDog2.Server.Models;

namespace SnapDog2.Server.Features.AudioStreams.Handlers;

/// <summary>
/// Handler for retrieving all currently active audio streams.
/// Active streams are those with Playing or Starting status.
/// </summary>
public sealed class GetActiveAudioStreamsHandler
    : IRequestHandler<GetActiveAudioStreamsQuery, Result<IEnumerable<AudioStreamResponse>>>
{
    private readonly IAudioStreamRepository _audioStreamRepository;
    private readonly ILogger<GetActiveAudioStreamsHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetActiveAudioStreamsHandler"/> class.
    /// </summary>
    /// <param name="audioStreamRepository">The audio stream repository.</param>
    /// <param name="logger">The logger.</param>
    public GetActiveAudioStreamsHandler(
        IAudioStreamRepository audioStreamRepository,
        ILogger<GetActiveAudioStreamsHandler> logger
    )
    {
        _audioStreamRepository =
            audioStreamRepository ?? throw new ArgumentNullException(nameof(audioStreamRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the request to get active audio streams.
    /// </summary>
    /// <param name="request">The get active audio streams query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the collection of active audio stream responses.</returns>
    public async Task<Result<IEnumerable<AudioStreamResponse>>> Handle(
        GetActiveAudioStreamsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _logger.LogInformation(
                "Retrieving active audio streams with options - IncludeStarting: {IncludeStarting}, IncludeDetails: {IncludeDetails}, IncludeMetrics: {IncludeMetrics}, Limit: {Limit}",
                request.IncludeStarting,
                request.IncludeDetails,
                request.IncludeMetrics,
                request.Limit
            );

            // Use repository method for active streams if available, otherwise filter manually
            var activeStreams = await _audioStreamRepository.GetActiveStreamsAsync(cancellationToken);

            // Apply additional filtering based on request options
            var filteredStreams = activeStreams.AsEnumerable();

            if (!request.IncludeStarting)
            {
                // Only include fully playing streams, exclude starting ones
                filteredStreams = filteredStreams.Where(static s => s.Status == StreamStatus.Playing);
                _logger.LogDebug("Filtered out starting streams, remaining count: {Count}", filteredStreams.Count());
            }

            // Apply sorting
            filteredStreams = ApplySorting(filteredStreams, request.OrderBy, request.Descending);

            // Apply pagination
            if (request.Skip.HasValue)
            {
                filteredStreams = filteredStreams.Skip(request.Skip.Value);
                _logger.LogDebug("Applied skip: {Skip}", request.Skip.Value);
            }

            if (request.Limit.HasValue)
            {
                filteredStreams = filteredStreams.Take(request.Limit.Value);
                _logger.LogDebug("Applied limit: {Limit}", request.Limit.Value);
            }

            // Convert to response models
            var streamResponses = new List<AudioStreamResponse>();
            foreach (var stream in filteredStreams)
            {
                var response = request.IncludeDetails
                    ? AudioStreamResponse.FromEntity(stream)
                    : AudioStreamResponse.CreateSummary(stream);

                // Add performance metrics if requested
                if (request.IncludeMetrics)
                {
                    await EnrichWithPerformanceMetrics(response, stream, cancellationToken);
                }

                streamResponses.Add(response);
            }

            _logger.LogInformation("Successfully retrieved {Count} active audio streams", streamResponses.Count);

            return Result<IEnumerable<AudioStreamResponse>>.Success(streamResponses);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve active audio streams with options - IncludeStarting: {IncludeStarting}, IncludeDetails: {IncludeDetails}",
                request.IncludeStarting,
                request.IncludeDetails
            );

            return Result<IEnumerable<AudioStreamResponse>>.Failure(
                $"Failed to retrieve active audio streams: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Applies sorting to the stream collection based on the specified criteria.
    /// </summary>
    /// <param name="streams">The stream collection to sort.</param>
    /// <param name="orderBy">The field to order by.</param>
    /// <param name="descending">Whether to sort in descending order.</param>
    /// <returns>The sorted stream collection.</returns>
    private static IEnumerable<Core.Models.Entities.AudioStream> ApplySorting(
        IEnumerable<Core.Models.Entities.AudioStream> streams,
        string? orderBy,
        bool descending
    )
    {
        if (string.IsNullOrWhiteSpace(orderBy))
        {
            // Default sorting by last started (most recently started first)
            return streams.OrderByDescending(static s => s.UpdatedAt ?? s.CreatedAt);
        }

        var sortedStreams = orderBy.ToLowerInvariant() switch
        {
            "name" => descending ? streams.OrderByDescending(static s => s.Name) : streams.OrderBy(static s => s.Name),
            "started" => descending
                ? streams.OrderByDescending(static s => s.UpdatedAt ?? s.CreatedAt)
                : streams.OrderBy(static s => s.UpdatedAt ?? s.CreatedAt),
            "codec" => descending
                ? streams.OrderByDescending(static s => s.Codec)
                : streams.OrderBy(static s => s.Codec),
            "bitrate" => descending
                ? streams.OrderByDescending(static s => s.BitrateKbps)
                : streams.OrderBy(static s => s.BitrateKbps),
            "samplerate" => descending
                ? streams.OrderByDescending(static s => s.SampleRateHz)
                : streams.OrderBy(static s => s.SampleRateHz),
            "status" => descending
                ? streams.OrderByDescending(static s => s.Status)
                : streams.OrderBy(static s => s.Status),
            _ => streams.OrderByDescending(static s => s.UpdatedAt ?? s.CreatedAt),
        };

        return sortedStreams;
    }

    /// <summary>
    /// Enriches the response with performance metrics if available.
    /// In a full implementation, this would gather real-time performance data.
    /// </summary>
    /// <param name="response">The response to enrich.</param>
    /// <param name="audioStream">The audio stream entity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task EnrichWithPerformanceMetrics(
        AudioStreamResponse response,
        Core.Models.Entities.AudioStream audioStream,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // In a real implementation, this would:
            // 1. Query real-time stream metrics (throughput, latency, etc.)
            // 2. Get client connection counts
            // 3. Retrieve buffer health information
            // 4. Calculate stream uptime and stability metrics

            _logger.LogDebug(
                "Performance metrics enrichment requested for active stream: {StreamId}. "
                    + "This would include throughput, latency, client count, and stability metrics.",
                audioStream.Id
            );

            // Simulate async operation for metrics gathering
            await Task.Delay(1, cancellationToken);

            // In a real implementation, metrics would be added to the response
            // response.Metrics = await _metricsService.GetStreamMetricsAsync(audioStream.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to enrich response with performance metrics for active stream: {StreamId}",
                audioStream.Id
            );

            // Don't fail the entire request if metrics retrieval fails
        }
    }
}
