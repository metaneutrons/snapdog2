namespace SnapDog2.Core.Abstractions;

using SnapDog2.Core.Models;

/// <summary>
/// Abstraction for Snapcast server communication.
/// Provides methods for managing Snapcast clients, groups, and streams.
/// </summary>
public interface ISnapcastService
{
    /// <summary>
    /// Initializes the connection to the Snapcast server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current server status including all clients, groups, and streams.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing server status or error.</returns>
    Task<Result<SnapcastServerStatus>> GetServerStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the volume for a specific client.
    /// </summary>
    /// <param name="snapcastClientId">Snapcast client ID.</param>
    /// <param name="volumePercent">Volume percentage (0-100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SetClientVolumeAsync(
        string snapcastClientId,
        int volumePercent,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Sets the mute state for a specific client.
    /// </summary>
    /// <param name="snapcastClientId">Snapcast client ID.</param>
    /// <param name="muted">Whether the client should be muted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SetClientMuteAsync(string snapcastClientId, bool muted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the latency for a specific client.
    /// </summary>
    /// <param name="snapcastClientId">Snapcast client ID.</param>
    /// <param name="latencyMs">Latency in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SetClientLatencyAsync(
        string snapcastClientId,
        int latencyMs,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Sets the name for a specific client.
    /// </summary>
    /// <param name="snapcastClientId">Snapcast client ID.</param>
    /// <param name="name">New client name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SetClientNameAsync(
        string snapcastClientId,
        string name,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Assigns a client to a specific group.
    /// </summary>
    /// <param name="snapcastClientId">Snapcast client ID.</param>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SetClientGroupAsync(
        string snapcastClientId,
        string groupId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Sets the mute state for a specific group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="muted">Whether the group should be muted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SetGroupMuteAsync(string groupId, bool muted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the stream for a specific group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="streamId">Stream ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SetGroupStreamAsync(string groupId, string streamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the name for a specific group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="name">New group name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SetGroupNameAsync(string groupId, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new group with the specified clients.
    /// </summary>
    /// <param name="clientIds">List of client IDs to include in the group.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the new group ID or error.</returns>
    Task<Result<string>> CreateGroupAsync(IEnumerable<string> clientIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a group and reassigns its clients to other groups.
    /// </summary>
    /// <param name="groupId">Group ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DeleteGroupAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a client from the server.
    /// </summary>
    /// <param name="snapcastClientId">Snapcast client ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DeleteClientAsync(string snapcastClientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the RPC version of the Snapcast server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing version information or error.</returns>
    Task<Result<VersionDetails>> GetRpcVersionAsync(CancellationToken cancellationToken = default);
}
