using SnapDog2.Core.Models.ValueObjects;

namespace SnapDog2.Core.Events;

/// <summary>
/// Domain event triggered when a Snapcast client connects to the server.
/// Contains information about the newly connected client.
/// </summary>
public sealed record ClientConnectedEvent : DomainEvent
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
    /// Gets the zone ID that the client is assigned to, if any.
    /// </summary>
    public string? AssignedZoneId { get; init; }

    /// <summary>
    /// Gets the initial volume level of the client.
    /// </summary>
    public int Volume { get; init; }

    /// <summary>
    /// Gets a value indicating whether the client connected for the first time.
    /// </summary>
    public bool IsFirstConnection { get; init; }

    /// <summary>
    /// Gets the client version or user agent information.
    /// </summary>
    public string? ClientVersion { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientConnectedEvent"/> record.
    /// </summary>
    public ClientConnectedEvent() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientConnectedEvent"/> record with a correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID to associate with this event.</param>
    public ClientConnectedEvent(string? correlationId)
        : base(correlationId) { }

    /// <summary>
    /// Creates a new client connected event.
    /// </summary>
    /// <param name="clientId">The unique identifier of the client.</param>
    /// <param name="clientName">The name of the client.</param>
    /// <param name="macAddress">The MAC address of the client.</param>
    /// <param name="ipAddress">The IP address of the client.</param>
    /// <param name="volume">The initial volume level of the client.</param>
    /// <param name="assignedZoneId">The zone ID that the client is assigned to, if any.</param>
    /// <param name="isFirstConnection">Whether the client connected for the first time.</param>
    /// <param name="clientVersion">The client version or user agent information.</param>
    /// <param name="correlationId">The correlation ID to associate with this event.</param>
    /// <returns>A new <see cref="ClientConnectedEvent"/> instance.</returns>
    public static ClientConnectedEvent Create(
        string clientId,
        string clientName,
        MacAddress macAddress,
        IpAddress ipAddress,
        int volume = 50,
        string? assignedZoneId = null,
        bool isFirstConnection = false,
        string? clientVersion = null,
        string? correlationId = null
    )
    {
        return new ClientConnectedEvent(correlationId)
        {
            ClientId = clientId,
            ClientName = clientName,
            MacAddress = macAddress,
            IpAddress = ipAddress,
            Volume = volume,
            AssignedZoneId = assignedZoneId,
            IsFirstConnection = isFirstConnection,
            ClientVersion = clientVersion,
        };
    }
}
