using MediatR;
using SnapDog2.Core.Common;
using SnapDog2.Server.Models;

namespace SnapDog2.Server.Features.AudioStreams.Queries;

/// <summary>
/// Query to retrieve all audio streams in the system.
/// Returns a comprehensive list of all configured audio streams with their current status.
/// </summary>
public sealed record GetAllAudioStreamsQuery : IRequest<Result<IEnumerable<AudioStreamResponse>>>
{
    /// <summary>
    /// Gets a value indicating whether to include detailed information in the response.
    /// When false, returns only basic stream information for performance optimization.
    /// </summary>
    public bool IncludeDetails { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to include inactive/stopped streams in the results.
    /// When false, only returns streams that are currently active or starting.
    /// </summary>
    public bool IncludeInactive { get; init; } = true;

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
    /// Supported values: "name", "created", "status", "codec".
    /// </summary>
    public string? OrderBy { get; init; }

    /// <summary>
    /// Gets a value indicating whether to sort in descending order.
    /// When false, sorts in ascending order.
    /// </summary>
    public bool Descending { get; init; } = false;

    /// <summary>
    /// Creates a query to get all audio streams with default settings.
    /// </summary>
    /// <returns>A new GetAllAudioStreamsQuery instance.</returns>
    public static GetAllAudioStreamsQuery Create() => new();

    /// <summary>
    /// Creates a query to get all audio streams with summary information only.
    /// </summary>
    /// <returns>A new GetAllAudioStreamsQuery instance configured for summary results.</returns>
    public static GetAllAudioStreamsQuery CreateSummary() => new() { IncludeDetails = false };

    /// <summary>
    /// Creates a query to get only active audio streams.
    /// </summary>
    /// <returns>A new GetAllAudioStreamsQuery instance configured for active streams only.</returns>
    public static GetAllAudioStreamsQuery CreateActiveOnly() => new() { IncludeInactive = false };

    /// <summary>
    /// Creates a paginated query to get audio streams.
    /// </summary>
    /// <param name="skip">Number of streams to skip.</param>
    /// <param name="limit">Maximum number of streams to return.</param>
    /// <param name="orderBy">Field to order by.</param>
    /// <param name="descending">Whether to sort in descending order.</param>
    /// <returns>A new GetAllAudioStreamsQuery instance configured for pagination.</returns>
    public static GetAllAudioStreamsQuery CreatePaginated(
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
}
