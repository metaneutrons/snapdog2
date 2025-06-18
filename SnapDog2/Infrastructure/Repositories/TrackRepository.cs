using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Infrastructure.Data;

namespace SnapDog2.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Track entities with domain-specific operations.
/// Provides Entity Framework Core-based data access for tracks.
/// </summary>
public sealed class TrackRepository : RepositoryBase<Track, string>, ITrackRepository
{
    /// <summary>
    /// Initializes a new instance of the TrackRepository class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    public TrackRepository(SnapDogDbContext context)
        : base(context) { }

    /// <summary>
    /// Retrieves tracks by artist name.
    /// </summary>
    /// <param name="artist">The artist name to search for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of tracks by the specified artist.</returns>
    /// <exception cref="ArgumentException">Thrown when artist is null or empty.</exception>
    public async Task<IEnumerable<Track>> GetTracksByArtistAsync(
        string artist,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(artist))
        {
            throw new ArgumentException("Artist name cannot be null or empty.", nameof(artist));
        }

        return await GetQueryableNoTracking()
            .Where(track => track.Artist != null && track.Artist.ToLower().Contains(artist.ToLower()))
            .OrderBy(track => track.Album ?? string.Empty)
            .ThenBy(track => track.TrackNumber ?? 0)
            .ThenBy(track => track.Title)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves tracks by album name.
    /// </summary>
    /// <param name="album">The album name to search for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of tracks from the specified album.</returns>
    /// <exception cref="ArgumentException">Thrown when album is null or empty.</exception>
    public async Task<IEnumerable<Track>> GetTracksByAlbumAsync(
        string album,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(album))
        {
            throw new ArgumentException("Album name cannot be null or empty.", nameof(album));
        }

        return await GetQueryableNoTracking()
            .Where(track => track.Album != null && track.Album.ToLower().Contains(album.ToLower()))
            .OrderBy(track => track.TrackNumber ?? 0)
            .ThenBy(track => track.Title)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves tracks within the specified duration range.
    /// </summary>
    /// <param name="minDuration">The minimum duration.</param>
    /// <param name="maxDuration">The maximum duration.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of tracks within the duration range.</returns>
    /// <exception cref="ArgumentException">Thrown when duration range is invalid.</exception>
    public async Task<IEnumerable<Track>> GetTracksByDurationRangeAsync(
        TimeSpan minDuration,
        TimeSpan maxDuration,
        CancellationToken cancellationToken = default
    )
    {
        if (minDuration < TimeSpan.Zero)
        {
            throw new ArgumentException("Minimum duration cannot be negative.", nameof(minDuration));
        }

        if (maxDuration < minDuration)
        {
            throw new ArgumentException(
                "Maximum duration must be greater than or equal to minimum duration.",
                nameof(maxDuration)
            );
        }

        var minSeconds = (int)minDuration.TotalSeconds;
        var maxSeconds = (int)maxDuration.TotalSeconds;

        return await GetQueryableNoTracking()
            .Where(track =>
                track.DurationSeconds.HasValue
                && track.DurationSeconds >= minSeconds
                && track.DurationSeconds <= maxSeconds
            )
            .OrderBy(track => track.DurationSeconds)
            .ThenBy(track => track.Artist ?? string.Empty)
            .ThenBy(track => track.Title)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for tracks by title using pattern matching.
    /// </summary>
    /// <param name="titlePattern">The title pattern to search for (case-insensitive).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of tracks matching the title pattern.</returns>
    /// <exception cref="ArgumentException">Thrown when title pattern is null or empty.</exception>
    public async Task<IEnumerable<Track>> SearchTracksByTitleAsync(
        string titlePattern,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(titlePattern))
        {
            throw new ArgumentException("Title pattern cannot be null or empty.", nameof(titlePattern));
        }

        return await GetQueryableNoTracking()
            .Where(track => track.Title.ToLower().Contains(titlePattern.ToLower()))
            .OrderBy(track => track.Title.ToLower().IndexOf(titlePattern.ToLower())) // Prioritize exact matches at the beginning
            .ThenBy(track => track.Artist ?? string.Empty)
            .ThenBy(track => track.Title)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
