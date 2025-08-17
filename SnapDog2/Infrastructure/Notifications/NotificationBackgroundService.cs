namespace SnapDog2.Infrastructure.Notifications;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Models;

/// <summary>
/// Background service that reads notifications from the queue and publishes
/// them to external systems (MQTT/KNX) with retry and graceful shutdown.
/// </summary>
public sealed class NotificationBackgroundService(
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
        this._logger.LogInformation("Notification background service started");

        var readers = new List<Task>();
        var concurrency = Math.Max(1, this._options.MaxConcurrency);
        for (int i = 0; i < concurrency; i++)
        {
            readers.Add(this.RunReaderAsync(stoppingToken));
        }

        await Task.WhenAll(readers);

        this._logger.LogInformation("Notification background service stopped");
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
                this._logger.LogError(
                    ex,
                    "Unhandled error processing notification {EventType} for zone {ZoneIndex}",
                    item.EventType,
                    item.ZoneIndex
                );
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
                this._logger.LogDebug(
                    "Notification {EventType} for zone {ZoneIndex} processed",
                    item.EventType,
                    item.ZoneIndex
                );
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
                    this._logger.LogError(
                        ex,
                        "Notification {EventType} for zone {ZoneIndex} failed after {Attempts} attempts; dead-lettering",
                        item.EventType,
                        item.ZoneIndex,
                        attempt
                    );
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
                this._logger.LogWarning(
                    ex,
                    "Retrying notification {EventType} for zone {ZoneIndex} (attempt {Attempt}/{MaxAttempts}) after {Delay}ms",
                    item.EventType,
                    item.ZoneIndex,
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
            // Use dynamic to invoke the generic method with runtime payload type
            var mqttResult = await this
                ._mqtt.PublishZoneStatusAsync(item.ZoneIndex, item.EventType, (dynamic)item.Payload, ct)
                .ConfigureAwait(false);
            if (mqttResult.IsFailure)
            {
                throw new InvalidOperationException(mqttResult.ErrorMessage ?? "MQTT publish failed");
            }
        }

        // KNX
        if (this._knx != null && this._knx.IsConnected)
        {
            var knxResult = await this
                ._knx.PublishZoneStatusAsync(item.ZoneIndex, item.EventType, (dynamic)item.Payload, ct)
                .ConfigureAwait(false);
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
}
