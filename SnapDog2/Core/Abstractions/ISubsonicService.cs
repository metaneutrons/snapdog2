namespace SnapDog2.Core.Abstractions;

using SnapDog2.Core.Models;

/// <summary>
/// Interface for Subsonic API integration service.
/// Provides access to playlists and streaming functionality from Subsonic-compatible servers.
/// </summary>
public interface ISubsonicService
{
    /// <summary>
    /// Gets all available playlists from the Subsonic server.
    /// Note: Radio stations are handled separately and not included in this list.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing list of playlist information.</returns>
    Task<Result<IReadOnlyList<PlaylistInfo>>> GetPlaylistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific playlist with all its tracks.
    /// </summary>
    /// <param name="playlistIndex">The playlist identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing playlist with tracks.</returns>
    Task<Result<Api.Models.PlaylistWithTracks>> GetPlaylistAsync(
        string playlistIndex,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets the streaming URL for a specific track.
    /// </summary>
    /// <param name="trackId">The track identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the streaming URL.</returns>
    Task<Result<string>> GetStreamUrlAsync(string trackId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to the Subsonic server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating connection success.</returns>
    Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes the Subsonic service and tests connectivity.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating initialization success.</returns>
    Task<Result> InitializeAsync(CancellationToken cancellationToken = default);
}
