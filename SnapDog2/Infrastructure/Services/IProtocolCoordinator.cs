using SnapDog2.Core.Common;

namespace SnapDog2.Infrastructure.Services;

/// <summary>
/// Interface for coordinating communication between different protocols (Snapcast, KNX, MQTT, Subsonic).
/// Provides methods for synchronizing state changes across all integrated systems.
/// </summary>
public interface IProtocolCoordinator
{
    /// <summary>
    /// Synchronizes a volume change across all connected protocols.
    /// </summary>
    /// <param name="clientId">The client identifier</param>
    /// <param name="volume">The new volume level (0-100)</param>
    /// <param name="sourceProtocol">The protocol that initiated the change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure of the synchronization</returns>
    Task<Result> SynchronizeVolumeChangeAsync(string clientId, int volume, string sourceProtocol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes a mute state change across all connected protocols.
    /// </summary>
    /// <param name="clientId">The client identifier</param>
    /// <param name="muted">The new mute state</param>
    /// <param name="sourceProtocol">The protocol that initiated the change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure of the synchronization</returns>
    Task<Result> SynchronizeMuteChangeAsync(string clientId, bool muted, string sourceProtocol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes a zone volume change across all connected protocols.
    /// </summary>
    /// <param name="zoneId">The zone identifier</param>
    /// <param name="volume">The new volume level (0-100)</param>
    /// <param name="sourceProtocol">The protocol that initiated the change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure of the synchronization</returns>
    Task<Result> SynchronizeZoneVolumeChangeAsync(int zoneId, int volume, string sourceProtocol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes a playback command across all connected protocols.
    /// </summary>
    /// <param name="command">The playback command (play, pause, stop)</param>
    /// <param name="streamId">The stream identifier (optional)</param>
    /// <param name="sourceProtocol">The protocol that initiated the command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure of the synchronization</returns>
    Task<Result> SynchronizePlaybackCommandAsync(string command, int? streamId, string sourceProtocol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes a stream assignment change across all connected protocols.
    /// </summary>
    /// <param name="groupId">The group identifier</param>
    /// <param name="streamId">The new stream identifier</param>
    /// <param name="sourceProtocol">The protocol that initiated the change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure of the synchronization</returns>
    Task<Result> SynchronizeStreamAssignmentAsync(string groupId, string streamId, string sourceProtocol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes client connection status across all connected protocols.
    /// </summary>
    /// <param name="clientId">The client identifier</param>
    /// <param name="connected">The connection status</param>
    /// <param name="sourceProtocol">The protocol that detected the change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure of the synchronization</returns>
    Task<Result> SynchronizeClientStatusAsync(string clientId, bool connected, string sourceProtocol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the protocol coordination service and begins monitoring for events.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure of the startup</returns>
    Task<Result> StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the protocol coordination service and cleanup resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure of the shutdown</returns>
    Task<Result> StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current health status of all connected protocols.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary containing health status for each protocol</returns>
    Task<Dictionary<string, bool>> GetProtocolHealthAsync(CancellationToken cancellationToken = default);
}