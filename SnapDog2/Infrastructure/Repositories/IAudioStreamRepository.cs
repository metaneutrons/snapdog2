using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;

namespace SnapDog2.Infrastructure.Repositories;

/// <summary>
/// Repository interface for AudioStream entities with domain-specific operations.
/// Extends the base repository with audio stream-specific query methods.
/// </summary>
public interface IAudioStreamRepository : IRepository<AudioStream, string>
{
    /// <summary>
    /// Retrieves all active audio streams (playing or starting status).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of active audio streams.</returns>
    Task<IEnumerable<AudioStream>> GetActiveStreamsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all audio streams using the specified codec.
    /// </summary>
    /// <param name="codec">The audio codec to filter by.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of audio streams using the specified codec.</returns>
    Task<IEnumerable<AudioStream>> GetStreamsByCodecAsync(
        AudioCodec codec,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retrieves all audio streams with the specified sample rate.
    /// </summary>
    /// <param name="sampleRate">The sample rate in Hz to filter by.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of audio streams with the specified sample rate.</returns>
    Task<IEnumerable<AudioStream>> GetStreamsBySampleRateAsync(
        int sampleRate,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retrieves an audio stream by its URL.
    /// </summary>
    /// <param name="url">The stream URL to search for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The audio stream if found; otherwise, null.</returns>
    Task<AudioStream?> GetStreamByUrlAsync(StreamUrl url, CancellationToken cancellationToken = default);
}
