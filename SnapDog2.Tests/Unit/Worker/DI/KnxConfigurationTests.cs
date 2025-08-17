namespace SnapDog2.Tests.Unit.Worker.DI;

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Extensions.DependencyInjection;

public class KnxConfigurationTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void AddKnxService_WhenEnabled_ShouldRegisterActualService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Add required dependencies
        var mockMediator = new Mock<Cortex.Mediator.IMediator>();
        services.AddSingleton(mockMediator.Object);

        var configuration = CreateTestConfiguration(enabled: true);

        // Act
        services.AddKnxService(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var knxService = serviceProvider.GetService<IKnxService>();

        // Assert
        knxService.Should().NotBeNull();
        knxService.GetType().Name.Should().Be("KnxService");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddKnxService_WithValidGroupAddresses_ShouldSucceed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = CreateTestConfiguration(enabled: true);

        // Act & Assert
        services.Invoking(s => s.AddKnxService(configuration)).Should().NotThrow();
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
                        Enabled = enabled,
                        Volume = "1/0/1",
                        VolumeStatus = "1/0/2",
                        Mute = "1/0/3",
                        MuteStatus = "1/0/4",
                        Play = "1/0/5",
                        Pause = "1/0/6",
                        Stop = "1/0/7",
                        TrackNext = "1/0/8",
                        TrackPrevious = "1/0/9",
                        TrackPlayingStatus = "1/0/10",
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
                        Enabled = enabled,
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

    private static SnapDogConfiguration CreateInvalidTestConfiguration()
    {
        return new SnapDogConfiguration
        {
            Services = new ServicesConfig
            {
                Knx = new KnxConfig
                {
                    Enabled = true,
                    Gateway = "192.168.1.10",
                    Port = -1, // Invalid port
                    Timeout = -5, // Invalid timeout
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
                        Volume = "invalid/address/format", // Invalid format
                        VolumeStatus = "1/0/2",
                        Mute = "1/0/3",
                        MuteStatus = "1/0/4",
                    },
                },
            },
            Clients = new List<ClientConfig>(),
        };
    }
}
