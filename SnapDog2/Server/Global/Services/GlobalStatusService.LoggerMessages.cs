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
        EventId = 7900,
        Level = LogLevel.Debug,
        Message = "System status retrieved: {IsOnline}"
    )]
    private partial void LogSystemStatusRetrieved(bool isOnline);

    [LoggerMessage(
        EventId = 7901,
        Level = LogLevel.Warning,
        Message = "Failed to get system status for publishing: {Error}"
    )]
    private partial void LogFailedToGetSystemStatusForPublishing(string? error);

    [LoggerMessage(
        EventId = 7902,
        Level = LogLevel.Error,
        Message = "Failed to publish system status"
    )]
    private partial void LogFailedToPublishSystemStatus(Exception ex);

    // Error Status Publishing Operations (9404-9405)
    [LoggerMessage(
        EventId = 7903,
        Level = LogLevel.Debug,
        Message = "Error status to publish: {ErrorCode}"
    )]
    private partial void LogErrorStatusToPublish(string errorCode);

    [LoggerMessage(
        EventId = 7904,
        Level = LogLevel.Error,
        Message = "Failed to publish error status"
    )]
    private partial void LogFailedToPublishErrorStatus(Exception ex);

    // Version Info Publishing Operations (9406-9408)
    [LoggerMessage(
        EventId = 7905,
        Level = LogLevel.Debug,
        Message = "Version info retrieved: {Version}"
    )]
    private partial void LogVersionInfoRetrieved(string version);

    [LoggerMessage(
        EventId = 7906,
        Level = LogLevel.Warning,
        Message = "Failed to get version info for publishing: {Error}"
    )]
    private partial void LogFailedToGetVersionInfoForPublishing(string? error);

    [LoggerMessage(
        EventId = 7907,
        Level = LogLevel.Error,
        Message = "Failed to publish version info"
    )]
    private partial void LogFailedToPublishVersionInfo(Exception ex);

    // Server Stats Publishing Operations (9409-9411)
    [LoggerMessage(
        EventId = 7908,
        Level = LogLevel.Debug,
        Message = "Server stats retrieved: CPU={CpuUsage:F2}%, Memory={MemoryUsage:F2}MB"
    )]
    private partial void LogServerStatsRetrieved(double cpuUsage, double memoryUsage);

    [LoggerMessage(
        EventId = 7909,
        Level = LogLevel.Warning,
        Message = "Failed to get server stats for publishing: {Error}"
    )]
    private partial void LogFailedToGetServerStatsForPublishing(string? error);

    [LoggerMessage(
        EventId = 7910,
        Level = LogLevel.Error,
        Message = "Failed to publish server stats"
    )]
    private partial void LogFailedToPublishServerStats(Exception ex);

    // Periodic Publishing Lifecycle Operations (9412-9417)
    [LoggerMessage(
        EventId = 7911,
        Level = LogLevel.Information,
        Message = "Starting periodic global status publishing"
    )]
    private partial void LogStartingPeriodicGlobalStatusPublishing();

    [LoggerMessage(
        EventId = 7912,
        Level = LogLevel.Error,
        Message = "Error in periodic system status publishing"
    )]
    private partial void LogErrorInPeriodicSystemStatusPublishing(Exception ex);

    [LoggerMessage(
        EventId = 7913,
        Level = LogLevel.Error,
        Message = "Error in periodic server stats publishing"
    )]
    private partial void LogErrorInPeriodicServerStatsPublishing(Exception ex);

    [LoggerMessage(
        EventId = 7914,
        Level = LogLevel.Information,
        Message = "Periodic global status publishing started"
    )]
    private partial void LogPeriodicGlobalStatusPublishingStarted();

    [LoggerMessage(
        EventId = 7915,
        Level = LogLevel.Information,
        Message = "Stopping periodic global status publishing"
    )]
    private partial void LogStoppingPeriodicGlobalStatusPublishing();

    [LoggerMessage(
        EventId = 7916,
        Level = LogLevel.Information,
        Message = "Periodic global status publishing stopped"
    )]
    private partial void LogPeriodicGlobalStatusPublishingStopped();
}
