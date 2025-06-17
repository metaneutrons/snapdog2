using MediatR;

namespace SnapDog2.Core.Events;

/// <summary>
/// Marker interface for domain events in the SnapDog2 system.
/// Domain events represent significant business occurrences that other parts of the system may need to react to.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Gets the unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// Gets the correlation ID to track related events and operations.
    /// </summary>
    string? CorrelationId { get; }
}
