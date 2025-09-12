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
    private readonly IServiceScopeFactory _scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
    private readonly NotificationProcessingOptions _options = options.Value;
    private readonly ILogger<NotificationBackgroundService> _logger = logger;
    private readonly IMetricsService? _metrics = metrics;

    async protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogServiceStarted();

        var readers = new List<Task>();
        var concurrency = Math.Max(1, this._options.MaxConcurrency);
        for (var i = 0; i < concurrency; i++)
        {
            readers.Add(this.RunReaderAsync(stoppingToken));
        }

        await Task.WhenAll(readers);

        LogServiceStopped();
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
                _logger.LogInformation("Notification processing failed for {EventType} {EntityType} {EntityId}: {Error}", item.EventType, item.EntityType, item.EntityId, ex.Message);
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
                _logger.LogInformation("Notification processed: {EventType} {EntityType} {EntityId}", item.EventType, item.EntityType, item.EntityId);
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
                    _logger.LogInformation("Notification failed after {Attempts} attempts for {EventType} {EntityType} {EntityId}: {Error}", attempt, item.EventType, item.EntityType, item.EntityId, ex.Message);
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
                _logger.LogWarning(ex, "RetryingNotification: {EventType} {EntityType} {EntityId} {Attempt}/{MaxAttempts} DelayMs={DelayMs}",
                    item.EventType,
                    item.EntityType,
                    item.EntityId,
                    attempt + 1,
                    maxAttempts,
                    (int)totalDelay.TotalMilliseconds);
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
        using var scope = _scopeFactory.CreateScope();
        var knx = scope.ServiceProvider.GetService<IKnxService>();
        if (knx != null && knx.IsConnected)
        {
            Result knxResult = item.EntityType switch
            {
                "Zone" when int.TryParse(item.EntityId, out var zoneIndex) => await knx.PublishZoneStatusAsync(
                    zoneIndex,
                    item.EventType,
                    (dynamic)item.Payload,
                    ct
                ),
                "Client" => await knx.PublishClientStatusAsync(
                    item.EntityId,
                    item.EventType,
                    (dynamic)item.Payload,
                    ct
                ),
                "Global" => await knx.PublishGlobalStatusAsync(item.EventType, (dynamic)item.Payload, ct),
                _ => Result.Failure($"Unknown entity type: {item.EntityType}"),
            };

            if (knxResult.IsFailure)
            {
                throw new InvalidOperationException(knxResult.ErrorMessage ?? "KNX publish failed");
            }
        }
    }

    public async override Task StopAsync(CancellationToken cancellationToken)
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

        await base.StopAsync(drainCts.Token);
    }

    // LoggerMessage methods for high-performance logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Notification service started")]
    private partial void LogServiceStarted();

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Notification service stopped")]
    private partial void LogServiceStopped();

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Notification processing failed for {EventType} {EntityType} {EntityId}: {Error}")]
    private partial void LogNotificationProcessingFailed(string EventType, string EntityType, string EntityId, string Error);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Notification processed: {EventType} {EntityType} {EntityId}")]
    private partial void LogNotificationProcessed(string EventType, string EntityType, string EntityId);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Notification failed after {Attempts} attempts for {EventType} {EntityType} {EntityId}: {Error}")]
    private partial void LogNotificationFailedAfterRetries(int Attempts, string EventType, string EntityType, string EntityId, string Error);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Retrying notification: {EventType} {EntityType} {EntityId} {Attempt}/{MaxAttempts} DelayMs={DelayMs}")]
    private partial void LogRetryingNotification(Exception ex, string EventType, string EntityType, string EntityId, int Attempt, int MaxAttempts, int DelayMs);
}
