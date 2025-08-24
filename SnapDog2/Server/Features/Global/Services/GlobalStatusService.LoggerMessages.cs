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
using Microsoft.Extensions.Logging;

namespace SnapDog2.Server.Features.Global.Services;

/// <summary>
/// High-performance LoggerMessage definitions for GlobalStatusService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class GlobalStatusService
{
    // System Status Publishing Operations (9401-9403)
    [LoggerMessage(9401, LogLevel.Debug, "System status retrieved: {IsOnline}")]
    private partial void LogSystemStatusRetrieved(bool isOnline);

    [LoggerMessage(9402, LogLevel.Warning, "Failed to get system status for publishing: {Error}")]
    private partial void LogFailedToGetSystemStatusForPublishing(string? error);

    [LoggerMessage(9403, LogLevel.Error, "Failed to publish system status")]
    private partial void LogFailedToPublishSystemStatus(Exception ex);

    // Error Status Publishing Operations (9404-9405)
    [LoggerMessage(9404, LogLevel.Debug, "Error status to publish: {ErrorCode}")]
    private partial void LogErrorStatusToPublish(string errorCode);

    [LoggerMessage(9405, LogLevel.Error, "Failed to publish error status")]
    private partial void LogFailedToPublishErrorStatus(Exception ex);

    // Version Info Publishing Operations (9406-9408)
    [LoggerMessage(9406, LogLevel.Debug, "Version info retrieved: {Version}")]
    private partial void LogVersionInfoRetrieved(string version);

    [LoggerMessage(9407, LogLevel.Warning, "Failed to get version info for publishing: {Error}")]
    private partial void LogFailedToGetVersionInfoForPublishing(string? error);

    [LoggerMessage(9408, LogLevel.Error, "Failed to publish version info")]
    private partial void LogFailedToPublishVersionInfo(Exception ex);

    // Server Stats Publishing Operations (9409-9411)
    [LoggerMessage(9409, LogLevel.Debug, "Server stats retrieved: CPU={CpuUsage}%, Memory={MemoryUsage}MB")]
    private partial void LogServerStatsRetrieved(double cpuUsage, double memoryUsage);

    [LoggerMessage(9410, LogLevel.Warning, "Failed to get server stats for publishing: {Error}")]
    private partial void LogFailedToGetServerStatsForPublishing(string? error);

    [LoggerMessage(9411, LogLevel.Error, "Failed to publish server stats")]
    private partial void LogFailedToPublishServerStats(Exception ex);

    // Periodic Publishing Lifecycle Operations (9412-9417)
    [LoggerMessage(9412, LogLevel.Information, "Starting periodic global status publishing")]
    private partial void LogStartingPeriodicGlobalStatusPublishing();

    [LoggerMessage(9413, LogLevel.Error, "Error in periodic system status publishing")]
    private partial void LogErrorInPeriodicSystemStatusPublishing(Exception ex);

    [LoggerMessage(9414, LogLevel.Error, "Error in periodic server stats publishing")]
    private partial void LogErrorInPeriodicServerStatsPublishing(Exception ex);

    [LoggerMessage(9415, LogLevel.Information, "Periodic global status publishing started")]
    private partial void LogPeriodicGlobalStatusPublishingStarted();

    [LoggerMessage(9416, LogLevel.Information, "Stopping periodic global status publishing")]
    private partial void LogStoppingPeriodicGlobalStatusPublishing();

    [LoggerMessage(9417, LogLevel.Information, "Periodic global status publishing stopped")]
    private partial void LogPeriodicGlobalStatusPublishingStopped();
}
