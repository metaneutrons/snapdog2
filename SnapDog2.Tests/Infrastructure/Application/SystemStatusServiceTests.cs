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
namespace SnapDog2.Tests.Infrastructure.Application;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Domain.Services;
using SnapDog2.Shared.Models;
using Xunit;

/// <summary>
/// Tests for the AppStatusService class.
/// </summary>
public class SystemStatusServiceTests
{
    private readonly Mock<ILogger<AppStatusService>> _mockLogger;
    private readonly Mock<IMetricsService> _mockMetricsService;
    private readonly Mock<IAppHealthCheckService> _mockHealthCheckService;
    private readonly AppStatusService _statusService;

    public SystemStatusServiceTests()
    {
        this._mockLogger = new Mock<ILogger<AppStatusService>>();
        this._mockMetricsService = new Mock<IMetricsService>();
        this._mockHealthCheckService = new Mock<IAppHealthCheckService>();

        // Setup logger to enable Debug level for LoggerMessage patterns
        this._mockLogger.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);

        this._statusService = new AppStatusService(this._mockLogger.Object, this._mockMetricsService.Object, this._mockHealthCheckService.Object);
    }

    [Fact]
    public async Task GetCurrentStatusAsync_WithHealthySystem_ShouldReturnOnlineStatus()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            HealthStatus.Healthy,
            TimeSpan.FromMilliseconds(100));

        this._mockHealthCheckService
            .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await this._statusService.GetCurrentStatusAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsOnline);
        Assert.True(result.TimestampUtc <= DateTime.UtcNow);
        Assert.True(result.TimestampUtc >= DateTime.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public async Task GetCurrentStatusAsync_WithUnhealthySystem_ShouldReturnOfflineStatus()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            HealthStatus.Unhealthy,
            TimeSpan.FromMilliseconds(100));

        this._mockHealthCheckService
            .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await this._statusService.GetCurrentStatusAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsOnline);
    }

    [Fact]
    public async Task GetVersionInfoAsync_ShouldReturnValidVersionDetails()
    {
        // Act
        var result = await this._statusService.GetVersionInfoAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Version);
        Assert.True(result.Major >= 0);
        Assert.True(result.Minor >= 0);
        Assert.True(result.Patch >= 0);
        Assert.True(result.TimestampUtc <= DateTime.UtcNow);
        Assert.NotNull(result.GitCommit);
        Assert.NotNull(result.GitBranch);
        Assert.NotNull(result.BuildConfiguration);

        // Build configuration should be Debug or Release
        Assert.True(result.BuildConfiguration == "Debug" ||
                   result.BuildConfiguration == "Release" ||
                   result.BuildConfiguration == "Unknown");
    }

    [Fact]
    public async Task GetServerStatsAsync_ShouldDelegateToMetricsService()
    {
        // Arrange
        var expectedStats = new ServerStats
        {
            TimestampUtc = DateTime.UtcNow,
            CpuUsagePercent = 25.5,
            MemoryUsageMb = 512.0,
            TotalMemoryMb = 1024.0,
            Uptime = TimeSpan.FromHours(2),
            ActiveConnections = 10,
            ProcessedRequests = 1000
        };

        this._mockMetricsService
            .Setup(x => x.GetServerStatsAsync())
            .ReturnsAsync(expectedStats);

        // Act
        var result = await this._statusService.GetServerStatsAsync();

        // Assert
        Assert.Equal(expectedStats, result);

        // Verify metrics service was called
        this._mockMetricsService.Verify(
            x => x.GetServerStatsAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetCurrentStatusAsync_ShouldLogGettingSystemStatus()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            HealthStatus.Healthy,
            TimeSpan.FromMilliseconds(100));

        this._mockHealthCheckService
            .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        await this._statusService.GetCurrentStatusAsync();

        // Assert - LoggerMessage patterns use IsEnabled checks
        this._mockLogger.Verify(
            x => x.IsEnabled(LogLevel.Debug),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetVersionInfoAsync_ShouldLogGettingVersionInfo()
    {
        // Act
        await this._statusService.GetVersionInfoAsync();

        // Assert - LoggerMessage patterns use IsEnabled checks
        this._mockLogger.Verify(
            x => x.IsEnabled(LogLevel.Debug),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetServerStatsAsync_ShouldLogGettingServerStats()
    {
        // Arrange
        this._mockMetricsService
            .Setup(x => x.GetServerStatsAsync())
            .ReturnsAsync(new ServerStats
            {
                TimestampUtc = DateTime.UtcNow,
                CpuUsagePercent = 25.5,
                MemoryUsageMb = 512.0,
                TotalMemoryMb = 2048.0,
                Uptime = TimeSpan.FromHours(2)
            });

        // Act
        await this._statusService.GetServerStatsAsync();

        // Assert - Verify LoggerMessage pattern (IsEnabled check)
        this._mockLogger.Verify(
            x => x.IsEnabled(LogLevel.Debug),
            Times.AtLeastOnce);
    }
}
