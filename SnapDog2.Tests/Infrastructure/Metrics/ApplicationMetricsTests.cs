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
namespace SnapDog2.Tests.Infrastructure.Metrics;

using System;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Moq;
using SnapDog2.Infrastructure.Metrics;
using Xunit;

/// <summary>
/// Tests for the ApplicationMetrics class.
/// </summary>
public class ApplicationMetricsTests : IDisposable
{
    private readonly Mock<ILogger<ApplicationMetrics>> _mockLogger;
    private readonly ApplicationMetrics _applicationMetrics;

    public ApplicationMetricsTests()
    {
        this._mockLogger = new Mock<ILogger<ApplicationMetrics>>();
        this._applicationMetrics = new ApplicationMetrics(this._mockLogger.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeSuccessfully()
    {
        // Act & Assert - Constructor should not throw
        using var metrics = new ApplicationMetrics(this._mockLogger.Object);

        // Verify logger IsEnabled was called (LoggerMessage pattern)
        this._mockLogger.Verify(
            x => x.IsEnabled(LogLevel.Information),
            Times.AtLeastOnce);
    }

    [Fact]
    public void RecordHttpRequest_ShouldNotThrow()
    {
        // Arrange
        const string method = "GET";
        const string endpoint = "/api/v1/zones";
        const int statusCode = 200;
        const double duration = 0.123;

        // Act & Assert
        var exception = Record.Exception(() => this._applicationMetrics.RecordHttpRequest(method, endpoint, statusCode, duration));

        Assert.Null(exception);
    }

    [Fact]
    public void RecordCommand_ShouldNotThrow()
    {
        // Arrange
        const string commandName = "SetZoneVolumeCommand";
        const double duration = 0.045;
        const bool success = true;

        // Act & Assert
        var exception = Record.Exception(() => this._applicationMetrics.RecordCommand(commandName, duration, success));

        Assert.Null(exception);
    }

    [Fact]
    public void RecordQuery_ShouldNotThrow()
    {
        // Arrange
        const string queryName = "GetZoneStatusQuery";
        const double duration = 0.012;
        const bool success = true;

        // Act & Assert
        var exception = Record.Exception(() => this._applicationMetrics.RecordQuery(queryName, duration, success));

        Assert.Null(exception);
    }

    [Fact]
    public void UpdateSystemMetrics_ShouldNotThrow()
    {
        // Arrange
        var systemState = new SystemMetricsState
        {
            CpuUsagePercent = 25.5,
            MemoryUsageMb = 512.0,
            MemoryUsagePercent = 60.0,
            ActiveConnections = 10,
            ThreadPoolThreads = 8
        };

        // Act & Assert
        var exception = Record.Exception(() => this._applicationMetrics.UpdateSystemMetrics(systemState));

        Assert.Null(exception);
    }

    [Fact]
    public void UpdateBusinessMetrics_ShouldNotThrow()
    {
        // Arrange
        var businessState = new BusinessMetricsState
        {
            ZonesTotal = 5,
            ZonesActive = 3,
            ClientsConnected = 8,
            TracksPlaying = 2
        };

        // Act & Assert
        var exception = Record.Exception(() => this._applicationMetrics.UpdateBusinessMetrics(businessState));

        Assert.Null(exception);
    }

    [Fact]
    public void RecordTrackChange_ShouldNotThrow()
    {
        // Arrange
        const string zoneIndex = "zone-1";
        const string fromTrack = "Track A";
        const string toTrack = "Track B";

        // Act & Assert
        var exception = Record.Exception(() => this._applicationMetrics.RecordTrackChange(zoneIndex, fromTrack, toTrack));

        Assert.Null(exception);
    }

    [Fact]
    public void RecordVolumeChange_ShouldNotThrow()
    {
        // Arrange
        const string targetId = "zone-1";
        const string targetType = "zone";
        const int fromVolume = 50;
        const int toVolume = 75;

        // Act & Assert
        var exception = Record.Exception(() => this._applicationMetrics.RecordVolumeChange(targetId, targetType, fromVolume, toVolume));

        Assert.Null(exception);
    }

    [Fact]
    public void RecordError_ShouldNotThrow()
    {
        // Arrange
        const string errorType = "ValidationError";
        const string component = "ZoneService";
        const string operation = "SetVolume";

        // Act & Assert
        var exception = Record.Exception(() => this._applicationMetrics.RecordError(errorType, component, operation));

        Assert.Null(exception);
    }

    [Fact]
    public void RecordException_ShouldNotThrow()
    {
        // Arrange
        var testException = new InvalidOperationException("Test exception");
        const string component = "TestComponent";
        const string operation = "TestOperation";

        // Act & Assert
        var exception = Record.Exception(() => this._applicationMetrics.RecordException(testException, component, operation));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("GET", "/api/v1/zones", 200, 0.1)]
    [InlineData("POST", "/api/v1/zones/1/volume", 400, 0.05)]
    [InlineData("PUT", "/api/v1/clients/1", 500, 1.2)]
    public void RecordHttpRequest_WithVariousInputs_ShouldNotThrow(
        string method, string endpoint, int statusCode, double duration)
    {
        // Act & Assert
        var exception = Record.Exception(() => this._applicationMetrics.RecordHttpRequest(method, endpoint, statusCode, duration));

        Assert.Null(exception);
    }

    public void Dispose()
    {
        this._applicationMetrics?.Dispose();
    }
}
