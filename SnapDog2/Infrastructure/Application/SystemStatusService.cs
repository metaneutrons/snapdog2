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
namespace SnapDog2.Infrastructure.Application;

using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Helpers;

/// <summary>
/// Implementation of system status service.
/// TODO: This is a placeholder implementation - will be enhanced with real metrics.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AppStatusService"/> class.
/// </remarks>
/// <param name="logger">The logger instance.</param>
public partial class AppStatusService(ILogger<AppStatusService> logger) : IAppStatusService
{
    private readonly ILogger<AppStatusService> _logger = logger;
    private static readonly DateTime _startTime = DateTime.UtcNow;

    /// <inheritdoc/>
    public Task<SystemStatus> GetCurrentStatusAsync()
    {
        this.LogGettingSystemStatus();

        var status = new SystemStatus
        {
            IsOnline = true, // TODO: Implement real health checks
            TimestampUtc = DateTime.UtcNow,
        };

        return Task.FromResult(status);
    }

    /// <inheritdoc/>
    public Task<VersionDetails> GetVersionInfoAsync()
    {
        this.LogGettingVersionInfo();

        var gitVersionInfo = GitVersionHelper.GetVersionInfo();
        var assembly = Assembly.GetExecutingAssembly();

        var versionDetails = new VersionDetails
        {
            Version = gitVersionInfo.FullSemVer,
            Major = gitVersionInfo.Major,
            Minor = gitVersionInfo.Minor,
            Patch = gitVersionInfo.Patch,
            TimestampUtc = DateTime.UtcNow,
            BuildDateUtc = GetBuildDate(assembly),
            GitCommit = gitVersionInfo.Sha,
            GitBranch = gitVersionInfo.BranchName,
            BuildConfiguration = GetBuildConfiguration(),
        };

        return Task.FromResult(versionDetails);
    }

    /// <inheritdoc/>
    public Task<ServerStats> GetServerStatsAsync()
    {
        this.LogGettingServerStats();

        // TODO: Implement real performance metrics
        var process = Process.GetCurrentProcess();
        var uptime = DateTime.UtcNow - _startTime;

        var stats = new ServerStats
        {
            TimestampUtc = DateTime.UtcNow,
            CpuUsagePercent = 0.0, // TODO: Implement CPU monitoring
            MemoryUsageMb = process.WorkingSet64 / (1024.0 * 1024.0),
            TotalMemoryMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0), // TODO: Get system memory
            Uptime = uptime,
            ActiveConnections = 0, // TODO: Implement connection tracking
            ProcessedRequests = 0, // TODO: Implement request counting
        };

        return Task.FromResult(stats);
    }

    private static DateTime? GetBuildDate(Assembly assembly)
    {
        // TODO: Implement build date extraction from assembly attributes
        return null;
    }

    private static string GetBuildConfiguration()
    {
#if DEBUG
        return "Debug";
#elif RELEASE
        return "Release";
#else
        return "Unknown";
#endif
    }

    [LoggerMessage(5001, LogLevel.Debug, "Getting system status")]
    private partial void LogGettingSystemStatus();

    [LoggerMessage(5002, LogLevel.Debug, "Getting version information")]
    private partial void LogGettingVersionInfo();

    [LoggerMessage(5003, LogLevel.Debug, "Getting server statistics")]
    private partial void LogGettingServerStats();
}
