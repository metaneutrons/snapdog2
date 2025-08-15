namespace SnapDog2.Core.Abstractions;

using System.Collections.Generic;
using System.Threading.Tasks;
using SnapDog2.Core.Models;

/// <summary>
/// Provides management operations for playlists and tracks.
/// </summary>
public interface IPlaylistManager
{
    /// <summary>
    /// Gets all available playlists.
    /// </summary>
    /// <returns>A result containing the list of all playlists.</returns>
    Task<Result<List<PlaylistInfo>>> GetAllPlaylistsAsync();

    /// <summary>
    /// Gets tracks for a specific playlist by ID.
    /// </summary>
    /// <param name="playlistIndex">The playlist ID.</param>
    /// <returns>A result containing the list of tracks in the playlist.</returns>
    Task<Result<List<TrackInfo>>> GetPlaylistTracksByIdAsync(string playlistIndex);

    /// <summary>
    /// Gets tracks for a specific playlist by index.
    /// </summary>
    /// <param name="playlistIndex">The playlist index (1-based).</param>
    /// <returns>A result containing the list of tracks in the playlist.</returns>
    Task<Result<List<TrackInfo>>> GetPlaylistTracksByIndexAsync(int playlistIndex);

    /// <summary>
    /// Gets a specific playlist by ID.
    /// </summary>
    /// <param name="playlistIndex">The playlist ID.</param>
    /// <returns>A result containing the playlist information.</returns>
    Task<Result<PlaylistInfo>> GetPlaylistByIdAsync(string playlistIndex);

    /// <summary>
    /// Gets a specific playlist by index.
    /// </summary>
    /// <param name="playlistIndex">The playlist index (1-based).</param>
    /// <returns>A result containing the playlist information.</returns>
    Task<Result<PlaylistInfo>> GetPlaylistByIndexAsync(int playlistIndex);
}
