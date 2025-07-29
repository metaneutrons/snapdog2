using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Events;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Infrastructure.Repositories;
using SnapDog2.Infrastructure.Services;
using MediatR;
using Xunit;

namespace SnapDog2.Tests.Integration;

/// <summary>
/// Integration tests for the ProtocolCoordinator service.
/// Tests protocol synchronization and coordination functionality.
/// </summary>
public class ProtocolCoordinatorIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<ISnapcastService> _mockSnapcastService;
    private readonly Mock<IMqttService> _mockMqttService;
    private readonly Mock<IKnxService> _mockKnxService;
    private readonly Mock<ISubsonicService> _mockSubsonicService;
    private readonly Mock<IClientRepository> _mockClientRepository;
    private readonly Mock<IZoneRepository> _mockZoneRepository;
    private readonly IProtocolCoordinator _protocolCoordinator;

    public ProtocolCoordinatorIntegrationTests()
    {
        // Create mocks
        _mockSnapcastService = new Mock<ISnapcastService>();
        _mockMqttService = new Mock<IMqttService>();
        _mockKnxService = new Mock<IKnxService>();
        _mockSubsonicService = new Mock<ISubsonicService>();
        _mockClientRepository = new Mock<IClientRepository>();
        _mockZoneRepository = new Mock<IZoneRepository>();

        // Setup service collection
        var services = new ServiceCollection();
        
        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ProtocolCoordinator).Assembly));
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add configuration
        var config = new SnapDogConfiguration
        {
            Snapcast = new SnapcastConfiguration { Enabled = true },
            Mqtt = new MqttConfiguration { Enabled = true },
            Knx = new KnxConfiguration { Enabled = true },
            Subsonic = new SubsonicConfiguration { Enabled = true }
        };
        services.AddSingleton(Options.Create(config));
        
        // Add mocked services
        services.AddSingleton(_mockSnapcastService.Object);
        services.AddSingleton(_mockMqttService.Object);
        services.AddSingleton(_mockKnxService.Object);
        services.AddSingleton(_mockSubsonicService.Object);
        services.AddSingleton(_mockClientRepository.Object);
        services.AddSingleton(_mockZoneRepository.Object);
        
        // Add protocol coordinator
        services.AddSingleton<IProtocolCoordinator, ProtocolCoordinator>();
        
        _serviceProvider = services.BuildServiceProvider();
        _protocolCoordinator = _serviceProvider.GetRequiredService<IProtocolCoordinator>();
    }

    [Fact]
    public async Task StartAsync_ShouldInitializeSuccessfully()
    {
        // Arrange
        _mockMqttService.Setup(x => x.SubscribeToCommandsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockSnapcastService.Setup(x => x.SynchronizeServerStateAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _protocolCoordinator.StartAsync();

        // Assert
        Assert.True(result.IsSuccess);
        _mockMqttService.Verify(x => x.SubscribeToCommandsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SynchronizeVolumeChangeAsync_ShouldSyncToAllProtocols_ExceptSource()
    {
        // Arrange
        const string clientId = "test-client";
        const int volume = 75;
        const string sourceProtocol = "MQTT";

        var client = new Client 
        { 
            Id = int.Parse(clientId),
            KnxVolumeGroupAddress = KnxAddress.Parse("1/2/3")
        };

        _mockClientRepository.Setup(x => x.GetByIdAsync(int.Parse(clientId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);
        _mockSnapcastService.Setup(x => x.SetClientVolumeAsync(clientId, volume, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockKnxService.Setup(x => x.SendVolumeCommandAsync(client.KnxVolumeGroupAddress, volume, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _protocolCoordinator.StartAsync();

        // Act
        var result = await _protocolCoordinator.SynchronizeVolumeChangeAsync(clientId, volume, sourceProtocol);

        // Assert
        Assert.True(result.IsSuccess);
        
        // Should sync to Snapcast (not source)
        _mockSnapcastService.Verify(x => x.SetClientVolumeAsync(clientId, volume, It.IsAny<CancellationToken>()), Times.Once);
        
        // Should sync to KNX (not source)
        _mockKnxService.Verify(x => x.SendVolumeCommandAsync(client.KnxVolumeGroupAddress, volume, It.IsAny<CancellationToken>()), Times.Once);
        
        // Should NOT sync to MQTT (is source)
        _mockMqttService.Verify(x => x.PublishClientVolumeAsync(clientId, volume, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SynchronizeZoneVolumeChangeAsync_ShouldSyncToConfiguredProtocols()
    {
        // Arrange
        const int zoneId = 1;
        const int volume = 80;
        const string sourceProtocol = "KNX";

        var zone = new Zone 
        { 
            Id = zoneId, 
            Name = "Test Zone",
            KnxVolumeGroupAddress = KnxAddress.Parse("2/3/4")
        };

        _mockZoneRepository.Setup(x => x.GetByIdAsync(zoneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);
        _mockMqttService.Setup(x => x.PublishZoneVolumeAsync(zoneId, volume, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _protocolCoordinator.StartAsync();

        // Act
        var result = await _protocolCoordinator.SynchronizeZoneVolumeChangeAsync(zoneId, volume, sourceProtocol);

        // Assert
        Assert.True(result.IsSuccess);
        
        // Should sync to MQTT (not source)
        _mockMqttService.Verify(x => x.PublishZoneVolumeAsync(zoneId, volume, It.IsAny<CancellationToken>()), Times.Once);
        
        // Should NOT sync to KNX (is source)
        _mockKnxService.Verify(x => x.SendVolumeCommandAsync(It.IsAny<KnxAddress>(), volume, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SynchronizePlaybackCommandAsync_ShouldBroadcastToRelevantProtocols()
    {
        // Arrange
        const string command = "PLAY";
        const int streamId = 1;
        const string sourceProtocol = "Snapcast";

        _mockMqttService.Setup(x => x.PublishStreamStatusAsync(streamId, "playing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _protocolCoordinator.StartAsync();

        // Act
        var result = await _protocolCoordinator.SynchronizePlaybackCommandAsync(command, streamId, sourceProtocol);

        // Assert
        Assert.True(result.IsSuccess);
        
        // Should publish to MQTT
        _mockMqttService.Verify(x => x.PublishStreamStatusAsync(streamId, "playing", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProtocolHealthAsync_ShouldReturnHealthStatusForAllProtocols()
    {
        // Arrange
        _mockSnapcastService.Setup(x => x.IsServerAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockSubsonicService.Setup(x => x.IsServerAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var health = await _protocolCoordinator.GetProtocolHealthAsync();

        // Assert
        Assert.NotEmpty(health);
        Assert.True(health.ContainsKey("Snapcast"));
        Assert.True(health.ContainsKey("Subsonic"));
        Assert.True(health["Snapcast"]);
        Assert.False(health["Subsonic"]);
    }

    [Fact]
    public async Task ProtocolCoordinator_ShouldHandleEventNotifications()
    {
        // Arrange
        var mediator = _serviceProvider.GetRequiredService<IMediator>();
        var volumeEvent = new SnapcastClientVolumeChangedEvent("test-client", 50, false);

        var client = new Client 
        { 
            Id = int.Parse("test-client"),
            KnxVolumeGroupAddress = KnxAddress.Parse("1/2/3")
        };

        _mockClientRepository.Setup(x => x.GetByIdAsync(int.Parse("test-client"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);
        _mockMqttService.Setup(x => x.PublishClientVolumeAsync("test-client", 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockKnxService.Setup(x => x.SendVolumeCommandAsync(client.KnxVolumeGroupAddress, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _protocolCoordinator.StartAsync();

        // Act
        await mediator.Publish(volumeEvent);

        // Wait a bit for async processing
        await Task.Delay(100);

        // Assert
        _mockMqttService.Verify(x => x.PublishClientVolumeAsync("test-client", 50, It.IsAny<CancellationToken>()), Times.Once);
        _mockKnxService.Verify(x => x.SendVolumeCommandAsync(client.KnxVolumeGroupAddress, 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SynchronizeVolumeChangeAsync_ShouldHandlePartialFailures()
    {
        // Arrange
        const string clientId = "test-client";
        const int volume = 75;
        const string sourceProtocol = "MQTT";

        var client = new Client 
        { 
            Id = int.Parse(clientId),
            KnxVolumeGroupAddress = KnxAddress.Parse("1/2/3")
        };

        _mockClientRepository.Setup(x => x.GetByIdAsync(int.Parse(clientId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);
        _mockSnapcastService.Setup(x => x.SetClientVolumeAsync(clientId, volume, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockKnxService.Setup(x => x.SendVolumeCommandAsync(client.KnxVolumeGroupAddress, volume, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Simulate failure

        await _protocolCoordinator.StartAsync();

        // Act
        var result = await _protocolCoordinator.SynchronizeVolumeChangeAsync(clientId, volume, sourceProtocol);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Partial sync failure", result.Error);
    }

    public void Dispose()
    {
        _protocolCoordinator?.Dispose();
        _serviceProvider?.Dispose();
    }
}