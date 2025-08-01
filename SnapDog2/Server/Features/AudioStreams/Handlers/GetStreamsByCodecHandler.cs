using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Common;
using SnapDog2.Infrastructure.Repositories;
using SnapDog2.Server.Features.AudioStreams.Queries;
using SnapDog2.Server.Models;

namespace SnapDog2.Server.Features.AudioStreams.Handlers;

/// <summary>
/// Handler for retrieving audio streams filtered by a specific codec type.
/// Supports advanced filtering by bitrate, sample rate, and other quality parameters.
/// </summary>
public sealed class GetStreamsByCodecHandler
    : IRequestHandler<GetStreamsByCodecQuery, Result<IEnumerable<AudioStreamResponse>>>
{
    private readonly IAudioStreamRepository _audioStreamRepository;
    private readonly ILogger<GetStreamsByCodecHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetStreamsByCodecHandler"/> class.
    /// </summary>
    /// <param name="audioStreamRepository">The audio stream repository.</param>
    /// <param name="logger">The logger.</param>
    public GetStreamsByCodecHandler(
        IAudioStreamRepository audioStreamRepository,
        ILogger<GetStreamsByCodecHandler> logger
    )
    {
        _audioStreamRepository =
            audioStreamRepository ?? throw new ArgumentNullException(nameof(audioStreamRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the request to get streams by codec.
    /// </summary>
    /// <param name="request">The get streams by codec query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the collection of filtered audio stream responses.</returns>
    public async Task<Result<IEnumerable<AudioStreamResponse>>> Handle(
        GetStreamsByCodecQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _logger.LogInformation(
                "Retrieving audio streams by codec: {Codec} with filters - MinBitrate: {MinBitrate}, MaxBitrate: {MaxBitrate}, SampleRate: {SampleRate}, Channels: {Channels}, IncludeInactive: {IncludeInactive}",
                request.Codec,
                request.MinBitrateKbps,
                request.MaxBitrateKbps,
                request.SampleRateHz,
                request.Channels,
                request.IncludeInactive
            );

            // Use repository method for codec filtering
            var codecStreams = await _audioStreamRepository.GetStreamsByCodecAsync(request.Codec, cancellationToken);

            // Apply additional filtering
            var filteredStreams = codecStreams.AsEnumerable();

            // Filter by active status if requested
            if (!request.IncludeInactive)
            {
                filteredStreams = filteredStreams.Where(s =>
                    s.IsPlaying || s.Status == Core.Models.Enums.StreamStatus.Starting
                );
                _logger.LogDebug("Filtered out inactive streams, remaining count: {Count}", filteredStreams.Count());
            }

            // Apply bitrate filters
            if (request.MinBitrateKbps.HasValue)
            {
                filteredStreams = filteredStreams.Where(s => s.BitrateKbps >= request.MinBitrateKbps.Value);
                _logger.LogDebug("Applied minimum bitrate filter: {MinBitrate} kbps", request.MinBitrateKbps.Value);
            }

            if (request.MaxBitrateKbps.HasValue)
            {
                filteredStreams = filteredStreams.Where(s => s.BitrateKbps <= request.MaxBitrateKbps.Value);
                _logger.LogDebug("Applied maximum bitrate filter: {MaxBitrate} kbps", request.MaxBitrateKbps.Value);
            }

            // Apply sample rate filter
            if (request.SampleRateHz.HasValue)
            {
                filteredStreams = filteredStreams.Where(s => s.SampleRateHz == request.SampleRateHz.Value);
                _logger.LogDebug("Applied sample rate filter: {SampleRate} Hz", request.SampleRateHz.Value);
            }

            // Apply channel count filter
            if (request.Channels.HasValue)
            {
                filteredStreams = filteredStreams.Where(s => s.Channels == request.Channels.Value);
                _logger.LogDebug("Applied channel count filter: {Channels}", request.Channels.Value);
            }

            // Validate filter results
            var validationResult = ValidateFilterCombination(request);
            if (validationResult.IsFailure)
            {
                _logger.LogWarning("Invalid filter combination: {Error}", validationResult.Error);
                return Result<IEnumerable<AudioStreamResponse>>.Failure(validationResult.Error!);
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

            _logger.LogInformation(
                "Successfully retrieved {Count} audio streams with codec: {Codec}",
                result.Count,
                request.Codec
            );

            return Result<IEnumerable<AudioStreamResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audio streams by codec: {Codec}", request.Codec);

            return Result<IEnumerable<AudioStreamResponse>>.Failure(
                $"Failed to retrieve audio streams by codec: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Validates the combination of filters to ensure they are compatible.
    /// </summary>
    /// <param name="request">The query request with filters.</param>
    /// <returns>A result indicating whether the filter combination is valid.</returns>
    private static Result ValidateFilterCombination(GetStreamsByCodecQuery request)
    {
        var errors = new List<string>();

        // Validate bitrate range
        if (request.MinBitrateKbps.HasValue && request.MaxBitrateKbps.HasValue)
        {
            if (request.MinBitrateKbps.Value > request.MaxBitrateKbps.Value)
            {
                errors.Add("Minimum bitrate cannot be greater than maximum bitrate.");
            }
        }

        // Validate bitrate values
        if (request.MinBitrateKbps.HasValue && (request.MinBitrateKbps.Value < 8 || request.MinBitrateKbps.Value > 320))
        {
            errors.Add("Minimum bitrate must be between 8 and 320 kbps.");
        }

        if (request.MaxBitrateKbps.HasValue && (request.MaxBitrateKbps.Value < 8 || request.MaxBitrateKbps.Value > 320))
        {
            errors.Add("Maximum bitrate must be between 8 and 320 kbps.");
        }

        // Validate sample rate
        if (request.SampleRateHz.HasValue)
        {
            var validSampleRates = new[]
            {
                8000,
                11025,
                16000,
                22050,
                32000,
                44100,
                48000,
                88200,
                96000,
                176400,
                192000,
            };
            if (!validSampleRates.Contains(request.SampleRateHz.Value))
            {
                errors.Add(
                    $"Invalid sample rate: {request.SampleRateHz.Value} Hz. Supported rates: {string.Join(", ", validSampleRates)} Hz."
                );
            }
        }

        // Validate channel count
        if (request.Channels.HasValue && (request.Channels.Value < 1 || request.Channels.Value > 8))
        {
            errors.Add("Channel count must be between 1 and 8.");
        }

        return errors.Count == 0 ? Result.Success() : Result.Failure(errors);
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
            // Default sorting by bitrate (highest quality first)
            return streams.OrderByDescending(static s => s.BitrateKbps).ThenByDescending(static s => s.SampleRateHz);
        }

        var sortedStreams = orderBy.ToLowerInvariant() switch
        {
            "name" => descending ? streams.OrderByDescending(static s => s.Name) : streams.OrderBy(static s => s.Name),
            "bitrate" => descending
                ? streams.OrderByDescending(static s => s.BitrateKbps)
                : streams.OrderBy(static s => s.BitrateKbps),
            "samplerate" => descending
                ? streams.OrderByDescending(static s => s.SampleRateHz)
                : streams.OrderBy(static s => s.SampleRateHz),
            "created" => descending
                ? streams.OrderByDescending(static s => s.CreatedAt)
                : streams.OrderBy(static s => s.CreatedAt),
            "status" => descending
                ? streams.OrderByDescending(static s => s.Status)
                : streams.OrderBy(static s => s.Status),
            "channels" => descending
                ? streams.OrderByDescending(static s => s.Channels)
                : streams.OrderBy(static s => s.Channels),
            _ => streams.OrderByDescending(static s => s.BitrateKbps).ThenByDescending(static s => s.SampleRateHz),
        };

        return sortedStreams;
    }
}
