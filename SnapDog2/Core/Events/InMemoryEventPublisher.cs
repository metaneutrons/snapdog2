using MediatR;
using Microsoft.Extensions.Logging;

namespace SnapDog2.Core.Events;

/// <summary>
/// In-memory implementation of the event publisher using MediatR for Phase 1.
/// Provides immediate event publishing through the MediatR pipeline.
/// </summary>
public sealed class InMemoryEventPublisher : IEventPublisher
{
    private readonly IMediator _mediator;
    private readonly ILogger<InMemoryEventPublisher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryEventPublisher"/> class.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance.</param>
    /// <param name="logger">The logger instance.</param>
    public InMemoryEventPublisher(IMediator mediator, ILogger<InMemoryEventPublisher> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publishes a single domain event asynchronously.
    /// </summary>
    /// <param name="domainEvent">The domain event to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent == null)
        {
            throw new ArgumentNullException(nameof(domainEvent));
        }

        try
        {
            _logger.LogDebug(
                "Publishing domain event {EventType} with ID {EventId} at {OccurredAt}",
                domainEvent.GetType().Name,
                domainEvent.EventId,
                domainEvent.OccurredAt
            );

            await _mediator.Publish(domainEvent, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Successfully published domain event {EventType} with ID {EventId}",
                domainEvent.GetType().Name,
                domainEvent.EventId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish domain event {EventType} with ID {EventId}",
                domainEvent.GetType().Name,
                domainEvent.EventId
            );
            throw;
        }
    }

    /// <summary>
    /// Publishes multiple domain events asynchronously.
    /// </summary>
    /// <param name="domainEvents">The collection of domain events to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task PublishAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default
    )
    {
        if (domainEvents == null)
        {
            throw new ArgumentNullException(nameof(domainEvents));
        }

        var eventList = domainEvents.ToList();
        if (eventList.Count == 0)
        {
            _logger.LogDebug("No domain events to publish");
            return;
        }

        _logger.LogDebug("Publishing {EventCount} domain events", eventList.Count);

        var publishTasks = eventList.Select(async domainEvent =>
        {
            try
            {
                await _mediator.Publish(domainEvent, cancellationToken).ConfigureAwait(false);
                return (Success: true, Event: domainEvent, Exception: (Exception?)null);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to publish domain event {EventType} with ID {EventId}",
                    domainEvent.GetType().Name,
                    domainEvent.EventId
                );
                return (Success: false, Event: domainEvent, Exception: ex);
            }
        });

        var results = await Task.WhenAll(publishTasks).ConfigureAwait(false);

        var failures = results.Where(r => !r.Success).ToList();
        if (failures.Count > 0)
        {
            _logger.LogWarning(
                "Failed to publish {FailureCount} out of {TotalCount} domain events",
                failures.Count,
                eventList.Count
            );

            // For Phase 1, we'll log failures but continue execution
            // In future phases, this could be enhanced with retry logic or dead letter queues
        }

        _logger.LogDebug("Completed publishing {EventCount} domain events", eventList.Count);
    }

    /// <summary>
    /// Publishes a single domain event and waits for all handlers to complete.
    /// </summary>
    /// <param name="domainEvent">The domain event to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task PublishAndWaitAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        // For the in-memory implementation, PublishAsync already waits for completion
        await PublishAsync(domainEvent, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Publishes multiple domain events and waits for all handlers to complete.
    /// </summary>
    /// <param name="domainEvents">The collection of domain events to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task PublishAndWaitAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default
    )
    {
        if (domainEvents == null)
        {
            throw new ArgumentNullException(nameof(domainEvents));
        }

        var eventList = domainEvents.ToList();
        if (eventList.Count == 0)
        {
            _logger.LogDebug("No domain events to publish and wait for");
            return;
        }

        _logger.LogDebug("Publishing and waiting for {EventCount} domain events", eventList.Count);

        // For better error handling when waiting, we'll publish sequentially
        var exceptions = new List<Exception>();

        foreach (var domainEvent in eventList)
        {
            try
            {
                await PublishAsync(domainEvent, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                _logger.LogError(
                    ex,
                    "Failed to publish and wait for domain event {EventType} with ID {EventId}",
                    domainEvent.GetType().Name,
                    domainEvent.EventId
                );
            }
        }

        if (exceptions.Count > 0)
        {
            // Aggregate all exceptions and throw
            throw new AggregateException(
                $"Failed to publish {exceptions.Count} out of {eventList.Count} domain events",
                exceptions
            );
        }

        _logger.LogDebug("Successfully published and waited for {EventCount} domain events", eventList.Count);
    }
}
