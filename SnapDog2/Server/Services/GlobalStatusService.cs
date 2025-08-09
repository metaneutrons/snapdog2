namespace SnapDog2.Server.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Global.Handlers;
using SnapDog2.Server.Features.Global.Queries;
using SnapDog2.Server.Features.Shared.Notifications;
using SnapDog2.Server.Services.Abstractions;

/// <summary>
/// Service for managing and publishing global system status.
/// </summary>
public class GlobalStatusService : IGlobalStatusService
{
    private readonly GetSystemStatusQueryHandler _systemStatusHandler;
    private readonly GetErrorStatusQueryHandler _errorStatusHandler;
    private readonly GetVersionInfoQueryHandler _versionInfoHandler;
    private readonly GetServerStatsQueryHandler _serverStatsHandler;
    private readonly IMediator _mediator;
    private readonly ILogger<GlobalStatusService> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalStatusService"/> class.
    /// </summary>
    /// <param name="systemStatusHandler">The system status query handler.</param>
    /// <param name="errorStatusHandler">The error status query handler.</param>
    /// <param name="versionInfoHandler">The version info query handler.</param>
    /// <param name="serverStatsHandler">The server stats query handler.</param>
    /// <param name="mediator">The mediator instance.</param>
    /// <param name="logger">The logger instance.</param>
    public GlobalStatusService(
        GetSystemStatusQueryHandler systemStatusHandler,
        GetErrorStatusQueryHandler errorStatusHandler,
        GetVersionInfoQueryHandler versionInfoHandler,
        GetServerStatsQueryHandler serverStatsHandler,
        IMediator mediator,
        ILogger<GlobalStatusService> logger
    )
    {
        this._systemStatusHandler = systemStatusHandler;
        this._errorStatusHandler = errorStatusHandler;
        this._versionInfoHandler = versionInfoHandler;
        this._serverStatsHandler = serverStatsHandler;
        this._mediator = mediator;
        this._logger = logger;
        this._cancellationTokenSource = new CancellationTokenSource();
    }

    /// <inheritdoc />
    public async Task PublishSystemStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await this._systemStatusHandler.Handle(new GetSystemStatusQuery(), cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                // Publish notification to external systems (MQTT, KNX) via mediator
                await this._mediator.PublishAsync(
                    new SystemStatusChangedNotification { Status = result.Value },
                    cancellationToken
                );
                this._logger.LogDebug("System status retrieved: {IsOnline}", result.Value.IsOnline);
            }
            else
            {
                this._logger.LogWarning("Failed to get system status for publishing: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to publish system status");
        }
    }

    /// <inheritdoc />
    public async Task PublishErrorStatusAsync(ErrorDetails errorDetails, CancellationToken cancellationToken = default)
    {
        try
        {
            // Publish error notification to external systems (MQTT, KNX) via mediator
            await this._mediator.PublishAsync(new SystemErrorNotification { Error = errorDetails }, cancellationToken);
            this._logger.LogDebug("Error status to publish: {ErrorCode}", errorDetails.ErrorCode);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to publish error status");
        }
    }

    /// <inheritdoc />
    public async Task PublishVersionInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await this._versionInfoHandler.Handle(new GetVersionInfoQuery(), cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                // Publish version info to external systems (MQTT, KNX) via mediator
                await this._mediator.PublishAsync(
                    new VersionInfoChangedNotification { VersionInfo = result.Value },
                    cancellationToken
                );
                this._logger.LogDebug("Version info retrieved: {Version}", result.Value.Version);
            }
            else
            {
                this._logger.LogWarning("Failed to get version info for publishing: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to publish version info");
        }
    }

    /// <inheritdoc />
    public async Task PublishServerStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await this._serverStatsHandler.Handle(new GetServerStatsQuery(), cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                // Publish server stats to external systems (MQTT, KNX) via mediator
                await this._mediator.PublishAsync(
                    new ServerStatsChangedNotification { Stats = result.Value },
                    cancellationToken
                );
                this._logger.LogDebug(
                    "Server stats retrieved: CPU={CpuUsage}%, Memory={MemoryUsage}MB",
                    result.Value.CpuUsagePercent,
                    result.Value.MemoryUsageMb
                );
            }
            else
            {
                this._logger.LogWarning("Failed to get server stats for publishing: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to publish server stats");
        }
    }

    /// <inheritdoc />
    public async Task StartPeriodicPublishingAsync(CancellationToken cancellationToken = default)
    {
        this._logger.LogInformation("Starting periodic global status publishing");

        // Publish initial status
        await this.PublishSystemStatusAsync(cancellationToken);
        await this.PublishVersionInfoAsync(cancellationToken);

        // Start periodic timers for status updates
        _ = Task.Run(
            async () =>
            {
                try
                {
                    // Publish system status every 30 seconds
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                        await this.PublishSystemStatusAsync(cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Error in periodic system status publishing");
                }
            },
            cancellationToken
        );

        _ = Task.Run(
            async () =>
            {
                try
                {
                    // Publish server stats every 60 seconds
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
                        await this.PublishServerStatsAsync(cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Error in periodic server stats publishing");
                }
            },
            cancellationToken
        );

        this._logger.LogInformation("Periodic global status publishing started");
    }

    /// <inheritdoc />
    public async Task StopPeriodicPublishingAsync()
    {
        this._logger.LogInformation("Stopping periodic global status publishing");

        this._cancellationTokenSource.Cancel();

        this._logger.LogInformation("Periodic global status publishing stopped");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the service and its resources.
    /// </summary>
    public void Dispose()
    {
        this._cancellationTokenSource?.Dispose();
    }
}
