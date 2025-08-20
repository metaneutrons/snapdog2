namespace SnapDog2.Infrastructure.Notifications;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;

internal sealed class NotificationItem
{
    public required string EventType { get; init; }
    public required string EntityType { get; init; } // "Zone", "Client", "Global"
    public required string EntityId { get; init; } // Zone index, client index, or "system"
    public required object Payload { get; init; }
    public int Attempt { get; set; }
}

/// <summary>
/// Default queue implementation using bounded Channel<T> with backpressure.
/// </summary>
public sealed class NotificationQueue : INotificationQueue
{
    private readonly Channel<NotificationItem> _queue;
    private readonly ILogger<NotificationQueue> _logger;
    private readonly IMetricsService? _metrics;
    private int _depth;

    internal ChannelReader<NotificationItem> Reader => this._queue.Reader;
    internal Task ReaderCompletion => this._queue.Reader.Completion;

    public NotificationQueue(
        IOptions<NotificationProcessingOptions> options,
        ILogger<NotificationQueue> logger,
        IMetricsService? metrics = null
    )
    {
        this._logger = logger;
        this._metrics = metrics;
        var capacity = Math.Max(16, options.Value.MaxQueueCapacity);
        var channelOptions = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        };
        this._queue = Channel.CreateBounded<NotificationItem>(channelOptions);
    }

    public async Task EnqueueZoneAsync<T>(
        string eventType,
        int zoneIndex,
        T payload,
        CancellationToken cancellationToken
    )
    {
        var item = new NotificationItem
        {
            EventType = eventType,
            EntityType = "Zone",
            EntityId = zoneIndex.ToString(),
            Payload = payload!,
            Attempt = 0,
        };

        await this._queue.Writer.WriteAsync(item, cancellationToken);
        var newDepth = Interlocked.Increment(ref this._depth);
        this._logger.LogDebug("Enqueued notification {EventType} for zone {ZoneIndex}", eventType, zoneIndex);
        this._metrics?.IncrementCounter("notifications_enqueued_total", 1, ("event", eventType));
        this._metrics?.SetGauge("notifications_queue_depth", newDepth);
    }

    public async Task EnqueueClientAsync<T>(
        string eventType,
        string clientIndex,
        T payload,
        CancellationToken cancellationToken
    )
    {
        var item = new NotificationItem
        {
            EventType = eventType,
            EntityType = "Client",
            EntityId = clientIndex,
            Payload = payload!,
            Attempt = 0,
        };

        await this._queue.Writer.WriteAsync(item, cancellationToken);
        var newDepth = Interlocked.Increment(ref this._depth);
        this._logger.LogDebug("Enqueued notification {EventType} for client {ClientIndex}", eventType, clientIndex);
        this._metrics?.IncrementCounter("notifications_enqueued_total", 1, ("event", eventType));
        this._metrics?.SetGauge("notifications_queue_depth", newDepth);
    }

    public async Task EnqueueGlobalAsync<T>(string eventType, T payload, CancellationToken cancellationToken)
    {
        var item = new NotificationItem
        {
            EventType = eventType,
            EntityType = "Global",
            EntityId = "system",
            Payload = payload!,
            Attempt = 0,
        };

        await this._queue.Writer.WriteAsync(item, cancellationToken);
        var newDepth = Interlocked.Increment(ref this._depth);
        this._logger.LogDebug("Enqueued notification {EventType} for global system", eventType);
        this._metrics?.IncrementCounter("notifications_enqueued_total", 1, ("event", eventType));
        this._metrics?.SetGauge("notifications_queue_depth", newDepth);
    }

    internal void OnItemDequeued(NotificationItem item)
    {
        var newDepth = Interlocked.Decrement(ref this._depth);
        this._metrics?.IncrementCounter("notifications_dequeued_total", 1, ("event", item.EventType));
        this._metrics?.SetGauge("notifications_queue_depth", Math.Max(0, newDepth));
    }

    internal void CompleteWriter()
    {
        this._queue.Writer.TryComplete();
    }
}
