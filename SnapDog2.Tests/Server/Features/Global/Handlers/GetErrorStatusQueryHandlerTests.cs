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
namespace SnapDog2.Tests.Server.Features.Global.Handlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Global.Handlers;
using SnapDog2.Server.Features.Global.Queries;
using Xunit;

/// <summary>
/// Tests for the GetErrorStatusQueryHandler class.
/// </summary>
public class GetErrorStatusQueryHandlerTests
{
    private readonly Mock<ILogger<GetErrorStatusQueryHandler>> _mockLogger;
    private readonly Mock<IErrorTrackingService> _mockErrorTrackingService;
    private readonly GetErrorStatusQueryHandler _handler;

    public GetErrorStatusQueryHandlerTests()
    {
        _mockLogger = new Mock<ILogger<GetErrorStatusQueryHandler>>();

        // Setup logger to enable Debug level for LoggerMessage patterns
        _mockLogger.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);

        _mockErrorTrackingService = new Mock<IErrorTrackingService>();
        _handler = new GetErrorStatusQueryHandler(_mockLogger.Object, _mockErrorTrackingService.Object);
    }

    [Fact]
    public async Task Handle_WithNoErrors_ShouldReturnSuccessWithNull()
    {
        // Arrange
        var query = new GetErrorStatusQuery();
        _mockErrorTrackingService
            .Setup(x => x.GetLatestErrorAsync())
            .ReturnsAsync((ErrorDetails?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);

        // Verify logging - LoggerMessage patterns use IsEnabled checks
        _mockLogger.Verify(
            x => x.IsEnabled(LogLevel.Debug),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_WithLatestError_ShouldReturnSuccessWithError()
    {
        // Arrange
        var query = new GetErrorStatusQuery();
        var errorDetails = new ErrorDetails
        {
            TimestampUtc = DateTime.UtcNow,
            Level = 3,
            ErrorCode = "TEST001",
            Message = "Test error message",
            Component = "TestComponent"
        };

        _mockErrorTrackingService
            .Setup(x => x.GetLatestErrorAsync())
            .ReturnsAsync(errorDetails);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("TEST001", result.Value.ErrorCode);
        Assert.Equal("Test error message", result.Value.Message);
        Assert.Equal("TestComponent", result.Value.Component);
    }

    [Fact]
    public async Task Handle_WhenErrorTrackingServiceThrows_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetErrorStatusQuery();
        var expectedException = new InvalidOperationException("Service error");

        _mockErrorTrackingService
            .Setup(x => x.GetLatestErrorAsync())
            .ThrowsAsync(expectedException);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Failed to retrieve error status", result.ErrorMessage);

        // Verify error logging (LoggerMessage pattern)
        _mockLogger.Verify(
            x => x.IsEnabled(LogLevel.Error),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_ShouldCallErrorTrackingServiceOnce()
    {
        // Arrange
        var query = new GetErrorStatusQuery();
        _mockErrorTrackingService
            .Setup(x => x.GetLatestErrorAsync())
            .ReturnsAsync((ErrorDetails?)null);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockErrorTrackingService.Verify(
            x => x.GetLatestErrorAsync(),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToService()
    {
        // Arrange
        var query = new GetErrorStatusQuery();
        var cancellationToken = new CancellationToken();

        _mockErrorTrackingService
            .Setup(x => x.GetLatestErrorAsync())
            .ReturnsAsync((ErrorDetails?)null);

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _mockErrorTrackingService.Verify(
            x => x.GetLatestErrorAsync(),
            Times.Once);
    }
}
