using MediatR;
using SnapDog2.Core.Common;
using SnapDog2.Server.Models;

namespace SnapDog2.Server.Features.AudioStreams.Queries;

/// <summary>
/// Query to retrieve all currently active audio streams in the system.
/// Active streams are those with Playing or Starting status.
/// </summary>
public sealed record GetActiveAudioStreamsQuery : IRequest<Result<IEnumerable<AudioStreamResponse>>>
{
    /// <summary>
    /// Gets a value indicating whether to include detailed information in the response.
    /// When false, returns only basic stream information for performance optimization.
    /// </summary>
    public bool IncludeDetails { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to include streams that are starting up.
    /// When false, only returns streams that are fully playing.
    /// </summary>
    public bool IncludeStarting { get; init; } = true;

    /// <summary>
    /// Gets the maximum number of streams to return (for pagination).
    /// When null, returns all matching streams.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// Gets the number of streams to skip (for pagination).
    /// Used in conjunction with Limit for pagination support.
    /// </summary>
    public int? Skip { get; init; }

    /// <summary>
    /// Gets the field to order the results by.
    /// Supported values: "name", "started", "codec", "bitrate".
    /// </summary>
    public string? OrderBy { get; init; }

    /// <summary>
    /// Gets a value indicating whether to sort in descending order.
    /// When false, sorts in ascending order.
    /// </summary>
    public bool Descending { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether to include performance metrics.
    /// When true, includes additional timing and performance data.
    /// </summary>
    public bool IncludeMetrics { get; init; } = false;

    /// <summary>
    /// Creates a query to get all active audio streams with default settings.
    /// </summary>
    /// <returns>A new GetActiveAudioStreamsQuery instance.</returns>
    public static GetActiveAudioStreamsQuery Create() => new();

    /// <summary>
    /// Creates a query to get all active audio streams with summary information only.
    /// </summary>
    /// <returns>A new GetActiveAudioStreamsQuery instance configured for summary results.</returns>
    public static GetActiveAudioStreamsQuery CreateSummary() => new() { IncludeDetails = false };

    /// <summary>
    /// Creates a query to get only fully playing streams (excluding starting streams).
    /// </summary>
    /// <returns>A new GetActiveAudioStreamsQuery instance configured for playing streams only.</returns>
    public static GetActiveAudioStreamsQuery CreatePlayingOnly() => new() { IncludeStarting = false };

    /// <summary>
    /// Creates a paginated query to get active audio streams.
    /// </summary>
    /// <param name="skip">Number of streams to skip.</param>
    /// <param name="limit">Maximum number of streams to return.</param>
    /// <param name="orderBy">Field to order by.</param>
    /// <param name="descending">Whether to sort in descending order.</param>
    /// <returns>A new GetActiveAudioStreamsQuery instance configured for pagination.</returns>
    public static GetActiveAudioStreamsQuery CreatePaginated(
        int skip,
        int limit,
        string? orderBy = null,
        bool descending = false
    ) =>
        new()
        {
            Skip = skip,
            Limit = limit,
            OrderBy = orderBy,
            Descending = descending,
        };

    /// <summary>
    /// Creates a query to get active audio streams with performance metrics.
    /// </summary>
    /// <returns>A new GetActiveAudioStreamsQuery instance configured to include metrics.</returns>
    public static GetActiveAudioStreamsQuery CreateWithMetrics() => new() { IncludeMetrics = true };
}
