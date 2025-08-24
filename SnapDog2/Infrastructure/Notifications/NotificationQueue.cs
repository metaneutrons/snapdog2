//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
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
public sealed partial class NotificationQueue : INotificationQueue
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
        this.LogZoneNotificationEnqueued(eventType, zoneIndex);
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
        this.LogClientNotificationEnqueued(eventType, clientIndex);
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
        this.LogGlobalNotificationEnqueued(eventType);
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

    [LoggerMessage(
        EventId = 6900,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Enqueued notification {EventType} for zone {ZoneIndex}"
    )]
    private partial void LogZoneNotificationEnqueued(string eventType, int zoneIndex);

    [LoggerMessage(
        EventId = 6901,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Enqueued notification {EventType} for client {ClientIndex}"
    )]
    private partial void LogClientNotificationEnqueued(string eventType, string clientIndex);

    [LoggerMessage(
        EventId = 6902,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Enqueued notification {EventType} for global system"
    )]
    private partial void LogGlobalNotificationEnqueued(string eventType);
}
