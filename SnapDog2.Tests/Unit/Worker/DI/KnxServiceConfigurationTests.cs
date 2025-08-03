namespace SnapDog2.Tests.Unit.Worker.DI;

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Worker.DI;

public class KnxServiceConfigurationTests
{
    [Fact]
    public void AddKnxService_WhenDisabled_ShouldRegisterNoOpService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = CreateTestConfiguration(enabled: false);

        // Act
        services.AddKnxService(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var knxService = serviceProvider.GetRequiredService<IKnxService>();

        // Assert
        knxService.Should().NotBeNull();
        knxService.IsConnected.Should().BeFalse();
        knxService.Status.Should().Be(SnapDog2.Core.Models.ServiceStatus.Disabled);
    }

    [Fact]
    public void AddKnxService_WhenEnabled_ShouldRegisterActualService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

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
    public void AddKnxService_WithInvalidConfiguration_ShouldRegisterNoOpService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = CreateInvalidTestConfiguration();

        // Act
        services.AddKnxService(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var knxService = serviceProvider.GetRequiredService<IKnxService>();

        // Assert
        knxService.Should().NotBeNull();
        knxService.Status.Should().Be(SnapDog2.Core.Models.ServiceStatus.Disabled);
    }

    [Fact]
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
                    Id = 1,
                    Name = "Living Room",
                    Sink = "living-room",
                    Knx = new ZoneKnxConfig
                    {
                        Enabled = enabled,
                        VolumeSetAddress = "1/0/1",
                        VolumeStatusAddress = "1/0/2",
                        MuteSetAddress = "1/0/3",
                        MuteStatusAddress = "1/0/4",
                        PlayAddress = "1/0/5",
                        PauseAddress = "1/0/6",
                        StopAddress = "1/0/7",
                        NextTrackAddress = "1/0/8",
                        PrevTrackAddress = "1/0/9",
                        PlayingStatusAddress = "1/0/10",
                    },
                },
            },
            Clients = new List<ClientConfig>
            {
                new ClientConfig
                {
                    Id = 1,
                    Name = "Living Room Client",
                    DefaultZone = 1,
                    Knx = new ClientKnxConfig
                    {
                        Enabled = enabled,
                        VolumeSetAddress = "2/0/1",
                        VolumeStatusAddress = "2/0/2",
                        MuteSetAddress = "2/0/3",
                        MuteStatusAddress = "2/0/4",
                        ConnectedStatusAddress = "2/0/5",
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
                    Id = 1,
                    Name = "Living Room",
                    Sink = "living-room",
                    Knx = new ZoneKnxConfig
                    {
                        Enabled = true,
                        VolumeSetAddress = "invalid/address/format", // Invalid format
                        VolumeStatusAddress = "1/0/2",
                        MuteSetAddress = "1/0/3",
                        MuteStatusAddress = "1/0/4",
                    },
                },
            },
            Clients = new List<ClientConfig>(),
        };
    }
}
