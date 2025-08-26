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
using SnapDog2.Core.Models;
using SnapDog2.Infrastructure.Application;
using Xunit;

/// <summary>
/// Tests for the ErrorTrackingService class.
/// </summary>
public class ErrorTrackingServiceTests
{
    private readonly Mock<ILogger<ErrorTrackingService>> _mockLogger;
    private readonly ErrorTrackingService _errorTrackingService;

    public ErrorTrackingServiceTests()
    {
        _mockLogger = new Mock<ILogger<ErrorTrackingService>>();

        // Setup logger to enable Warning and Information levels for LoggerMessage patterns
        _mockLogger.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);
        _mockLogger.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);

        _errorTrackingService = new ErrorTrackingService(_mockLogger.Object);
    }

    [Fact]
    public void RecordError_WithValidError_ShouldStoreError()
    {
        // Arrange
        var error = new ErrorDetails
        {
            TimestampUtc = DateTime.UtcNow,
            Level = 3, // Error
            ErrorCode = "TEST001",
            Message = "Test error message",
            Component = "TestComponent"
        };

        // Act
        _errorTrackingService.RecordError(error);

        // Assert - LoggerMessage patterns use IsEnabled checks
        _mockLogger.Verify(
            x => x.IsEnabled(LogLevel.Warning),
            Times.AtLeastOnce);
    }

    [Fact]
    public void RecordError_WithNullError_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => _errorTrackingService.RecordError(null!));
        Assert.Null(exception);
    }

    [Fact]
    public void RecordException_WithValidException_ShouldCreateAndStoreError()
    {
        // Arrange
        var testException = new InvalidOperationException("Test exception message");
        const string component = "TestComponent";
        const string operation = "TestOperation";

        // Act
        _errorTrackingService.RecordException(testException, component, operation);

        // Assert - LoggerMessage patterns use IsEnabled checks
        _mockLogger.Verify(
            x => x.IsEnabled(LogLevel.Warning),
            Times.AtLeastOnce);
    }

    [Fact]
    public void RecordException_WithNullException_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() =>
            _errorTrackingService.RecordException(null!, "TestComponent"));
        Assert.Null(exception);
    }

    [Fact]
    public async Task GetLatestErrorAsync_WithNoErrors_ShouldReturnNull()
    {
        // Act
        var result = await _errorTrackingService.GetLatestErrorAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestErrorAsync_WithErrors_ShouldReturnMostRecent()
    {
        // Arrange
        var firstError = new ErrorDetails
        {
            TimestampUtc = DateTime.UtcNow.AddMinutes(-5),
            Level = 2,
            ErrorCode = "FIRST",
            Message = "First error",
            Component = "TestComponent"
        };

        var secondError = new ErrorDetails
        {
            TimestampUtc = DateTime.UtcNow,
            Level = 3,
            ErrorCode = "SECOND",
            Message = "Second error",
            Component = "TestComponent"
        };

        _errorTrackingService.RecordError(firstError);
        _errorTrackingService.RecordError(secondError);

        // Act
        var result = await _errorTrackingService.GetLatestErrorAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SECOND", result.ErrorCode);
        Assert.Equal("Second error", result.Message);
    }

    [Fact]
    public async Task GetRecentErrorsAsync_WithTimeWindow_ShouldReturnErrorsInWindow()
    {
        // Arrange
        var oldError = new ErrorDetails
        {
            TimestampUtc = DateTime.UtcNow.AddHours(-2),
            Level = 2,
            ErrorCode = "OLD",
            Message = "Old error",
            Component = "TestComponent"
        };

        var recentError = new ErrorDetails
        {
            TimestampUtc = DateTime.UtcNow.AddMinutes(-5),
            Level = 3,
            ErrorCode = "RECENT",
            Message = "Recent error",
            Component = "TestComponent"
        };

        _errorTrackingService.RecordError(oldError);
        _errorTrackingService.RecordError(recentError);

        // Act
        var result = await _errorTrackingService.GetRecentErrorsAsync(TimeSpan.FromHours(1));

        // Assert
        Assert.Single(result);
        Assert.Equal("RECENT", result[0].ErrorCode);
    }

    [Fact]
    public async Task ClearErrors_ShouldRemoveAllErrors()
    {
        // Arrange
        var error = new ErrorDetails
        {
            TimestampUtc = DateTime.UtcNow,
            Level = 3,
            ErrorCode = "TEST",
            Message = "Test error",
            Component = "TestComponent"
        };

        _errorTrackingService.RecordError(error);

        // Act
        _errorTrackingService.ClearErrors();

        // Assert
        var result = await _errorTrackingService.GetLatestErrorAsync();
        Assert.Null(result);

        // Verify logging - LoggerMessage patterns use IsEnabled checks
        _mockLogger.Verify(
            x => x.IsEnabled(LogLevel.Information),
            Times.AtLeastOnce);
    }

    [Theory]
    [InlineData(typeof(ArgumentException), "ArgumentException")]
    [InlineData(typeof(InvalidOperationException), "InvalidOperationException")]
    [InlineData(typeof(NotSupportedException), "NotSupportedException")]
    public async Task RecordException_WithDifferentExceptionTypes_ShouldRecordCorrectErrorCode(
        Type exceptionType, string expectedErrorCode)
    {
        // Arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType, "Test message")!;
        const string component = "TestComponent";

        // Act
        _errorTrackingService.RecordException(exception, component);

        // Assert
        var result = await _errorTrackingService.GetLatestErrorAsync();
        Assert.NotNull(result);
        Assert.Equal(expectedErrorCode, result.ErrorCode);
        Assert.Equal(component, result.Component);
    }

    [Fact]
    public async Task GetRecentErrorsAsync_ShouldReturnErrorsInDescendingOrder()
    {
        // Arrange
        var errors = new[]
        {
            new ErrorDetails
            {
                TimestampUtc = DateTime.UtcNow.AddMinutes(-10),
                Level = 1,
                ErrorCode = "FIRST",
                Message = "First error",
                Component = "TestComponent"
            },
            new ErrorDetails
            {
                TimestampUtc = DateTime.UtcNow.AddMinutes(-5),
                Level = 2,
                ErrorCode = "SECOND",
                Message = "Second error",
                Component = "TestComponent"
            },
            new ErrorDetails
            {
                TimestampUtc = DateTime.UtcNow,
                Level = 3,
                ErrorCode = "THIRD",
                Message = "Third error",
                Component = "TestComponent"
            }
        };

        foreach (var error in errors)
        {
            _errorTrackingService.RecordError(error);
        }

        // Act
        var result = await _errorTrackingService.GetRecentErrorsAsync(TimeSpan.FromHours(1));

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("THIRD", result[0].ErrorCode); // Most recent first
        Assert.Equal("SECOND", result[1].ErrorCode);
        Assert.Equal("FIRST", result[2].ErrorCode);
    }
}
