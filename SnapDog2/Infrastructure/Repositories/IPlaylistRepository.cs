using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SnapDog2.Core.Models.Entities;

namespace SnapDog2.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Playlist entities with domain-specific operations.
/// Extends the base repository with playlist-specific query methods.
/// </summary>
public interface IPlaylistRepository : IRepository<Playlist, string>
{
    /// <summary>
    /// Retrieves playlists within the specified duration range.
    /// </summary>
    /// <param name="minDuration">The minimum duration.</param>
    /// <param name="maxDuration">The maximum duration.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of playlists within the duration range.</returns>
    Task<IEnumerable<Playlist>> GetPlaylistsByDurationRangeAsync(
        TimeSpan minDuration,
        TimeSpan maxDuration,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retrieves a playlist by its name.
    /// </summary>
    /// <param name="name">The playlist name to search for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The playlist if found; otherwise, null.</returns>
    Task<Playlist?> GetPlaylistByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recently created or updated playlists.
    /// </summary>
    /// <param name="count">The number of recent playlists to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of recent playlists.</returns>
    Task<IEnumerable<Playlist>> GetRecentPlaylistsAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of tracks in a specific playlist.
    /// </summary>
    /// <param name="playlistId">The playlist ID to count tracks for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The number of tracks in the playlist.</returns>
    Task<int> GetTrackCountInPlaylistAsync(string playlistId, CancellationToken cancellationToken = default);
}
