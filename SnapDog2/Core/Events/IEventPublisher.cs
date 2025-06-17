namespace SnapDog2.Core.Events;

/// <summary>
/// Interface for publishing domain events within the SnapDog2 system.
/// Provides methods to publish single events or multiple events in batch.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a single domain event asynchronously.
    /// </summary>
    /// <param name="domainEvent">The domain event to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple domain events asynchronously.
    /// </summary>
    /// <param name="domainEvents">The collection of domain events to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a single domain event and waits for all handlers to complete.
    /// </summary>
    /// <param name="domainEvent">The domain event to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAndWaitAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple domain events and waits for all handlers to complete.
    /// </summary>
    /// <param name="domainEvents">The collection of domain events to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAndWaitAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
