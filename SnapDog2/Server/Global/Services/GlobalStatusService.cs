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
namespace SnapDog2.Server.Global.Services;

using Microsoft.Extensions.Hosting;
using SnapDog2.Server.Global.Services.Abstractions;
using SnapDog2.Shared.Models;

/// <summary>
/// Background service that periodically collects and caches global system status information.
/// Provides centralized access to system health, version info, and server statistics.
/// </summary>
public partial class GlobalStatusService : BackgroundService, IGlobalStatusService
{
    private readonly ILogger<GlobalStatusService> _logger;
    private Timer? _periodicTimer;
    private bool _disposed;

    public GlobalStatusService(ILogger<GlobalStatusService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogGlobalStatusServiceStarted();

        // Start periodic publishing
        await StartPeriodicPublishingAsync(stoppingToken);

        // Wait for cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public async Task PublishSystemStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            LogPublishingSystemStatus();
            // TODO: Implement actual system status publishing
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            LogSystemStatusPublishFailed(ex.Message);
        }
    }

    public async Task PublishErrorStatusAsync(ErrorDetails errorDetails, CancellationToken cancellationToken = default)
    {
        try
        {
            LogPublishingErrorStatus(errorDetails.Message ?? "Unknown error");
            // TODO: Implement actual error status publishing
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            LogErrorStatusPublishFailed(ex.Message);
        }
    }

    public async Task PublishVersionInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            LogPublishingVersionInfo();
            // TODO: Implement actual version info publishing
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            LogVersionInfoPublishFailed(ex.Message);
        }
    }

    public async Task PublishServerStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            LogPublishingServerStats();
            // TODO: Implement actual server stats publishing
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            LogServerStatsPublishFailed(ex.Message);
        }
    }

    public async Task StartPeriodicPublishingAsync(CancellationToken cancellationToken = default)
    {
        LogStartingPeriodicPublishing();

        _periodicTimer = new Timer(async _ =>
        {
            await PublishSystemStatusAsync(cancellationToken);
            await PublishServerStatsAsync(cancellationToken);
        }, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));

        await Task.CompletedTask;
    }

    public async Task StopPeriodicPublishingAsync()
    {
        LogStoppingPeriodicPublishing();

        _periodicTimer?.Dispose();
        _periodicTimer = null;

        await Task.CompletedTask;
    }

    public override void Dispose()
    {
        if (!_disposed)
        {
            _periodicTimer?.Dispose();
            _disposed = true;
        }
        base.Dispose();
    }

    // LoggerMessage methods
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Global status service started")]
    private partial void LogGlobalStatusServiceStarted();

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Publishing system status")]
    private partial void LogPublishingSystemStatus();

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to publish system status: {Error}")]
    private partial void LogSystemStatusPublishFailed(string Error);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Publishing error status: {ErrorMessage}")]
    private partial void LogPublishingErrorStatus(string ErrorMessage);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Failed to publish error status: {Error}")]
    private partial void LogErrorStatusPublishFailed(string Error);

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Publishing version info")]
    private partial void LogPublishingVersionInfo();

    [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "Failed to publish version info: {Error}")]
    private partial void LogVersionInfoPublishFailed(string Error);

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Publishing server stats")]
    private partial void LogPublishingServerStats();

    [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "Failed to publish server stats: {Error}")]
    private partial void LogServerStatsPublishFailed(string Error);

    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "Starting periodic publishing")]
    private partial void LogStartingPeriodicPublishing();

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Stopping periodic publishing")]
    private partial void LogStoppingPeriodicPublishing();
}
