namespace SnapDog2.Core.Events;

/// <summary>
/// Event published when a Snapcast client disconnects.
/// </summary>
/// <param name="ClientId">The client identifier</param>
public record SnapcastClientDisconnectedEvent(string ClientId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
}