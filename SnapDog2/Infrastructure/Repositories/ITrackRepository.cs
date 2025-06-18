using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SnapDog2.Core.Models.Entities;

namespace SnapDog2.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Track entities with domain-specific operations.
/// Extends the base repository with track-specific query methods.
/// </summary>
public interface ITrackRepository : IRepository<Track, string>
{
    /// <summary>
    /// Retrieves tracks by artist name.
    /// </summary>
    /// <param name="artist">The artist name to search for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of tracks by the specified artist.</returns>
    Task<IEnumerable<Track>> GetTracksByArtistAsync(string artist, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves tracks by album name.
    /// </summary>
    /// <param name="album">The album name to search for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of tracks from the specified album.</returns>
    Task<IEnumerable<Track>> GetTracksByAlbumAsync(string album, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves tracks within the specified duration range.
    /// </summary>
    /// <param name="minDuration">The minimum duration.</param>
    /// <param name="maxDuration">The maximum duration.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of tracks within the duration range.</returns>
    Task<IEnumerable<Track>> GetTracksByDurationRangeAsync(
        TimeSpan minDuration,
        TimeSpan maxDuration,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Searches for tracks by title using pattern matching.
    /// </summary>
    /// <param name="titlePattern">The title pattern to search for (case-insensitive).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of tracks matching the title pattern.</returns>
    Task<IEnumerable<Track>> SearchTracksByTitleAsync(
        string titlePattern,
        CancellationToken cancellationToken = default
    );
}
