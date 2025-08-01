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
/// Repository implementation for Playlist entities with domain-specific operations.
/// Provides Entity Framework Core-based data access for playlists.
/// </summary>
public sealed class PlaylistRepository : RepositoryBase<Playlist, string>, IPlaylistRepository
{
    /// <summary>
    /// Initializes a new instance of the PlaylistRepository class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    public PlaylistRepository(SnapDogDbContext context)
        : base(context) { }

    /// <summary>
    /// Retrieves playlists within the specified duration range.
    /// </summary>
    /// <param name="minDuration">The minimum duration.</param>
    /// <param name="maxDuration">The maximum duration.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of playlists within the duration range.</returns>
    /// <exception cref="ArgumentException">Thrown when duration range is invalid.</exception>
    public async Task<IEnumerable<Playlist>> GetPlaylistsByDurationRangeAsync(
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
            .Where(playlist =>
                playlist.TotalDurationSeconds.HasValue
                && playlist.TotalDurationSeconds >= minSeconds
                && playlist.TotalDurationSeconds <= maxSeconds
            )
            .OrderBy(playlist => playlist.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves a playlist by its name.
    /// </summary>
    /// <param name="name">The playlist name to search for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The playlist if found; otherwise, null.</returns>
    /// <exception cref="ArgumentException">Thrown when name is null or empty.</exception>
    public async Task<Playlist?> GetPlaylistByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Playlist name cannot be null or empty.", nameof(name));
        }

        return await GetQueryableNoTracking()
            .FirstOrDefaultAsync(playlist => playlist.Name.ToLower() == name.ToLower(), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the most recently created or updated playlists.
    /// </summary>
    /// <param name="count">The number of recent playlists to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of recent playlists.</returns>
    /// <exception cref="ArgumentException">Thrown when count is invalid.</exception>
    public async Task<IEnumerable<Playlist>> GetRecentPlaylistsAsync(
        int count,
        CancellationToken cancellationToken = default
    )
    {
        if (count <= 0)
        {
            throw new ArgumentException("Count must be greater than zero.", nameof(count));
        }

        return await GetQueryableNoTracking()
            .OrderByDescending(static playlist => playlist.UpdatedAt ?? playlist.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the count of tracks in a specific playlist.
    /// </summary>
    /// <param name="playlistId">The playlist ID to count tracks for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The number of tracks in the playlist.</returns>
    /// <exception cref="ArgumentException">Thrown when playlist ID is null or empty.</exception>
    public async Task<int> GetTrackCountInPlaylistAsync(
        string playlistId,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(playlistId))
        {
            throw new ArgumentException("Playlist ID cannot be null or empty.", nameof(playlistId));
        }

        var playlist = await GetQueryableNoTracking()
            .Where(p => p.Id == playlistId)
            .Select(p => new { p.TrackIds })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return playlist?.TrackIds.Count ?? 0;
    }
}
