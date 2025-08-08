namespace SnapDog2.Core.Abstractions;

using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Interface for KNX integration service providing bi-directional KNX communication.
/// </summary>
public interface IKnxService : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether the KNX service is connected and operational.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the current connection status of the KNX service.
    /// </summary>
    ServiceStatus Status { get; }

    /// <summary>
    /// Initializes and starts the KNX service connection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result> InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the KNX service and disconnects from the KNX bus.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result> StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a status update to the configured KNX group address.
    /// </summary>
    /// <param name="statusId">The status identifier.</param>
    /// <param name="targetId">The target identifier (zone or client ID).</param>
    /// <param name="value">The value to send.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result> SendStatusAsync(
        string statusId,
        int targetId,
        object value,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Writes a value to a specific KNX group address.
    /// </summary>
    /// <param name="groupAddress">The KNX group address.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result> WriteGroupValueAsync(string groupAddress, object value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a value from a specific KNX group address.
    /// </summary>
    /// <param name="groupAddress">The KNX group address.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation with the read value.</returns>
    Task<Result<object>> ReadGroupValueAsync(string groupAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes client status updates to KNX group addresses.
    /// </summary>
    /// <param name="clientId">Client identifier.</param>
    /// <param name="eventType">Type of event (e.g., CLIENT_VOLUME, CLIENT_MUTE).</param>
    /// <param name="payload">Event payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> PublishClientStatusAsync<T>(
        string clientId,
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Publishes zone status updates to KNX group addresses.
    /// </summary>
    /// <param name="zoneId">Zone identifier.</param>
    /// <param name="eventType">Type of event (e.g., ZONE_VOLUME, ZONE_MUTE).</param>
    /// <param name="payload">Event payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> PublishZoneStatusAsync<T>(
        int zoneId,
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    );
}
