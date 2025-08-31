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
using Microsoft.Extensions.Logging;
using Moq;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Domain.Services;
using SnapDog2.Infrastructure.Metrics;
using Xunit;

/// <summary>
/// Tests for the EnterpriseMetricsService class.
/// </summary>
public class MetricsServiceTests : IDisposable
{
    private readonly Mock<ILogger<EnterpriseMetricsService>> _mockLogger;
    private readonly Mock<IApplicationMetrics> _mockApplicationMetrics;
    private readonly EnterpriseMetricsService _metricsService;

    public MetricsServiceTests()
    {
        this._mockLogger = new Mock<ILogger<EnterpriseMetricsService>>();
        this._mockApplicationMetrics = new Mock<IApplicationMetrics>();

        // Setup logger to enable Debug and Information levels for LoggerMessage patterns
        this._mockLogger.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);
        this._mockLogger.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);

        this._metricsService = new EnterpriseMetricsService(this._mockLogger.Object, this._mockApplicationMetrics.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeSuccessfully()
    {
        // Act & Assert - Constructor should not throw
        using var service = new EnterpriseMetricsService(this._mockLogger.Object, this._mockApplicationMetrics.Object);

        // Verify initialization logging - LoggerMessage patterns use IsEnabled checks
        this._mockLogger.Verify(
            x => x.IsEnabled(LogLevel.Information),
            Times.AtLeastOnce);
    }

    [Fact]
    public void RecordCortexMediatorRequestDuration_Command_ShouldCallApplicationMetrics()
    {
        // Arrange
        const string requestType = "Command";
        const string requestName = "SetZoneVolumeCommand";
        const long durationMs = 45;
        const bool success = true;

        // Act
        this._metricsService.RecordCortexMediatorRequestDuration(requestType, requestName, durationMs, success);

        // Assert
        this._mockApplicationMetrics.Verify(
            x => x.RecordCommand(requestName, 0.045, success),
            Times.Once);
    }

    [Fact]
    public void RecordCortexMediatorRequestDuration_Query_ShouldCallApplicationMetrics()
    {
        // Arrange
        const string requestType = "Query";
        const string requestName = "GetZoneStatusQuery";
        const long durationMs = 12;
        const bool success = true;

        // Act
        this._metricsService.RecordCortexMediatorRequestDuration(requestType, requestName, durationMs, success);

        // Assert
        this._mockApplicationMetrics.Verify(
            x => x.RecordQuery(requestName, 0.012, success),
            Times.Once);
    }

    [Fact]
    public void IncrementCounter_ShouldLogCounterIncrement()
    {
        // Arrange
        const string name = "test_counter";
        const long delta = 5;
        var labels = new[] { ("component", "test"), ("operation", "increment") };

        // Act
        this._metricsService.IncrementCounter(name, delta, labels);

        // Assert - LoggerMessage patterns use IsEnabled checks
        this._mockLogger.Verify(
            x => x.IsEnabled(LogLevel.Debug),
            Times.AtLeastOnce);
    }

    [Fact]
    public void SetGauge_ShouldLogGaugeSet()
    {
        // Arrange
        const string name = "test_gauge";
        const double value = 42.5;
        var labels = new[] { ("component", "test") };

        // Act
        this._metricsService.SetGauge(name, value, labels);

        // Assert - LoggerMessage patterns use IsEnabled checks
        this._mockLogger.Verify(
            x => x.IsEnabled(LogLevel.Debug),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetServerStatsAsync_ShouldReturnValidStats()
    {
        // Act
        var stats = await this._metricsService.GetServerStatsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.TimestampUtc <= DateTime.UtcNow);
        Assert.True(stats.MemoryUsageMb >= 0);
        Assert.True(stats.TotalMemoryMb >= 0);
        Assert.True(stats.Uptime >= TimeSpan.Zero);
        Assert.True(stats.ActiveConnections >= 0);
        Assert.True(stats.ProcessedRequests >= 0);
    }

    [Fact]
    public void RecordHttpRequest_ShouldCallApplicationMetrics()
    {
        // Arrange
        const string method = "GET";
        const string endpoint = "/api/v1/zones";
        const int statusCode = 200;
        const double duration = 0.123;

        // Act
        this._metricsService.RecordHttpRequest(method, endpoint, statusCode, duration);

        // Assert
        this._mockApplicationMetrics.Verify(
            x => x.RecordHttpRequest(method, endpoint, statusCode, duration),
            Times.Once);
    }

    [Fact]
    public void RecordError_ShouldCallApplicationMetrics()
    {
        // Arrange
        const string errorType = "ValidationError";
        const string component = "ZoneService";
        const string operation = "SetVolume";

        // Act
        this._metricsService.RecordError(errorType, component, operation);

        // Assert
        this._mockApplicationMetrics.Verify(
            x => x.RecordError(errorType, component, operation),
            Times.Once);
    }

    [Fact]
    public void RecordException_ShouldCallApplicationMetrics()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        const string component = "TestComponent";
        const string operation = "TestOperation";

        // Act
        this._metricsService.RecordException(exception, component, operation);

        // Assert
        this._mockApplicationMetrics.Verify(
            x => x.RecordException(exception, component, operation),
            Times.Once);
    }

    [Fact]
    public void RecordTrackChange_ShouldCallApplicationMetrics()
    {
        // Arrange
        const string zoneIndex = "zone-1";
        const string fromTrack = "Track A";
        const string toTrack = "Track B";

        // Act
        this._metricsService.RecordTrackChange(zoneIndex, fromTrack, toTrack);

        // Assert
        this._mockApplicationMetrics.Verify(
            x => x.RecordTrackChange(zoneIndex, fromTrack, toTrack),
            Times.Once);
    }

    [Fact]
    public void RecordVolumeChange_ShouldCallApplicationMetrics()
    {
        // Arrange
        const string targetId = "zone-1";
        const string targetType = "zone";
        const int fromVolume = 50;
        const int toVolume = 75;

        // Act
        this._metricsService.RecordVolumeChange(targetId, targetType, fromVolume, toVolume);

        // Assert
        this._mockApplicationMetrics.Verify(
            x => x.RecordVolumeChange(targetId, targetType, fromVolume, toVolume),
            Times.Once);
    }

    [Theory]
    [InlineData("Command", "TestCommand", 100, true)]
    [InlineData("Query", "TestQuery", 50, false)]
    [InlineData("command", "LowercaseCommand", 200, true)] // Case insensitive
    [InlineData("QUERY", "UppercaseQuery", 75, false)] // Case insensitive
    public void RecordCortexMediatorRequestDuration_VariousInputs_ShouldHandleCorrectly(
        string requestType, string requestName, long durationMs, bool success)
    {
        // Act & Assert - Should not throw
        var exception = Record.Exception(() => this._metricsService.RecordCortexMediatorRequestDuration(requestType, requestName, durationMs, success));

        Assert.Null(exception);
    }

    public void Dispose()
    {
        this._metricsService?.Dispose();
    }
}
