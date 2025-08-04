namespace SnapDog2.Tests.Unit.Infrastructure.Services;

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Enums;
using SnapDog2.Infrastructure.Services;

public class KnxServiceTests
{
    private readonly Mock<ILogger<KnxService>> _mockLogger;

    public KnxServiceTests()
    {
        _mockLogger = new Mock<ILogger<KnxService>>();
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        var config = CreateTestConfiguration(enabled: true);
        var options = Options.Create(config);

        // Act
        var mockServiceProvider = new Mock<IServiceProvider>();
        var service = new KnxService(options, mockServiceProvider.Object, _mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
        service.IsConnected.Should().BeFalse();
        service.Status.Should().Be(ServiceStatus.Stopped);
    }

    [Fact]
    public async Task InitializeAsync_WhenDisabled_ShouldReturnSuccess()
    {
        // Arrange
        var config = CreateTestConfiguration(enabled: false);
        var options = Options.Create(config);
        var mockServiceProvider = new Mock<IServiceProvider>();
        var service = new KnxService(options, mockServiceProvider.Object, _mockLogger.Object);

        // Act
        var result = await service.InitializeAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        service.Status.Should().Be(ServiceStatus.Stopped);
    }

    [Fact]
    public async Task SendStatusAsync_WhenNotConnected_ShouldReturnFailure()
    {
        // Arrange
        var config = CreateTestConfiguration(enabled: true);
        var options = Options.Create(config);
        var mockServiceProvider = new Mock<IServiceProvider>();
        var service = new KnxService(options, mockServiceProvider.Object, _mockLogger.Object);

        // Act
        var result = await service.SendStatusAsync("VOLUME", 1, 50);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not connected");
    }

    [Fact]
    public async Task WriteGroupValueAsync_WhenNotConnected_ShouldReturnFailure()
    {
        // Arrange
        var config = CreateTestConfiguration(enabled: true);
        var options = Options.Create(config);
        var mockServiceProvider = new Mock<IServiceProvider>();
        var service = new KnxService(options, mockServiceProvider.Object, _mockLogger.Object);

        // Act
        var result = await service.WriteGroupValueAsync("1/0/1", true);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not connected");
    }

    [Fact]
    public async Task ReadGroupValueAsync_WhenNotConnected_ShouldReturnFailure()
    {
        // Arrange
        var config = CreateTestConfiguration(enabled: true);
        var options = Options.Create(config);
        var mockServiceProvider = new Mock<IServiceProvider>();
        var service = new KnxService(options, mockServiceProvider.Object, _mockLogger.Object);

        // Act
        var result = await service.ReadGroupValueAsync("1/0/1");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not connected");
    }

    [Fact]
    public async Task StopAsync_ShouldReturnSuccess()
    {
        // Arrange
        var config = CreateTestConfiguration(enabled: true);
        var options = Options.Create(config);
        var mockServiceProvider = new Mock<IServiceProvider>();
        var service = new KnxService(options, mockServiceProvider.Object, _mockLogger.Object);

        // Act
        var result = await service.StopAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        service.Status.Should().Be(ServiceStatus.Stopped);
    }

    [Fact]
    public async Task DisposeAsync_ShouldCompleteWithoutError()
    {
        // Arrange
        var config = CreateTestConfiguration(enabled: true);
        var options = Options.Create(config);
        var mockServiceProvider = new Mock<IServiceProvider>();
        var service = new KnxService(options, mockServiceProvider.Object, _mockLogger.Object);

        // Act & Assert
        await service.Invoking(s => s.DisposeAsync().AsTask()).Should().NotThrowAsync();
    }

    private static SnapDogConfiguration CreateTestConfiguration(bool enabled)
    {
        return new SnapDogConfiguration
        {
            Services = new ServicesConfig
            {
                Knx = new KnxConfig
                {
                    Enabled = enabled,
                    Gateway = "192.168.1.10",
                    Port = 3671,
                    Timeout = 10,
                    AutoReconnect = true,
                },
            },
            Zones = new List<ZoneConfig>
            {
                new ZoneConfig
                {
                    Name = "Living Room",
                    Sink = "living-room",
                    Knx = new ZoneKnxConfig
                    {
                        Enabled = true,
                        Volume = "1/0/1",
                        VolumeStatus = "1/0/2",
                        Mute = "1/0/3",
                        MuteStatus = "1/0/4",
                        Play = "1/0/5",
                        Pause = "1/0/6",
                        Stop = "1/0/7",
                        TrackNext = "1/0/8",
                        TrackPrevious = "1/0/9",
                        ControlStatus = "1/0/10",
                    },
                },
            },
            Clients = new List<ClientConfig>
            {
                new ClientConfig
                {
                    Name = "Living Room Client",
                    DefaultZone = 1,
                    Knx = new ClientKnxConfig
                    {
                        Enabled = true,
                        Volume = "2/0/1",
                        VolumeStatus = "2/0/2",
                        Mute = "2/0/3",
                        MuteStatus = "2/0/4",
                        ConnectedStatus = "2/0/5",
                    },
                },
            },
        };
    }
}
