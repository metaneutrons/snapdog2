namespace SnapDog2.Core.Models;

/// <summary>
/// Represents the complete state of a client.
/// </summary>
public record ClientState
{
    /// <summary>
    /// Gets the Snapcast client ID.
    /// </summary>
    public required string SnapcastId { get; init; }

    /// <summary>
    /// Gets the SnapDog2 configured client name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the client MAC address.
    /// </summary>
    public required string Mac { get; init; }

    /// <summary>
    /// Gets whether the client is connected.
    /// </summary>
    public required bool Connected { get; init; }

    /// <summary>
    /// Gets the client volume (0-100).
    /// </summary>
    public required int Volume { get; init; }

    /// <summary>
    /// Gets whether the client is muted.
    /// </summary>
    public required bool Mute { get; init; }

    /// <summary>
    /// Gets the client latency in milliseconds.
    /// </summary>
    public required int LatencyMs { get; init; }

    /// <summary>
    /// Gets the 1-based SnapDog2 Zone ID the client is currently assigned to (null if unassigned).
    /// </summary>
    public int? ZoneIndex { get; init; }

    /// <summary>
    /// Gets the name from Snapcast client configuration.
    /// </summary>
    public string? ConfiguredSnapcastName { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the client was last seen.
    /// </summary>
    public DateTime? LastSeenUtc { get; init; }

    /// <summary>
    /// Gets the client host IP address.
    /// </summary>
    public string? HostIpAddress { get; init; }

    /// <summary>
    /// Gets the client host name.
    /// </summary>
    public string? HostName { get; init; }

    /// <summary>
    /// Gets the client host operating system.
    /// </summary>
    public string? HostOs { get; init; }

    /// <summary>
    /// Gets the client host architecture.
    /// </summary>
    public string? HostArch { get; init; }

    /// <summary>
    /// Gets the Snapcast client version.
    /// </summary>
    public string? SnapClientVersion { get; init; }

    /// <summary>
    /// Gets the Snapcast client protocol version.
    /// </summary>
    public int? SnapClientProtocolVersion { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the client state was recorded.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
