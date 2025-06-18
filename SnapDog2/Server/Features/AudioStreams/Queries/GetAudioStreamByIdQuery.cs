using MediatR;
using SnapDog2.Core.Common;
using SnapDog2.Server.Models;

namespace SnapDog2.Server.Features.AudioStreams.Queries;

/// <summary>
/// Query to retrieve a specific audio stream by its unique identifier.
/// Returns detailed information about the requested stream if it exists.
/// </summary>
/// <param name="StreamId">The unique identifier of the audio stream to retrieve.</param>
public sealed record GetAudioStreamByIdQuery(string StreamId) : IRequest<Result<AudioStreamResponse>>
{
    /// <summary>
    /// Gets a value indicating whether to include detailed information in the response.
    /// When false, returns only basic stream information for performance optimization.
    /// </summary>
    public bool IncludeDetails { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to include historical status information.
    /// When true, includes additional metadata about stream history and events.
    /// </summary>
    public bool IncludeHistory { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether to validate that the stream exists before querying.
    /// When true, performs existence check first to provide better error messages.
    /// </summary>
    public bool ValidateExistence { get; init; } = true;

    /// <summary>
    /// Creates a query to get an audio stream by ID with default settings.
    /// </summary>
    /// <param name="streamId">The unique identifier of the audio stream.</param>
    /// <returns>A new GetAudioStreamByIdQuery instance.</returns>
    /// <exception cref="ArgumentException">Thrown when streamId is null or empty.</exception>
    public static GetAudioStreamByIdQuery Create(string streamId)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or empty.", nameof(streamId));
        }

        return new GetAudioStreamByIdQuery(streamId);
    }

    /// <summary>
    /// Creates a query to get an audio stream by ID with summary information only.
    /// </summary>
    /// <param name="streamId">The unique identifier of the audio stream.</param>
    /// <returns>A new GetAudioStreamByIdQuery instance configured for summary results.</returns>
    /// <exception cref="ArgumentException">Thrown when streamId is null or empty.</exception>
    public static GetAudioStreamByIdQuery CreateSummary(string streamId)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or empty.", nameof(streamId));
        }

        return new GetAudioStreamByIdQuery(streamId) { IncludeDetails = false };
    }

    /// <summary>
    /// Creates a query to get an audio stream by ID with historical information.
    /// </summary>
    /// <param name="streamId">The unique identifier of the audio stream.</param>
    /// <returns>A new GetAudioStreamByIdQuery instance configured to include history.</returns>
    /// <exception cref="ArgumentException">Thrown when streamId is null or empty.</exception>
    public static GetAudioStreamByIdQuery CreateWithHistory(string streamId)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or empty.", nameof(streamId));
        }

        return new GetAudioStreamByIdQuery(streamId) { IncludeHistory = true };
    }

    /// <summary>
    /// Creates a query to get an audio stream by ID without existence validation.
    /// Use this for performance-critical scenarios where you're certain the stream exists.
    /// </summary>
    /// <param name="streamId">The unique identifier of the audio stream.</param>
    /// <returns>A new GetAudioStreamByIdQuery instance without existence validation.</returns>
    /// <exception cref="ArgumentException">Thrown when streamId is null or empty.</exception>
    public static GetAudioStreamByIdQuery CreateFast(string streamId)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or empty.", nameof(streamId));
        }

        return new GetAudioStreamByIdQuery(streamId) { ValidateExistence = false };
    }
}
