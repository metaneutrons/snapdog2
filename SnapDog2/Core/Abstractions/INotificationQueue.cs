namespace SnapDog2.Core.Abstractions;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Asynchronous notification queue for publishing zone/client status updates to external systems.
/// Producers enqueue lightweight items and return immediately; a background service processes items.
/// </summary>
public interface INotificationQueue
{
    /// <summary>
    /// Enqueues a zone-scoped notification to be published to external systems.
    /// </summary>
    /// <typeparam name="T">Payload type (will be serialized or mapped by publishers)</typeparam>
    /// <param name="eventType">StatusId string for the notification</param>
    /// <param name="zoneIndex">Zone identifier</param>
    /// <param name="payload">Payload object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnqueueZoneAsync<T>(string eventType, int zoneIndex, T payload, CancellationToken cancellationToken);

    /// <summary>
    /// Enqueues a client-scoped notification to be published to external systems.
    /// </summary>
    /// <typeparam name="T">Payload type (will be serialized or mapped by publishers)</typeparam>
    /// <param name="eventType">StatusId string for the notification</param>
    /// <param name="clientIndex">Client identifier</param>
    /// <param name="payload">Payload object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnqueueClientAsync<T>(string eventType, string clientIndex, T payload, CancellationToken cancellationToken);

    /// <summary>
    /// Enqueues a global system notification to be published to external systems.
    /// </summary>
    /// <typeparam name="T">Payload type (will be serialized or mapped by publishers)</typeparam>
    /// <param name="eventType">StatusId string for the notification</param>
    /// <param name="payload">Payload object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnqueueGlobalAsync<T>(string eventType, T payload, CancellationToken cancellationToken);
}
