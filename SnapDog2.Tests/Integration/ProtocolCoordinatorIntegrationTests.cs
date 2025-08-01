using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Events;
using SnapDog2.Core.Models;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;
using SnapDog2.Infrastructure.Repositories;
using SnapDog2.Infrastructure.Services;
using Xunit;

namespace SnapDog2.Tests.Integration
{
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
            services.AddMediatR(static cfg => cfg.RegisterServicesFromAssemblyContaining<ProtocolCoordinator>());

            // Add logging
            services.AddLogging(static builder => builder.AddConsole());

            // Add configuration
            var config = new SnapDogConfiguration
            {
                Services = new ServicesConfiguration
                {
                    Snapcast = new SnapcastConfiguration { Enabled = true },
                    Mqtt = new ServicesMqttConfiguration { Enabled = true },
                    Knx = new KnxConfiguration { Enabled = true },
                    Subsonic = new SubsonicConfiguration { Enabled = true },
                },
                Clients = new List<ClientConfiguration>
                {
                    new()
                    {
                        Mac = "00:00:00:00:00:00",
                        Knx = new KnxClientConfiguration { Volume = KnxAddress.Parse("1/2/3") },
                    },
                },
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
            _mockMqttService
                .Setup(static x => x.SubscribeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockSnapcastService
                .Setup(static x => x.SynchronizeServerStateAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _protocolCoordinator.StartAsync(CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            _mockMqttService.Verify(
                static x => x.SubscribeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce
            );
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
                Id = clientId,
                Name = "Test Client",
                MacAddress = MacAddress.Parse("00:00:00:00:00:00"),
                IpAddress = IpAddress.Parse("127.0.0.1"),
                Status = ClientStatus.Connected,
                Volume = 50,
                ZoneId = "1",
            };

            var zone = new Zone { Id = "1", Name = "Test Zone" };

            _mockClientRepository
                .Setup(static x => x.GetByIdAsync(clientId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(client);
            _mockZoneRepository
                .Setup(static x => x.GetByIdAsync("1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(zone);
            _mockSnapcastService
                .Setup(static x => x.SetClientVolumeAsync(clientId, volume, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockKnxService
                .Setup(static x =>
                    x.SendVolumeCommandAsync(It.IsAny<KnxAddress>(), volume, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(true);

            await _protocolCoordinator.StartAsync(CancellationToken.None);

            // Act
            var result = await _protocolCoordinator.SynchronizeVolumeChangeAsync(
                clientId,
                volume,
                sourceProtocol,
                CancellationToken.None
            );

            // Assert
            Assert.True(result.IsSuccess);

            // Should sync to Snapcast (not source)
            _mockSnapcastService.Verify(
                static x => x.SetClientVolumeAsync(clientId, volume, It.IsAny<CancellationToken>()),
                Times.Once
            );

            // Should sync to KNX (not source)
            _mockKnxService.Verify(
                static x => x.SendVolumeCommandAsync(It.IsAny<KnxAddress>(), volume, It.IsAny<CancellationToken>()),
                Times.Once
            );

            // Should NOT sync to MQTT (is source)
            _mockMqttService.Verify(
                static x => x.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task SynchronizeZoneVolumeChangeAsync_ShouldSyncToConfiguredProtocols()
        {
            // Arrange
            const string zoneId = "1";
            const int volume = 80;
            const string sourceProtocol = "KNX";

            var zone = new Zone { Id = zoneId, Name = "Test Zone" };

            _mockZoneRepository
                .Setup(static x => x.GetByIdAsync(zoneId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(zone);
            _mockMqttService
                .Setup(static x =>
                    x.PublishAsync(
                        It.Is<string>(static s => s.Contains("volume")),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(true);

            await _protocolCoordinator.StartAsync(CancellationToken.None);

            // Act
            var result = await _protocolCoordinator.SynchronizeZoneVolumeChangeAsync(
                zoneId,
                volume,
                sourceProtocol,
                CancellationToken.None
            );

            // Assert
            Assert.True(result.IsSuccess);

            // Should sync to MQTT (not source)
            _mockMqttService.Verify(
                static x =>
                    x.PublishAsync(
                        It.Is<string>(static s => s.Contains("volume")),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );

            // Should NOT sync to KNX (is source)
            _mockKnxService.Verify(
                static x => x.SendVolumeCommandAsync(It.IsAny<KnxAddress>(), volume, It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task SynchronizePlaybackCommandAsync_ShouldBroadcastToRelevantProtocols()
        {
            // Arrange
            const string command = "PLAY";
            const string streamId = "test-stream";
            const string sourceProtocol = "Snapcast";

            _mockMqttService
                .Setup(static x =>
                    x.PublishAsync(
                        It.Is<string>(static s => s.Contains("status")),
                        "playing",
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(true);

            await _protocolCoordinator.StartAsync(CancellationToken.None);

            // Act
            var result = await _protocolCoordinator.SynchronizePlaybackCommandAsync(
                command,
                streamId,
                sourceProtocol,
                CancellationToken.None
            );

            // Assert
            Assert.True(result.IsSuccess);

            // Should publish to MQTT
            _mockMqttService.Verify(
                static x =>
                    x.PublishAsync(
                        It.Is<string>(static s => s.Contains("status")),
                        "playing",
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetProtocolHealthAsync_ShouldReturnHealthStatusForAllProtocols()
        {
            // Arrange
            _mockSnapcastService
                .Setup(static x => x.IsServerAvailableAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockSubsonicService
                .Setup(static x => x.IsServerAvailableAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var health = await _protocolCoordinator.GetProtocolHealthAsync(CancellationToken.None);

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
                Id = "test-client",
                Name = "Test Client",
                MacAddress = MacAddress.Parse("00:00:00:00:00:00"),
                IpAddress = IpAddress.Parse("127.0.0.1"),
                Status = ClientStatus.Connected,
                Volume = 50,
                ZoneId = "1",
            };

            var zone = new Zone { Id = "1", Name = "Test Zone" };

            _mockClientRepository
                .Setup(static x => x.GetByIdAsync("test-client", It.IsAny<CancellationToken>()))
                .ReturnsAsync(client);
            _mockZoneRepository
                .Setup(static x => x.GetByIdAsync("1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(zone);
            _mockMqttService
                .Setup(static x =>
                    x.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(true);
            _mockKnxService
                .Setup(static x => x.SendVolumeCommandAsync(It.IsAny<KnxAddress>(), 50, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            await _protocolCoordinator.StartAsync(CancellationToken.None);

            // Act
            await mediator.Publish(volumeEvent, CancellationToken.None);

            // Wait a bit for async processing
            await Task.Delay(100);

            // Assert
            _mockMqttService.Verify(
                static x => x.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
            _mockKnxService.Verify(
                static x => x.SendVolumeCommandAsync(It.IsAny<KnxAddress>(), 50, It.IsAny<CancellationToken>()),
                Times.Once
            );
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
                Id = clientId,
                Name = "Test Client",
                MacAddress = MacAddress.Parse("00:00:00:00:00:00"),
                IpAddress = IpAddress.Parse("127.0.0.1"),
                Status = ClientStatus.Connected,
                Volume = 50,
                ZoneId = "1",
            };

            var zone = new Zone { Id = "1", Name = "Test Zone" };

            _mockClientRepository
                .Setup(static x => x.GetByIdAsync(clientId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(client);
            _mockZoneRepository
                .Setup(static x => x.GetByIdAsync("1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(zone);
            _mockSnapcastService
                .Setup(static x => x.SetClientVolumeAsync(clientId, volume, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockKnxService
                .Setup(static x =>
                    x.SendVolumeCommandAsync(It.IsAny<KnxAddress>(), volume, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(false); // Simulate failure

            await _protocolCoordinator.StartAsync(CancellationToken.None);

            // Act
            var result = await _protocolCoordinator.SynchronizeVolumeChangeAsync(
                clientId,
                volume,
                sourceProtocol,
                CancellationToken.None
            );

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Partial sync failure", result.Error);
        }

        public void Dispose()
        {
            (_protocolCoordinator as IDisposable)?.Dispose();
            _serviceProvider?.Dispose();
        }
    }
}
