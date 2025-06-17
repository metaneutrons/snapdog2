using SnapDog2.Core.Models.ValueObjects;

namespace SnapDog2.Core.Events;

/// <summary>
/// Domain event triggered when a Snapcast client disconnects from the server.
/// Contains information about the disconnected client and the reason for disconnection.
/// </summary>
public sealed record ClientDisconnectedEvent : DomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the client.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Gets the name of the client.
    /// </summary>
    public required string ClientName { get; init; }

    /// <summary>
    /// Gets the MAC address of the client.
    /// </summary>
    public required MacAddress MacAddress { get; init; }

    /// <summary>
    /// Gets the IP address of the client.
    /// </summary>
    public required IpAddress IpAddress { get; init; }

    /// <summary>
    /// Gets the zone ID that the client was assigned to, if any.
    /// </summary>
    public string? AssignedZoneId { get; init; }

    /// <summary>
    /// Gets the reason for the disconnection.
    /// </summary>
    public string? DisconnectionReason { get; init; }

    /// <summary>
    /// Gets the duration the client was connected in seconds.
    /// </summary>
    public long? ConnectionDurationSeconds { get; init; }

    /// <summary>
    /// Gets a value indicating whether the disconnection was expected/graceful.
    /// </summary>
    public bool IsGracefulDisconnection { get; init; }

    /// <summary>
    /// Gets the last known volume level of the client.
    /// </summary>
    public int LastVolume { get; init; }

    /// <summary>
    /// Gets a value indicating whether the client was muted when it disconnected.
    /// </summary>
    public bool WasMuted { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientDisconnectedEvent"/> record.
    /// </summary>
    public ClientDisconnectedEvent() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientDisconnectedEvent"/> record with a correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID to associate with this event.</param>
    public ClientDisconnectedEvent(string? correlationId)
        : base(correlationId) { }

    /// <summary>
    /// Creates a new client disconnected event.
    /// </summary>
    /// <param name="clientId">The unique identifier of the client.</param>
    /// <param name="clientName">The name of the client.</param>
    /// <param name="macAddress">The MAC address of the client.</param>
    /// <param name="ipAddress">The IP address of the client.</param>
    /// <param name="assignedZoneId">The zone ID that the client was assigned to, if any.</param>
    /// <param name="disconnectionReason">The reason for the disconnection.</param>
    /// <param name="connectionDurationSeconds">The duration the client was connected in seconds.</param>
    /// <param name="isGracefulDisconnection">Whether the disconnection was expected/graceful.</param>
    /// <param name="lastVolume">The last known volume level of the client.</param>
    /// <param name="wasMuted">Whether the client was muted when it disconnected.</param>
    /// <param name="correlationId">The correlation ID to associate with this event.</param>
    /// <returns>A new <see cref="ClientDisconnectedEvent"/> instance.</returns>
    public static ClientDisconnectedEvent Create(
        string clientId,
        string clientName,
        MacAddress macAddress,
        IpAddress ipAddress,
        string? assignedZoneId = null,
        string? disconnectionReason = null,
        long? connectionDurationSeconds = null,
        bool isGracefulDisconnection = false,
        int lastVolume = 0,
        bool wasMuted = false,
        string? correlationId = null
    )
    {
        return new ClientDisconnectedEvent(correlationId)
        {
            ClientId = clientId,
            ClientName = clientName,
            MacAddress = macAddress,
            IpAddress = ipAddress,
            AssignedZoneId = assignedZoneId,
            DisconnectionReason = disconnectionReason,
            ConnectionDurationSeconds = connectionDurationSeconds,
            IsGracefulDisconnection = isGracefulDisconnection,
            LastVolume = lastVolume,
            WasMuted = wasMuted,
        };
    }

    /// <summary>
    /// Gets the connection duration as a TimeSpan.
    /// </summary>
    public TimeSpan? ConnectionDuration =>
        ConnectionDurationSeconds.HasValue ? TimeSpan.FromSeconds(ConnectionDurationSeconds.Value) : null;
}
