using SnapDog2.Core.Configuration;

namespace SnapDog2.Infrastructure.Services;

/// <summary>
/// Interface for KNX/EIB bus communication and control operations.
/// Provides methods for connecting to KNX gateways, reading and writing group values,
/// and handling KNX bus events within the building automation system.
/// </summary>
public interface IKnxService
{
    /// <summary>
    /// Establishes connection to the KNX gateway.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection was successful, false otherwise</returns>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the KNX gateway.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the disconnect operation</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a value to a specific KNX group address.
    /// </summary>
    /// <param name="address">The KNX group address to write to</param>
    /// <param name="value">The value to write as byte array</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if write operation was successful, false otherwise</returns>
    Task<bool> WriteGroupValueAsync(KnxAddress address, byte[] value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a value from a specific KNX group address.
    /// </summary>
    /// <param name="address">The KNX group address to read from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The value read as byte array, or null if read failed</returns>
    Task<byte[]?> ReadGroupValueAsync(KnxAddress address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to value changes on a specific KNX group address.
    /// </summary>
    /// <param name="address">The KNX group address to monitor</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if subscription was successful, false otherwise</returns>
    Task<bool> SubscribeToGroupAsync(KnxAddress address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from value changes on a specific KNX group address.
    /// </summary>
    /// <param name="address">The KNX group address to stop monitoring</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if unsubscription was successful, false otherwise</returns>
    Task<bool> UnsubscribeFromGroupAsync(KnxAddress address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a group value is received from a subscribed address.
    /// </summary>
    event EventHandler<KnxGroupValueEventArgs> GroupValueReceived;
}

/// <summary>
/// Event arguments for KNX group value received events.
/// Contains the address, value, and timestamp of the received group value.
/// </summary>
public class KnxGroupValueEventArgs : EventArgs
{
    /// <summary>
    /// Gets the KNX group address where the value was received.
    /// </summary>
    public KnxAddress Address { get; init; } = default!;

    /// <summary>
    /// Gets the received value as byte array.
    /// </summary>
    public byte[] Value { get; init; } = Array.Empty<byte>();

    /// <summary>
    /// Gets the timestamp when the value was received.
    /// </summary>
    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
}
