namespace SnapDog2.Infrastructure.Services;

/// <summary>
/// Interface for Snapcast server communication and control operations.
/// Provides methods for monitoring server status, managing groups and clients,
/// and controlling audio streaming within the Snapcast ecosystem.
/// </summary>
public interface ISnapcastService
{
    /// <summary>
    /// Checks if the Snapcast server is available and responding.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if server is available, false otherwise</returns>
    Task<bool> IsServerAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current status of the Snapcast server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Server status information as JSON string</returns>
    Task<string> GetServerStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all groups configured on the Snapcast server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of group identifiers</returns>
    Task<IEnumerable<string>> GetGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all clients connected to the Snapcast server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of client identifiers</returns>
    Task<IEnumerable<string>> GetClientsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the volume level for a specific Snapcast client.
    /// </summary>
    /// <param name="clientId">Unique identifier of the client</param>
    /// <param name="volume">Volume level (0-100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if operation was successful, false otherwise</returns>
    Task<bool> SetClientVolumeAsync(string clientId, int volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the mute state for a specific Snapcast client.
    /// </summary>
    /// <param name="clientId">Unique identifier of the client</param>
    /// <param name="muted">True to mute, false to unmute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if operation was successful, false otherwise</returns>
    Task<bool> SetClientMuteAsync(string clientId, bool muted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a specific stream to a group.
    /// </summary>
    /// <param name="groupId">Unique identifier of the group</param>
    /// <param name="streamId">Unique identifier of the stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if operation was successful, false otherwise</returns>
    Task<bool> SetGroupStreamAsync(string groupId, string streamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes the current server state and publishes events for changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the synchronization operation</returns>
    Task SynchronizeServerStateAsync(CancellationToken cancellationToken = default);
}
