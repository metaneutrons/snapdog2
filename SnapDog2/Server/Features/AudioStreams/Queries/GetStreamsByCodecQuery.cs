using MediatR;
using SnapDog2.Core.Common;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Server.Models;

namespace SnapDog2.Server.Features.AudioStreams.Queries;

/// <summary>
/// Query to retrieve audio streams filtered by a specific codec type.
/// Allows filtering and sorting of streams based on their audio encoding format.
/// </summary>
/// <param name="Codec">The audio codec to filter streams by.</param>
public sealed record GetStreamsByCodecQuery(AudioCodec Codec) : IRequest<Result<IEnumerable<AudioStreamResponse>>>
{
    /// <summary>
    /// Gets a value indicating whether to include detailed information in the response.
    /// When false, returns only basic stream information for performance optimization.
    /// </summary>
    public bool IncludeDetails { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to include inactive/stopped streams in the results.
    /// When false, only returns active streams with the specified codec.
    /// </summary>
    public bool IncludeInactive { get; init; } = true;

    /// <summary>
    /// Gets the minimum bitrate filter in kbps.
    /// When specified, only returns streams with bitrate >= this value.
    /// </summary>
    public int? MinBitrateKbps { get; init; }

    /// <summary>
    /// Gets the maximum bitrate filter in kbps.
    /// When specified, only returns streams with bitrate <= this value.
    /// </summary>
    public int? MaxBitrateKbps { get; init; }

    /// <summary>
    /// Gets the sample rate filter in Hz.
    /// When specified, only returns streams with this exact sample rate.
    /// </summary>
    public int? SampleRateHz { get; init; }

    /// <summary>
    /// Gets the channel count filter.
    /// When specified, only returns streams with this exact channel count.
    /// </summary>
    public int? Channels { get; init; }

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
    /// Supported values: "name", "bitrate", "samplerate", "created", "status".
    /// </summary>
    public string? OrderBy { get; init; }

    /// <summary>
    /// Gets a value indicating whether to sort in descending order.
    /// When false, sorts in ascending order.
    /// </summary>
    public bool Descending { get; init; } = false;

    /// <summary>
    /// Creates a query to get streams by codec with default settings.
    /// </summary>
    /// <param name="codec">The audio codec to filter by.</param>
    /// <returns>A new GetStreamsByCodecQuery instance.</returns>
    public static GetStreamsByCodecQuery Create(AudioCodec codec) => new(codec);

    /// <summary>
    /// Creates a query to get active streams by codec only.
    /// </summary>
    /// <param name="codec">The audio codec to filter by.</param>
    /// <returns>A new GetStreamsByCodecQuery instance configured for active streams only.</returns>
    public static GetStreamsByCodecQuery CreateActiveOnly(AudioCodec codec) => new(codec) { IncludeInactive = false };

    /// <summary>
    /// Creates a query to get streams by codec with summary information only.
    /// </summary>
    /// <param name="codec">The audio codec to filter by.</param>
    /// <returns>A new GetStreamsByCodecQuery instance configured for summary results.</returns>
    public static GetStreamsByCodecQuery CreateSummary(AudioCodec codec) => new(codec) { IncludeDetails = false };

    /// <summary>
    /// Creates a query to get streams by codec with bitrate filtering.
    /// </summary>
    /// <param name="codec">The audio codec to filter by.</param>
    /// <param name="minBitrate">Minimum bitrate in kbps.</param>
    /// <param name="maxBitrate">Maximum bitrate in kbps.</param>
    /// <returns>A new GetStreamsByCodecQuery instance with bitrate filtering.</returns>
    public static GetStreamsByCodecQuery CreateWithBitrateFilter(
        AudioCodec codec,
        int? minBitrate = null,
        int? maxBitrate = null
    ) => new(codec) { MinBitrateKbps = minBitrate, MaxBitrateKbps = maxBitrate };

    /// <summary>
    /// Creates a query to get streams by codec and specific audio quality parameters.
    /// </summary>
    /// <param name="codec">The audio codec to filter by.</param>
    /// <param name="sampleRate">Required sample rate in Hz.</param>
    /// <param name="channels">Required channel count.</param>
    /// <returns>A new GetStreamsByCodecQuery instance with quality filtering.</returns>
    public static GetStreamsByCodecQuery CreateWithQualityFilter(
        AudioCodec codec,
        int? sampleRate = null,
        int? channels = null
    ) => new(codec) { SampleRateHz = sampleRate, Channels = channels };

    /// <summary>
    /// Creates a paginated query to get streams by codec.
    /// </summary>
    /// <param name="codec">The audio codec to filter by.</param>
    /// <param name="skip">Number of streams to skip.</param>
    /// <param name="limit">Maximum number of streams to return.</param>
    /// <param name="orderBy">Field to order by.</param>
    /// <param name="descending">Whether to sort in descending order.</param>
    /// <returns>A new GetStreamsByCodecQuery instance configured for pagination.</returns>
    public static GetStreamsByCodecQuery CreatePaginated(
        AudioCodec codec,
        int skip,
        int limit,
        string? orderBy = null,
        bool descending = false
    ) =>
        new(codec)
        {
            Skip = skip,
            Limit = limit,
            OrderBy = orderBy,
            Descending = descending,
        };
}
