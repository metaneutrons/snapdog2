using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;

namespace SnapDog2.Core.Models.Entities;

/// <summary>
/// Represents a Snapcast client with ID, name, MAC address, IP, status, and volume information.
/// Immutable domain entity for the SnapDog2 multi-audio zone management system.
/// </summary>
public sealed record Client
{
    /// <summary>
    /// Gets the unique identifier for the client.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the display name of the client.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the MAC address of the client.
    /// </summary>
    public required MacAddress MacAddress { get; init; }

    /// <summary>
    /// Gets the IP address of the client.
    /// </summary>
    public required IpAddress IpAddress { get; init; }

    /// <summary>
    /// Gets the current status of the client.
    /// </summary>
    public required ClientStatus Status { get; init; }

    /// <summary>
    /// Gets the current volume level (0-100).
    /// </summary>
    public required int Volume { get; init; }

    /// <summary>
    /// Gets a value indicating whether the client is muted.
    /// </summary>
    public bool IsMuted { get; init; }

    /// <summary>
    /// Gets the zone ID that this client is currently assigned to.
    /// </summary>
    public string? ZoneId { get; init; }

    /// <summary>
    /// Gets the description of the client.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the physical location of the client.
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// Gets the audio latency in milliseconds.
    /// </summary>
    public int? LatencyMs { get; init; }

    /// <summary>
    /// Gets the last time the client was seen/connected.
    /// </summary>
    public DateTime? LastSeen { get; init; }

    /// <summary>
    /// Gets the timestamp when the client was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the timestamp when the client was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Client"/> record.
    /// </summary>
    public Client()
    {
        // Required properties must be set via object initializer
    }

    /// <summary>
    /// Creates a new client with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the client.</param>
    /// <param name="name">The display name of the client.</param>
    /// <param name="macAddress">The MAC address of the client.</param>
    /// <param name="ipAddress">The IP address of the client.</param>
    /// <param name="status">The current status of the client.</param>
    /// <param name="volume">The initial volume level (0-100).</param>
    /// <returns>A new <see cref="Client"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public static Client Create(
        string id,
        string name,
        MacAddress macAddress,
        IpAddress ipAddress,
        ClientStatus status = ClientStatus.Disconnected,
        int volume = 50
    )
    {
        ValidateParameters(id, name, volume);

        return new Client
        {
            Id = id,
            Name = name,
            MacAddress = macAddress,
            IpAddress = ipAddress,
            Status = status,
            Volume = volume,
            IsMuted = false,
            CreatedAt = DateTime.UtcNow,
            LastSeen = status == ClientStatus.Connected ? DateTime.UtcNow : null,
        };
    }

    /// <summary>
    /// Creates a copy of the current client with updated status.
    /// </summary>
    /// <param name="newStatus">The new status to set.</param>
    /// <returns>A new <see cref="Client"/> instance with updated status.</returns>
    public Client WithStatus(ClientStatus newStatus)
    {
        var lastSeen = newStatus == ClientStatus.Connected ? DateTime.UtcNow : LastSeen;

        return this with
        {
            Status = newStatus,
            LastSeen = lastSeen,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current client with updated volume.
    /// </summary>
    /// <param name="newVolume">The new volume level (0-100).</param>
    /// <returns>A new <see cref="Client"/> instance with updated volume.</returns>
    /// <exception cref="ArgumentException">Thrown when volume is out of range.</exception>
    public Client WithVolume(int newVolume)
    {
        if (newVolume < 0 || newVolume > 100)
        {
            throw new ArgumentException("Volume must be between 0 and 100.", nameof(newVolume));
        }

        return this with
        {
            Volume = newVolume,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current client with updated mute status.
    /// </summary>
    /// <param name="muted">True to mute the client; false to unmute.</param>
    /// <returns>A new <see cref="Client"/> instance with updated mute status.</returns>
    public Client WithMute(bool muted)
    {
        return this with { IsMuted = muted, UpdatedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Creates a copy of the current client assigned to a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID to assign the client to.</param>
    /// <returns>A new <see cref="Client"/> instance assigned to the zone.</returns>
    public Client WithZone(string? zoneId)
    {
        return this with { ZoneId = zoneId, UpdatedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Creates a copy of the current client with updated IP address.
    /// </summary>
    /// <param name="newIpAddress">The new IP address.</param>
    /// <returns>A new <see cref="Client"/> instance with updated IP address.</returns>
    public Client WithIpAddress(IpAddress newIpAddress)
    {
        return this with { IpAddress = newIpAddress, UpdatedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Creates a copy of the current client with updated latency.
    /// </summary>
    /// <param name="latencyMs">The audio latency in milliseconds.</param>
    /// <returns>A new <see cref="Client"/> instance with updated latency.</returns>
    /// <exception cref="ArgumentException">Thrown when latency is negative.</exception>
    public Client WithLatency(int? latencyMs)
    {
        if (latencyMs < 0)
        {
            throw new ArgumentException("Latency cannot be negative.", nameof(latencyMs));
        }

        return this with
        {
            LatencyMs = latencyMs,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Gets a value indicating whether the client is currently connected.
    /// </summary>
    public bool IsConnected => Status == ClientStatus.Connected;

    /// <summary>
    /// Gets a value indicating whether the client is disconnected.
    /// </summary>
    public bool IsDisconnected => Status == ClientStatus.Disconnected;

    /// <summary>
    /// Gets a value indicating whether the client has an error.
    /// </summary>
    public bool HasError => Status == ClientStatus.Error;

    /// <summary>
    /// Gets a value indicating whether the client is assigned to a zone.
    /// </summary>
    public bool IsAssignedToZone => !string.IsNullOrWhiteSpace(ZoneId);

    /// <summary>
    /// Gets a value indicating whether the client is effectively silent (muted or volume 0).
    /// </summary>
    public bool IsSilent => IsMuted || Volume == 0;

    /// <summary>
    /// Gets the effective volume considering mute status.
    /// </summary>
    public int EffectiveVolume => IsMuted ? 0 : Volume;

    /// <summary>
    /// Validates the client parameters.
    /// </summary>
    /// <param name="id">The client ID to validate.</param>
    /// <param name="name">The client name to validate.</param>
    /// <param name="volume">The volume to validate.</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    private static void ValidateParameters(string id, string name, int volume)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Client ID cannot be null or empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Client name cannot be null or empty.", nameof(name));
        }

        if (volume < 0 || volume > 100)
        {
            throw new ArgumentException("Volume must be between 0 and 100.", nameof(volume));
        }
    }
}
