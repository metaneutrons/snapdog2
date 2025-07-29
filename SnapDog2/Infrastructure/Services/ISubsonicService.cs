using SnapDog2.Core.Models.Entities;

namespace SnapDog2.Infrastructure.Services;

/// <summary>
/// Interface for Subsonic API communication and music streaming operations.
/// Provides methods for connecting to Subsonic servers, managing playlists,
/// and streaming music content for multi-room audio systems.
/// </summary>
public interface ISubsonicService
{
    /// <summary>
    /// Checks if the Subsonic server is available and responding.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if server is available, false otherwise</returns>
    Task<bool> IsServerAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates with the Subsonic server and validates credentials.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if authentication was successful, false otherwise</returns>
    Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all playlists from the Subsonic server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of playlists</returns>
    Task<IEnumerable<Playlist>> GetPlaylistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific playlist by identifier.
    /// </summary>
    /// <param name="playlistId">The playlist identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The playlist, or null if not found</returns>
    Task<Playlist?> GetPlaylistAsync(string playlistId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves tracks for a specific playlist.
    /// </summary>
    /// <param name="playlistId">The playlist identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of tracks in the playlist</returns>
    Task<IEnumerable<Track>> GetPlaylistTracksAsync(string playlistId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for tracks, albums, or artists on the Subsonic server.
    /// </summary>
    /// <param name="query">The search query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of tracks matching the search</returns>
    Task<IEnumerable<Track>> SearchAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the streaming URL for a specific track.
    /// </summary>
    /// <param name="trackId">The track identifier</param>
    /// <param name="maxBitRate">Maximum bitrate for streaming (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The streaming URL, or null if not available</returns>
    Task<string?> GetStreamUrlAsync(string trackId, int? maxBitRate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a stream for a specific track.
    /// </summary>
    /// <param name="trackId">The track identifier</param>
    /// <param name="maxBitRate">Maximum bitrate for streaming (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The audio stream, or null if not available</returns>
    Task<Stream?> GetTrackStreamAsync(string trackId, int? maxBitRate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new playlist on the Subsonic server.
    /// </summary>
    /// <param name="name">The playlist name</param>
    /// <param name="trackIds">Track identifiers to add to the playlist</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created playlist, or null if creation failed</returns>
    Task<Playlist?> CreatePlaylistAsync(string name, IEnumerable<string>? trackIds = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing playlist on the Subsonic server.
    /// </summary>
    /// <param name="playlistId">The playlist identifier</param>
    /// <param name="name">The new playlist name (optional)</param>
    /// <param name="trackIds">Track identifiers to set for the playlist (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if update was successful, false otherwise</returns>
    Task<bool> UpdatePlaylistAsync(string playlistId, string? name = null, IEnumerable<string>? trackIds = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a playlist from the Subsonic server.
    /// </summary>
    /// <param name="playlistId">The playlist identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    Task<bool> DeletePlaylistAsync(string playlistId, CancellationToken cancellationToken = default);
}