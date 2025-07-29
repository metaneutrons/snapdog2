using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.Services;
using Xunit;

namespace SnapDog2.Tests.Integration.Snapcast;

[Trait("Category", "Integration")]
public class SnapcastServiceIntegrationTests
{
    [Fact]
    public async Task GetServerStatusAsync_WithValidData_ShouldSucceed()
    {
        // Arrange
        var mockService = new Mock<ISnapcastService>();
        var expectedStatus = """{"server": {"host": "localhost", "snapserver": {"version": "0.26.0"}}}""";
        mockService.Setup(s => s.GetServerStatusAsync(default)).ReturnsAsync(expectedStatus);

        var logger = new Mock<ILogger<SnapcastService>>().Object;
        var config = new SnapcastConfiguration();
        var options = Options.Create(config);
        var service = new SnapcastService(options, logger);

        // Act
        var result = await mockService.Object.GetServerStatusAsync();

        // Assert
        Assert.Equal(expectedStatus, result);
    }
}
