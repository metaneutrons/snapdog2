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
namespace SnapDog2.Tests.Middleware;

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SnapDog2.Api.Middleware;
using SnapDog2.Domain.Abstractions;
using Xunit;

/// <summary>
/// Tests for the HttpMetricsMiddleware class.
/// </summary>
public class HttpMetricsMiddlewareTests
{
    private readonly Mock<ILogger<HttpMetricsMiddleware>> _mockLogger;
    private readonly Mock<IApplicationMetrics> _mockMetricsService;
    private readonly HttpMetricsMiddleware _middleware;

    public HttpMetricsMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<HttpMetricsMiddleware>>();

        // Setup logger to enable Warning level for LoggerMessage patterns
        _mockLogger.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);

        // Create mock for IApplicationMetrics
        _mockMetricsService = new Mock<IApplicationMetrics>();

        // Create a simple next delegate that does nothing
        RequestDelegate next = (HttpContext context) => Task.CompletedTask;

        _middleware = new HttpMetricsMiddleware(next, _mockLogger.Object, _mockMetricsService.Object);
    }

    [Fact]
    public async Task InvokeAsync_WithSuccessfulRequest_ShouldRecordMetrics()
    {
        // Arrange
        var context = CreateHttpContext("GET", "/api/v1/zones");

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockMetricsService.Verify(
            x => x.RecordHttpRequest(
                "GET",
                "/api/v1/zones",
                200, // Default status code
                It.IsAny<double>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithException_ShouldRecordExceptionAndMetrics()
    {
        // Arrange
        var context = CreateHttpContext("POST", "/api/v1/zones");
        var testException = new InvalidOperationException("Test exception");

        // Create middleware with next delegate that throws
        RequestDelegate next = (HttpContext ctx) => throw testException;
        var middleware = new HttpMetricsMiddleware(next, _mockLogger.Object, _mockMetricsService.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context));

        Assert.Equal("Test exception", exception.Message);

        // Verify exception was recorded
        _mockMetricsService.Verify(
            x => x.RecordException(testException, "HttpPipeline", "POST /api/v1/zones"),
            Times.Once);
    }

    [Theory]
    [InlineData("/api/v1/zones/123", "/api/v1/zones/{index}")]
    [InlineData("/api/v1/clients/456/volume", "/api/v1/clients/{index}/volume")]
    [InlineData("/api/v1/zones/789/tracks/101", "/api/v1/zones/{index}/tracks/{index}")]
    [InlineData("/health", "/health")]
    [InlineData("/swagger/index.html", "/swagger")]
    [InlineData("/openapi/v1.json", "/swagger")]
    public async Task InvokeAsync_ShouldNormalizePathsCorrectly(string originalPath, string expectedNormalizedPath)
    {
        // Arrange
        var context = CreateHttpContext("GET", originalPath);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockMetricsService.Verify(
            x => x.RecordHttpRequest(
                "GET",
                expectedNormalizedPath,
                200,
                It.IsAny<double>()),
            Times.Once);
    }

    [Theory]
    [InlineData(400, "BadRequest")]
    [InlineData(401, "Unauthorized")]
    [InlineData(404, "NotFound")]
    [InlineData(500, "InternalServerError")]
    [InlineData(503, "ServiceUnavailable")]
    public async Task InvokeAsync_WithErrorStatusCodes_ShouldRecordErrors(int statusCode, string expectedErrorType)
    {
        // Arrange
        var context = CreateHttpContext("GET", "/api/v1/zones");
        context.Response.StatusCode = statusCode;

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockMetricsService.Verify(
            x => x.RecordError(expectedErrorType, "HttpPipeline", "GET /api/v1/zones"),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithSlowRequest_ShouldLogSlowRequest()
    {
        // Arrange
        var context = CreateHttpContext("GET", "/api/v1/zones");

        // Create middleware with slow next delegate
        RequestDelegate slowNext = async (HttpContext ctx) =>
        {
            await Task.Delay(1100); // Simulate slow request (> 1 second)
        };
        var middleware = new HttpMetricsMiddleware(slowNext, _mockLogger.Object, _mockMetricsService.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - LoggerMessage patterns use IsEnabled checks
        _mockLogger.Verify(
            x => x.IsEnabled(LogLevel.Warning),
            Times.AtLeastOnce);
    }

    private static HttpContext CreateHttpContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.StatusCode = 200; // Default success status
        return context;
    }
}
