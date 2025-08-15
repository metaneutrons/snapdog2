namespace SnapDog2.Core.Abstractions;

using SnapDog2.Core.Models;

/// <summary>
/// Interface for media player services that handle audio streaming to zones.
/// </summary>
public interface IMediaPlayerService
{
    /// <summary>
    /// Starts playing audio for the specified zone.
    /// </summary>
    /// <param name="zoneIndex">The zone ID to play audio in</param>
    /// <param name="trackInfo">Information about the track to play</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> PlayAsync(int zoneIndex, TrackInfo trackInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops playback for the specified zone.
    /// </summary>
    /// <param name="zoneIndex">The zone ID to stop playback in</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> StopAsync(int zoneIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses playback for the specified zone.
    /// Note: For streaming audio, this may be equivalent to stop.
    /// </summary>
    /// <param name="zoneIndex">The zone ID to pause playback in</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> PauseAsync(int zoneIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current playback status for a specific zone.
    /// </summary>
    /// <param name="zoneIndex">The zone ID to get status for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the playback status</returns>
    Task<Result<PlaybackStatus>> GetStatusAsync(int zoneIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current playback status for all zones.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing playback status for all zones</returns>
    Task<Result<IEnumerable<PlaybackStatus>>> GetAllStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops all active playback across all zones.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> StopAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets system-wide playback statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing playback statistics</returns>
    Task<Result<PlaybackStatistics>> GetStatisticsAsync(CancellationToken cancellationToken = default);
}
