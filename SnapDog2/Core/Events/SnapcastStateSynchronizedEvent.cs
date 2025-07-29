using SnapDog2.Infrastructure.Services.Models;

namespace SnapDog2.Core.Events;

/// <summary>
/// Event published when Snapcast server state is synchronized.
/// </summary>
/// <param name="ServerState">The current server state</param>
public record SnapcastStateSynchronizedEvent(SnapcastServer ServerState) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
}