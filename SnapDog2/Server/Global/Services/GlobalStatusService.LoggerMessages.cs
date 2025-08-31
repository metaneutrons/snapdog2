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

/// <summary>
/// High-performance LoggerMessage definitions for GlobalStatusService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class GlobalStatusService
{
    // System Status Publishing Operations (9401-9403)
    [LoggerMessage(
        EventId = 4800,
        Level = LogLevel.Debug,
        Message = "System status retrieved: {IsOnline}"
    )]
    private partial void LogSystemStatusRetrieved(bool isOnline);

    [LoggerMessage(
        EventId = 4801,
        Level = LogLevel.Warning,
        Message = "Failed to get system status for publishing: {Error}"
    )]
    private partial void LogFailedToGetSystemStatusForPublishing(string? error);

    [LoggerMessage(
        EventId = 4802,
        Level = LogLevel.Error,
        Message = "Failed to publish system status"
    )]
    private partial void LogFailedToPublishSystemStatus(Exception ex);

    // Error Status Publishing Operations (9404-9405)
    [LoggerMessage(
        EventId = 4803,
        Level = LogLevel.Debug,
        Message = "Error status to publish: {ErrorCode}"
    )]
    private partial void LogErrorStatusToPublish(string errorCode);

    [LoggerMessage(
        EventId = 4804,
        Level = LogLevel.Error,
        Message = "Failed to publish error status"
    )]
    private partial void LogFailedToPublishErrorStatus(Exception ex);

    // Version Info Publishing Operations (9406-9408)
    [LoggerMessage(
        EventId = 4805,
        Level = LogLevel.Debug,
        Message = "Version info retrieved: {Version}"
    )]
    private partial void LogVersionInfoRetrieved(string version);

    [LoggerMessage(
        EventId = 4806,
        Level = LogLevel.Warning,
        Message = "Failed to get version info for publishing: {Error}"
    )]
    private partial void LogFailedToGetVersionInfoForPublishing(string? error);

    [LoggerMessage(
        EventId = 4807,
        Level = LogLevel.Error,
        Message = "Failed to publish version info"
    )]
    private partial void LogFailedToPublishVersionInfo(Exception ex);

    // Server Stats Publishing Operations (9409-9411)
    [LoggerMessage(
        EventId = 4808,
        Level = LogLevel.Debug,
        Message = "Server stats retrieved: CPU={CpuUsage:F2}%, Memory={MemoryUsage:F2}MB"
    )]
    private partial void LogServerStatsRetrieved(double cpuUsage, double memoryUsage);

    [LoggerMessage(
        EventId = 4809,
        Level = LogLevel.Warning,
        Message = "Failed to get server stats for publishing: {Error}"
    )]
    private partial void LogFailedToGetServerStatsForPublishing(string? error);

    [LoggerMessage(
        EventId = 4810,
        Level = LogLevel.Error,
        Message = "Failed to publish server stats"
    )]
    private partial void LogFailedToPublishServerStats(Exception ex);

    // Periodic Publishing Lifecycle Operations (9412-9417)
    [LoggerMessage(
        EventId = 4811,
        Level = LogLevel.Information,
        Message = "Starting periodic global status publishing"
    )]
    private partial void LogStartingPeriodicGlobalStatusPublishing();

    [LoggerMessage(
        EventId = 4812,
        Level = LogLevel.Error,
        Message = "Error in periodic system status publishing"
    )]
    private partial void LogErrorInPeriodicSystemStatusPublishing(Exception ex);

    [LoggerMessage(
        EventId = 4813,
        Level = LogLevel.Error,
        Message = "Error in periodic server stats publishing"
    )]
    private partial void LogErrorInPeriodicServerStatsPublishing(Exception ex);

    [LoggerMessage(
        EventId = 4814,
        Level = LogLevel.Information,
        Message = "Periodic global status publishing started"
    )]
    private partial void LogPeriodicGlobalStatusPublishingStarted();

    [LoggerMessage(
        EventId = 4815,
        Level = LogLevel.Information,
        Message = "Stopping periodic global status publishing"
    )]
    private partial void LogStoppingPeriodicGlobalStatusPublishing();

    [LoggerMessage(
        EventId = 4816,
        Level = LogLevel.Information,
        Message = "Periodic global status publishing stopped"
    )]
    private partial void LogPeriodicGlobalStatusPublishingStopped();
}
