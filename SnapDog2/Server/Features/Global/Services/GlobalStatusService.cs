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
namespace SnapDog2.Server.Features.Global.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Global.Handlers;
using SnapDog2.Server.Features.Global.Notifications;
using SnapDog2.Server.Features.Global.Queries;
using SnapDog2.Server.Features.Global.Services.Abstractions;

/// <summary>
/// Service for managing and publishing global system status.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GlobalStatusService"/> class.
/// </remarks>
/// <param name="systemStatusHandler">The system status query handler.</param>
/// <param name="errorStatusHandler">The error status query handler.</param>
/// <param name="versionInfoHandler">The version info query handler.</param>
/// <param name="serverStatsHandler">The server stats query handler.</param>
/// <param name="mediator">The mediator instance.</param>
/// <param name="logger">The logger instance.</param>
public partial class GlobalStatusService(
    GetSystemStatusQueryHandler systemStatusHandler,
    GetErrorStatusQueryHandler errorStatusHandler,
    GetVersionInfoQueryHandler versionInfoHandler,
    GetServerStatsQueryHandler serverStatsHandler,
    IMediator mediator,
    ILogger<GlobalStatusService> logger
) : IGlobalStatusService
{
    private readonly GetSystemStatusQueryHandler _systemStatusHandler = systemStatusHandler;
    private readonly GetErrorStatusQueryHandler _errorStatusHandler = errorStatusHandler;
    private readonly GetVersionInfoQueryHandler _versionInfoHandler = versionInfoHandler;
    private readonly GetServerStatsQueryHandler _serverStatsHandler = serverStatsHandler;
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<GlobalStatusService> _logger = logger;
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

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
                this.LogSystemStatusRetrieved(result.Value.IsOnline);
            }
            else
            {
                this.LogFailedToGetSystemStatusForPublishing(result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            this.LogFailedToPublishSystemStatus(ex);
        }
    }

    /// <inheritdoc />
    public async Task PublishErrorStatusAsync(ErrorDetails errorDetails, CancellationToken cancellationToken = default)
    {
        try
        {
            // Publish error notification to external systems (MQTT, KNX) via mediator
            await this._mediator.PublishAsync(new SystemErrorNotification { Error = errorDetails }, cancellationToken);
            this.LogErrorStatusToPublish(errorDetails.ErrorCode);
        }
        catch (Exception ex)
        {
            this.LogFailedToPublishErrorStatus(ex);
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
                this.LogVersionInfoRetrieved(result.Value.Version);
            }
            else
            {
                this.LogFailedToGetVersionInfoForPublishing(result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            this.LogFailedToPublishVersionInfo(ex);
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
                this.LogServerStatsRetrieved(result.Value.CpuUsagePercent, result.Value.MemoryUsageMb);
            }
            else
            {
                this.LogFailedToGetServerStatsForPublishing(result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            this.LogFailedToPublishServerStats(ex);
        }
    }

    /// <inheritdoc />
    public async Task StartPeriodicPublishingAsync(CancellationToken cancellationToken = default)
    {
        this.LogStartingPeriodicGlobalStatusPublishing();

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
                    this.LogErrorInPeriodicSystemStatusPublishing(ex);
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
                    this.LogErrorInPeriodicServerStatsPublishing(ex);
                }
            },
            cancellationToken
        );

        this.LogPeriodicGlobalStatusPublishingStarted();
    }

    /// <inheritdoc />
    public async Task StopPeriodicPublishingAsync()
    {
        this.LogStoppingPeriodicGlobalStatusPublishing();

        this._cancellationTokenSource.Cancel();

        this.LogPeriodicGlobalStatusPublishingStopped();
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
