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
namespace SnapDog2.Domain.Services;

using System.Reflection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Helpers;
using SnapDog2.Shared.Models;

/// <summary>
/// Implementation of system status service with real metrics integration.
/// Provides comprehensive system health, version, and performance information.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AppStatusService"/> class.
/// </remarks>
/// <param name="logger">The logger instance.</param>
/// <param name="metricsService">The metrics service for real performance data.</param>
/// <param name="healthCheckService">The health check service for system health.</param>
public partial class AppStatusService(
    ILogger<AppStatusService> logger,
    IMetricsService metricsService,
    IAppHealthCheckService healthCheckService
) : IAppStatusService
{
    private readonly ILogger<AppStatusService> _logger = logger;
    private readonly IMetricsService _metricsService = metricsService;
    private readonly IAppHealthCheckService _healthCheckService = healthCheckService;

    /// <inheritdoc/>
    public async Task<SystemStatus> GetCurrentStatusAsync()
    {
        this.LogGettingSystemStatus();

        // Get real health check results
        var healthReport = await this._healthCheckService.CheckHealthAsync();
        var isOnline = healthReport.Status == HealthStatus.Healthy;

        var status = new SystemStatus
        {
            IsOnline = isOnline,
            TimestampUtc = DateTime.UtcNow,
        };

        return status;
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
            BuildDateUtc = this.GetBuildDate(assembly),
            GitCommit = gitVersionInfo.Sha,
            GitBranch = gitVersionInfo.BranchName,
            BuildConfiguration = GetBuildConfiguration(),
        };

        return Task.FromResult(versionDetails);
    }

    /// <inheritdoc/>
    public async Task<ServerStats> GetServerStatsAsync()
    {
        this.LogGettingServerStats();

        // Use the real metrics service to get accurate server statistics
        var stats = await this._metricsService.GetServerStatsAsync();

        return stats;
    }

    private DateTime? GetBuildDate(Assembly assembly)
    {
        try
        {
            // Extract build date from assembly metadata
            var buildDateAttribute = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attr => attr.Key == "BuildDate");

            if (buildDateAttribute?.Value != null &&
                DateTime.TryParse(buildDateAttribute.Value, out var buildDate))
            {
                return buildDate;
            }

            // Fallback: use file creation time
            var assemblyLocation = assembly.Location;
            if (!string.IsNullOrEmpty(assemblyLocation) && File.Exists(assemblyLocation))
            {
                return File.GetCreationTimeUtc(assemblyLocation);
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - build date is not critical
            _logger.LogInformation("BuildDateExtractionFailed: {Details}", ex);
        }

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

    [LoggerMessage(EventId = 110400, Level = LogLevel.Debug, Message = "Getting system status"
)]
    private partial void LogGettingSystemStatus();

    [LoggerMessage(EventId = 110401, Level = LogLevel.Debug, Message = "Getting version information"
)]
    private partial void LogGettingVersionInfo();

    [LoggerMessage(EventId = 110402, Level = LogLevel.Debug, Message = "Getting server statistics"
)]
    private partial void LogGettingServerStats();

}
