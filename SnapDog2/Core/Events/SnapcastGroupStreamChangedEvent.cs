namespace SnapDog2.Core.Events;

/// <summary>
/// Event published when a Snapcast group's stream changes.
/// </summary>
/// <param name="GroupId">The group identifier</param>
/// <param name="StreamId">The new stream identifier</param>
public record SnapcastGroupStreamChangedEvent(string GroupId, string StreamId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
}