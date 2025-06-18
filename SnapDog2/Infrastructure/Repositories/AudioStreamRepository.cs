using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;
using SnapDog2.Infrastructure.Data;

namespace SnapDog2.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for AudioStream entities with domain-specific operations.
/// Provides Entity Framework Core-based data access for audio streams.
/// </summary>
public sealed class AudioStreamRepository : RepositoryBase<AudioStream, string>, IAudioStreamRepository
{
    /// <summary>
    /// Initializes a new instance of the AudioStreamRepository class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    public AudioStreamRepository(SnapDogDbContext context)
        : base(context) { }

    /// <summary>
    /// Retrieves all active audio streams (playing or starting status).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of active audio streams.</returns>
    public async Task<IEnumerable<AudioStream>> GetActiveStreamsAsync(CancellationToken cancellationToken = default)
    {
        return await GetQueryableNoTracking()
            .Where(stream => stream.Status == StreamStatus.Playing || stream.Status == StreamStatus.Starting)
            .OrderBy(stream => stream.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all audio streams using the specified codec.
    /// </summary>
    /// <param name="codec">The audio codec to filter by.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of audio streams using the specified codec.</returns>
    public async Task<IEnumerable<AudioStream>> GetStreamsByCodecAsync(
        AudioCodec codec,
        CancellationToken cancellationToken = default
    )
    {
        return await GetQueryableNoTracking()
            .Where(stream => stream.Codec == codec)
            .OrderBy(stream => stream.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all audio streams with the specified sample rate.
    /// </summary>
    /// <param name="sampleRate">The sample rate in Hz to filter by.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of audio streams with the specified sample rate.</returns>
    /// <exception cref="ArgumentException">Thrown when sample rate is invalid.</exception>
    public async Task<IEnumerable<AudioStream>> GetStreamsBySampleRateAsync(
        int sampleRate,
        CancellationToken cancellationToken = default
    )
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentException("Sample rate must be greater than zero.", nameof(sampleRate));
        }

        return await GetQueryableNoTracking()
            .Where(stream => stream.SampleRateHz == sampleRate)
            .OrderBy(stream => stream.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves an audio stream by its URL.
    /// </summary>
    /// <param name="url">The stream URL to search for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The audio stream if found; otherwise, null.</returns>
    public async Task<AudioStream?> GetStreamByUrlAsync(StreamUrl url, CancellationToken cancellationToken = default)
    {
        return await GetQueryableNoTracking()
            .FirstOrDefaultAsync(stream => stream.Url.Equals(url), cancellationToken)
            .ConfigureAwait(false);
    }
}
