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

using Microsoft.Extensions.Options;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Models;

/// <summary>
/// Background service that reads notifications from the queue and publishes
/// them to external systems (MQTT/KNX) with retry and graceful shutdown.
/// </summary>
public sealed partial class NotificationBackgroundService(
    NotificationQueue queue,
    IServiceProvider services,
    IOptions<NotificationProcessingOptions> options,
    ILogger<NotificationBackgroundService> logger,
    IMetricsService? metrics = null
) : BackgroundService
{
    private readonly NotificationQueue _queue = queue;
    private readonly IMqttService? _mqtt = services.GetService<IMqttService>();
    private readonly IKnxService? _knx = services.GetService<IKnxService>();
    private readonly NotificationProcessingOptions _options = options.Value;
    private readonly ILogger<NotificationBackgroundService> _logger = logger;
    private readonly IMetricsService? _metrics = metrics;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogServiceStarted(this._logger);

        var readers = new List<Task>();
        var concurrency = Math.Max(1, this._options.MaxConcurrency);
        for (var i = 0; i < concurrency; i++)
        {
            readers.Add(this.RunReaderAsync(stoppingToken));
        }

        await Task.WhenAll(readers);

        LogServiceStopped(this._logger);
    }

    private async Task RunReaderAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in this._queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await this.ProcessItemWithRetryAsync(item, stoppingToken);
                this._queue.OnItemDequeued(item);
            }
            catch (OperationCanceledException)
            {
                // shutdown
                break;
            }
            catch (Exception ex)
            {
                LogUnhandledError(this._logger, ex, item.EventType, item.EntityType, item.EntityId);
            }
        }
    }

    private async Task ProcessItemWithRetryAsync(NotificationItem item, CancellationToken ct)
    {
        var attempt = 0;
        var maxAttempts = Math.Max(1, this._options.MaxRetryAttempts);
        var baseDelay = TimeSpan.FromMilliseconds(Math.Max(0, this._options.RetryBaseDelayMs));
        var maxDelay = TimeSpan.FromMilliseconds(
            Math.Max(this._options.RetryBaseDelayMs, this._options.RetryMaxDelayMs)
        );

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await this.PublishAsync(item, ct);
                this._metrics?.IncrementCounter("notifications_processed_total", 1, ("event", item.EventType));
                LogNotificationProcessed(this._logger, item.EventType, item.EntityType, item.EntityId);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                attempt++;
                if (attempt >= maxAttempts)
                {
                    LogNotificationFailed(this._logger, ex, item.EventType, item.EntityType, item.EntityId, attempt);
                    this._metrics?.IncrementCounter("notifications_dead_letter_total", 1, ("event", item.EventType));
                    // dead-letter hook: could enqueue to an external DLQ or emit metric/log for inspection
                    return;
                }

                // exponential backoff with jitter
                var delay = TimeSpan.FromMilliseconds(
                    Math.Min(maxDelay.TotalMilliseconds, baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1))
                );
                var jitter = TimeSpan.FromMilliseconds(
                    Random.Shared.Next(0, (int)Math.Min(250, delay.TotalMilliseconds / 5))
                );
                var totalDelay = delay + jitter;
                this._metrics?.IncrementCounter("notifications_retried_total", 1, ("event", item.EventType));
                LogRetryingNotification(
                    this._logger,
                    ex,
                    item.EventType,
                    item.EntityType,
                    item.EntityId,
                    attempt + 1,
                    maxAttempts,
                    (int)totalDelay.TotalMilliseconds
                );
                await Task.Delay(totalDelay, ct);
            }
        }
    }

    private async Task PublishAsync(NotificationItem item, CancellationToken ct)
    {
        // MQTT
        if (this._mqtt != null && this._mqtt.IsConnected)
        {
            Result mqttResult = item.EntityType switch
            {
                "Zone" when int.TryParse(item.EntityId, out var zoneIndex) => await this._mqtt.PublishZoneStatusAsync(
                    zoneIndex,
                    item.EventType,
                    (dynamic)item.Payload,
                    ct
                ),
                "Client" => await this._mqtt.PublishClientStatusAsync(
                    item.EntityId,
                    item.EventType,
                    (dynamic)item.Payload,
                    ct
                ),
                "Global" => await this._mqtt.PublishGlobalStatusAsync(item.EventType, (dynamic)item.Payload, ct),
                _ => Result.Failure($"Unknown entity type: {item.EntityType}"),
            };

            if (mqttResult.IsFailure)
            {
                throw new InvalidOperationException(mqttResult.ErrorMessage ?? "MQTT publish failed");
            }
        }

        // KNX
        if (this._knx != null && this._knx.IsConnected)
        {
            Result knxResult = item.EntityType switch
            {
                "Zone" when int.TryParse(item.EntityId, out var zoneIndex) => await this._knx.PublishZoneStatusAsync(
                    zoneIndex,
                    item.EventType,
                    (dynamic)item.Payload,
                    ct
                ),
                "Client" => await this._knx.PublishClientStatusAsync(
                    item.EntityId,
                    item.EventType,
                    (dynamic)item.Payload,
                    ct
                ),
                "Global" => await this._knx.PublishGlobalStatusAsync(item.EventType, (dynamic)item.Payload, ct),
                _ => Result.Failure($"Unknown entity type: {item.EntityType}"),
            };

            if (knxResult.IsFailure)
            {
                throw new InvalidOperationException(knxResult.ErrorMessage ?? "KNX publish failed");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Graceful drain: complete writer and wait up to configured timeout
        this._queue.CompleteWriter();
        var timeout = TimeSpan.FromSeconds(Math.Max(1, this._options.ShutdownTimeoutSeconds));
        using var drainCts = new CancellationTokenSource(timeout);
        try
        {
            await Task.WhenAny(this._queue.ReaderCompletion, Task.Delay(Timeout.InfiniteTimeSpan, drainCts.Token));
        }
        catch
        {
            // ignore
        }

        await base.StopAsync(cancellationToken);
    }

    // LoggerMessage methods for high-performance logging
    [LoggerMessage(
        EventId = 6800,
        Level = LogLevel.Information,
        Message = "Notification background service started"
    )]
    private static partial void LogServiceStarted(ILogger logger);

    [LoggerMessage(
        EventId = 6801,
        Level = LogLevel.Information,
        Message = "Notification background service stopped"
    )]
    private static partial void LogServiceStopped(ILogger logger);

    [LoggerMessage(
        EventId = 6802,
        Level = LogLevel.Error,
        Message = "Unhandled error processing notification {EventType} for {EntityType} {EntityId}"
    )]
    private static partial void LogUnhandledError(
        ILogger logger,
        Exception ex,
        string eventType,
        string entityType,
        string entityId
    );

    [LoggerMessage(
        EventId = 6803,
        Level = LogLevel.Debug,
        Message = "Notification {EventType} for {EntityType} {EntityId} processed"
    )]
    private static partial void LogNotificationProcessed(
        ILogger logger,
        string eventType,
        string entityType,
        string entityId
    );

    [LoggerMessage(
        EventId = 6804,
        Level = LogLevel.Error,
        Message = "Notification {EventType} for {EntityType} {EntityId} failed after {Attempts} attempts; dead-lettering"
    )]
    private static partial void LogNotificationFailed(
        ILogger logger,
        Exception ex,
        string eventType,
        string entityType,
        string entityId,
        int attempts
    );

    [LoggerMessage(
        EventId = 6805,
        Level = LogLevel.Warning,
        Message = "Retrying notification {EventType} for {EntityType} {EntityId} (attempt {Attempt}/{MaxAttempts}) after {Delay}ms"
    )]
    private static partial void LogRetryingNotification(
        ILogger logger,
        Exception ex,
        string eventType,
        string entityType,
        string entityId,
        int attempt,
        int maxAttempts,
        int delay
    );
}
