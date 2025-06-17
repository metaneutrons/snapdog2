namespace SnapDog2.Core.Events;

/// <summary>
/// Base implementation for domain events with common properties.
/// Provides a foundation for all domain events in the SnapDog2 system.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier for this event instance.
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the correlation ID to track related events and operations.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> record.
    /// </summary>
    protected DomainEvent() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> record with a correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID to associate with this event.</param>
    protected DomainEvent(string? correlationId)
    {
        CorrelationId = correlationId;
    }
}
