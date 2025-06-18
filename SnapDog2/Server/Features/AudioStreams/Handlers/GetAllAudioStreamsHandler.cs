using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Common;
using SnapDog2.Infrastructure.Repositories;
using SnapDog2.Server.Features.AudioStreams.Queries;
using SnapDog2.Server.Models;

namespace SnapDog2.Server.Features.AudioStreams.Handlers;

/// <summary>
/// Handler for retrieving all audio streams in the system.
/// Supports filtering, pagination, and sorting options.
/// </summary>
public sealed class GetAllAudioStreamsHandler
    : IRequestHandler<GetAllAudioStreamsQuery, Result<IEnumerable<AudioStreamResponse>>>
{
    private readonly IAudioStreamRepository _audioStreamRepository;
    private readonly ILogger<GetAllAudioStreamsHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAllAudioStreamsHandler"/> class.
    /// </summary>
    /// <param name="audioStreamRepository">The audio stream repository.</param>
    /// <param name="logger">The logger.</param>
    public GetAllAudioStreamsHandler(
        IAudioStreamRepository audioStreamRepository,
        ILogger<GetAllAudioStreamsHandler> logger
    )
    {
        _audioStreamRepository =
            audioStreamRepository ?? throw new ArgumentNullException(nameof(audioStreamRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the request to get all audio streams.
    /// </summary>
    /// <param name="request">The get all audio streams query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the collection of audio stream responses.</returns>
    public async Task<Result<IEnumerable<AudioStreamResponse>>> Handle(
        GetAllAudioStreamsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _logger.LogInformation(
                "Retrieving all audio streams with filters - IncludeInactive: {IncludeInactive}, IncludeDetails: {IncludeDetails}, Limit: {Limit}",
                request.IncludeInactive,
                request.IncludeDetails,
                request.Limit
            );

            // Retrieve all streams from repository
            var allStreams = await _audioStreamRepository.GetAllAsync(cancellationToken);

            // Apply filtering
            var filteredStreams = allStreams.AsEnumerable();

            if (!request.IncludeInactive)
            {
                filteredStreams = filteredStreams.Where(s =>
                    s.IsPlaying || s.Status == Core.Models.Enums.StreamStatus.Starting
                );
                _logger.LogDebug("Filtered out inactive streams, remaining count: {Count}", filteredStreams.Count());
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
            var streamResponses = request.IncludeDetails
                ? filteredStreams.Select(AudioStreamResponse.FromEntity)
                : filteredStreams.Select(AudioStreamResponse.CreateSummary);

            var result = streamResponses.ToList();

            _logger.LogInformation("Successfully retrieved {Count} audio streams", result.Count);

            return Result<IEnumerable<AudioStreamResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve audio streams with filters - IncludeInactive: {IncludeInactive}, IncludeDetails: {IncludeDetails}",
                request.IncludeInactive,
                request.IncludeDetails
            );

            return Result<IEnumerable<AudioStreamResponse>>.Failure($"Failed to retrieve audio streams: {ex.Message}");
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
            return streams;
        }

        var sortedStreams = orderBy.ToLowerInvariant() switch
        {
            "name" => descending ? streams.OrderByDescending(s => s.Name) : streams.OrderBy(s => s.Name),
            "created" => descending ? streams.OrderByDescending(s => s.CreatedAt) : streams.OrderBy(s => s.CreatedAt),
            "status" => descending ? streams.OrderByDescending(s => s.Status) : streams.OrderBy(s => s.Status),
            "codec" => descending ? streams.OrderByDescending(s => s.Codec) : streams.OrderBy(s => s.Codec),
            "bitrate" => descending
                ? streams.OrderByDescending(s => s.BitrateKbps)
                : streams.OrderBy(s => s.BitrateKbps),
            "samplerate" => descending
                ? streams.OrderByDescending(s => s.SampleRateHz)
                : streams.OrderBy(s => s.SampleRateHz),
            _ => streams,
        };

        return sortedStreams;
    }
}
