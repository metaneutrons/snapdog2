namespace SnapDog2.Core.Events;

/// <summary>
/// Event published when a Snapcast client's volume changes.
/// </summary>
/// <param name="ClientId">The client identifier</param>
/// <param name="Volume">The new volume level (0-100)</param>
/// <param name="Muted">Whether the client is muted</param>
public record SnapcastClientVolumeChangedEvent(string ClientId, int Volume, bool Muted) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
}