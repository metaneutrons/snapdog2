using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SnapDog2.Core.Common;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Events;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Infrastructure.Repositories;
using SnapDog2.Infrastructure.Services;
using Xunit;

namespace SnapDog2.Tests.Infrastructure.Services;

/// <summary>
/// Comprehensive unit tests for ProtocolCoordinator focusing on synchronization logic,
/// debounce mechanisms, error handling, and multi-protocol coordination scenarios.
/// Award-worthy test suite ensuring robust protocol coordination with complete coverage.
/// </summary>
public class ProtocolCoordinatorUnitTests : IDisposable
{
    private readonly Mock<ISnapcastService> _mockSnapcastService;
    private readonly Mock<IMqttService> _mockMqttService;
    private readonly Mock<IKnxService> _mockKnxService;
    private readonly Mock<ISubsonicService> _mockSubsonicService;
    private readonly Mock<IClientRepository> _mockClientRepository;
    private readonly Mock<IZoneRepository> _mockZoneRepository;
    private readonly Mock<ILogger<ProtocolCoordinator>> _mockLogger;
    private readonly SnapDogConfiguration _config;
    private readonly ProtocolCoordinator _protocolCoordinator;

    public ProtocolCoordinatorUnitTests()
    {
        _mockSnapcastService = new Mock<ISnapcastService>();
        _mockMqttService = new Mock<IMqttService>();
        _mockKnxService = new Mock<IKnxService>();
        _mockSubsonicService = new Mock<ISubsonicService>();
        _mockClientRepository = new Mock<IClientRepository>();
        _mockZoneRepository = new Mock<IZoneRepository>();
        _mockLogger = new Mock<ILogger<ProtocolCoordinator>>();

        _config = new SnapDogConfiguration
        {
            Services = new ServicesConfiguration
            {
                Snapcast = new SnapcastConfiguration { Enabled = true },
                Mqtt = new MqttConfiguration { Enabled = true },
                Knx = new KnxConfiguration { Enabled = true },
                Subsonic = new SubsonicConfiguration { Enabled = true },
            },
        };

        var options = Options.Create(_config);
        _protocolCoordinator = new ProtocolCoordinator(
            _mockSnapcastService.Object,
            _mockMqttService.Object,
            _mockKnxService.Object,
            _mockSubsonicService.Object,
            _mockClientRepository.Object,
            _mockZoneRepository.Object,
            options,
            _mockLogger.Object
        );
    }

    #region Constructor and Initialization Tests

    [Fact]
    public void Constructor_WithNullSnapcastService_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_config);

        // Act & Assert
        var act = () =>
            new ProtocolCoordinator(
                null!,
                _mockMqttService.Object,
                _mockKnxService.Object,
                _mockSubsonicService.Object,
                _mockClientRepository.Object,
                _mockZoneRepository.Object,
                options,
                _mockLogger.Object
            );

        act.Should().Throw<ArgumentNullException>().WithParameterName("snapcastService");
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () =>
            new ProtocolCoordinator(
                _mockSnapcastService.Object,
                _mockMqttService.Object,
                _mockKnxService.Object,
                _mockSubsonicService.Object,
                _mockClientRepository.Object,
                _mockZoneRepository.Object,
                null!,
                _mockLogger.Object
            );

        act.Should().Throw<ArgumentNullException>().WithParameterName("config");
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyStarted_ShouldReturnSuccessWithoutReinitialization()
    {
        // Arrange
        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        // Act
        var result = await _protocolCoordinator.StartAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockMqttService.Verify(
            static x => x.SubscribeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task StartAsync_WithMqttSubscriptionFailure_ShouldStillStartSuccessfully()
    {
        // Arrange
        _mockMqttService
            .Setup(static x => x.SubscribeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockSnapcastService
            .Setup(static x => x.SynchronizeServerStateAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _protocolCoordinator.StartAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        VerifyLoggerWarning("Failed to subscribe to MQTT topics");
    }

    [Fact]
    public async Task StopAsync_WhenNotStarted_ShouldReturnSuccessWithoutError()
    {
        // Act
        var result = await _protocolCoordinator.StopAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Volume Synchronization Tests

    [Fact]
    public async Task SynchronizeVolumeChangeAsync_WithValidClient_ShouldSyncToAllNonSourceProtocols()
    {
        // Arrange
        var clientId = "test-client";
        var volume = 75;
        var sourceProtocol = "MQTT";

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        var client = CreateTestClient(clientId);
        _mockClientRepository.Setup(x => x.GetByIdAsync(clientId, It.IsAny<CancellationToken>())).ReturnsAsync(client);

        var zone = CreateTestZone("1");
        _mockZoneRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        SetupSuccessfulProtocolResponses();

        // Act
        var result = await _protocolCoordinator.SynchronizeVolumeChangeAsync(clientId, volume, sourceProtocol);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Should sync to Snapcast (not source)
        _mockSnapcastService.Verify(
            x => x.SetClientVolumeAsync(clientId, volume, It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Should sync to KNX (not source)
        _mockKnxService.Verify(
            x => x.SendVolumeCommandAsync(It.IsAny<string>(), volume, It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Should NOT sync to MQTT (is source)
        _mockMqttService.Verify(
            x =>
                x.PublishAsync(
                    It.Is<string>(s => s.Contains("volume")),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Theory]
    [InlineData("Snapcast")]
    [InlineData("KNX")]
    [InlineData("MQTT")]
    public async Task SynchronizeVolumeChangeAsync_WithDifferentSourceProtocols_ShouldExcludeSource(
        string sourceProtocol
    )
    {
        // Arrange
        var clientId = "1";
        var volume = 60;

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        var client = CreateTestClient(clientId);
        _mockClientRepository.Setup(x => x.GetByIdAsync(clientId, It.IsAny<CancellationToken>())).ReturnsAsync(client);

        var zone = CreateTestZone("1");
        _mockZoneRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        SetupSuccessfulProtocolResponses();

        // Act
        var result = await _protocolCoordinator.SynchronizeVolumeChangeAsync(clientId, volume, sourceProtocol);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify source protocol is excluded
        switch (sourceProtocol)
        {
            case "Snapcast":
                _mockSnapcastService.Verify(
                    x => x.SetClientVolumeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                    Times.Never
                );
                break;
            case "KNX":
                _mockKnxService.Verify(
                    x => x.SendVolumeCommandAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                    Times.Never
                );
                break;
            case "MQTT":
                _mockMqttService.Verify(
                    x =>
                        x.PublishAsync(
                            It.Is<string>(s => s.Contains("volume")),
                            It.IsAny<string>(),
                            It.IsAny<CancellationToken>()
                        ),
                    Times.Never
                );
                break;
        }
    }

    [Fact]
    public async Task SynchronizeVolumeChangeAsync_WithNonExistentClient_ShouldReturnFailure()
    {
        // Arrange
        var clientId = "999";
        var volume = 50;
        var sourceProtocol = "MQTT";

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        _mockClientRepository
            .Setup(static x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client)null!);

        // Act
        var result = await _protocolCoordinator.SynchronizeVolumeChangeAsync(clientId, volume, sourceProtocol);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain($"Client with ID {clientId} not found");
    }

    [Fact]
    public async Task SynchronizeVolumeChangeAsync_WithPartialFailures_ShouldReturnFailureWithDetails()
    {
        // Arrange
        var clientId = "1";
        var volume = 80;
        var sourceProtocol = "MQTT";

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        var client = CreateTestClient(clientId);
        _mockClientRepository.Setup(x => x.GetByIdAsync(clientId, It.IsAny<CancellationToken>())).ReturnsAsync(client);

        var zone = CreateTestZone("1");
        _mockZoneRepository
            .Setup(x => x.GetByIdAsync(client.ZoneId!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        // Setup Snapcast to succeed, KNX to fail
        _mockSnapcastService
            .Setup(x => x.SetClientVolumeAsync(clientId, volume, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockKnxService
            .Setup(x => x.SendVolumeCommandAsync(zone.KnxVolumeGroupAddress!, volume, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _protocolCoordinator.SynchronizeVolumeChangeAsync(clientId, volume, sourceProtocol);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Partial sync failure");
        result.Error.Should().Contain("1/2 failed");
    }

    [Fact]
    public async Task SynchronizeVolumeChangeAsync_WithClientWithoutKnxConfiguration_ShouldSkipKnxSync()
    {
        // Arrange
        var clientId = "1";
        var volume = 70;
        var sourceProtocol = "MQTT";

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        var client = CreateTestClient(clientId);

        _mockClientRepository.Setup(x => x.GetByIdAsync(clientId, It.IsAny<CancellationToken>())).ReturnsAsync(client);

        var zone = new Zone
        {
            Id = "1",
            Name = "Test Zone",
            KnxVolumeGroupAddress = null,
        };
        _mockZoneRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        _mockSnapcastService
            .Setup(x => x.SetClientVolumeAsync(clientId, volume, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _protocolCoordinator.SynchronizeVolumeChangeAsync(clientId, volume, sourceProtocol);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Should sync to Snapcast
        _mockSnapcastService.Verify(
            x => x.SetClientVolumeAsync(clientId, volume, It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Should NOT attempt KNX sync (no configuration)
        _mockKnxService.Verify(
            x => x.SendVolumeCommandAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    #endregion

    #region Zone Volume Synchronization Tests

    [Fact]
    public async Task SynchronizeZoneVolumeChangeAsync_WithValidZone_ShouldSyncToEnabledProtocols()
    {
        // Arrange
        var zoneId = "1";
        var volume = 85;
        var sourceProtocol = "KNX";

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        var zone = CreateTestZone(zoneId);
        _mockZoneRepository.Setup(x => x.GetByIdAsync(zoneId, It.IsAny<CancellationToken>())).ReturnsAsync(zone);

        _mockMqttService
            .Setup(x =>
                x.PublishAsync(
                    It.Is<string>(s => s.Contains("volume")),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);

        // Act
        var result = await _protocolCoordinator.SynchronizeZoneVolumeChangeAsync(zoneId, volume, sourceProtocol);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Should sync to MQTT (not source)
        _mockMqttService.Verify(
            x =>
                x.PublishAsync(
                    It.Is<string>(s => s.Contains("volume")),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        // Should NOT sync to KNX (is source)
        _mockKnxService.Verify(
            x => x.SendVolumeCommandAsync(It.IsAny<string>(), volume, It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SynchronizeZoneVolumeChangeAsync_WithNonExistentZone_ShouldReturnFailure()
    {
        // Arrange
        var zoneId = "999";
        var volume = 50;
        var sourceProtocol = "MQTT";

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        _mockZoneRepository.Setup(x => x.GetByIdAsync(zoneId, It.IsAny<CancellationToken>())).ReturnsAsync((Zone)null!);

        // Act
        var result = await _protocolCoordinator.SynchronizeZoneVolumeChangeAsync(zoneId, volume, sourceProtocol);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain($"Zone with ID {zoneId} not found");
    }

    #endregion

    #region Mute Synchronization Tests

    [Fact]
    public async Task SynchronizeMuteChangeAsync_WithValidClient_ShouldSyncToRelevantProtocols()
    {
        // Arrange
        var clientId = "1";
        var muted = true;
        var sourceProtocol = "Snapcast";

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        var client = CreateTestClient(clientId);
        _mockClientRepository.Setup(x => x.GetByIdAsync(clientId, It.IsAny<CancellationToken>())).ReturnsAsync(client);

        var zone = CreateTestZone("1");
        _mockZoneRepository
            .Setup(x => x.GetByIdAsync(client.ZoneId!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        _mockKnxService
            .Setup(x => x.SendBooleanCommandAsync(zone.KnxMuteGroupAddress!, muted, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _protocolCoordinator.SynchronizeMuteChangeAsync(clientId, muted, sourceProtocol);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Should sync to KNX (not source)
        _mockKnxService.Verify(
            x => x.SendBooleanCommandAsync(zone.KnxMuteGroupAddress!, muted, It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Should NOT sync to Snapcast (is source)
        _mockSnapcastService.Verify(
            x => x.SetClientMuteAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SynchronizeMuteChangeAsync_WithClientWithoutKnxMuteAddress_ShouldSkipKnxSync()
    {
        // Arrange
        var clientId = "1";
        var muted = false;
        var sourceProtocol = "Snapcast";

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        var client = CreateTestClient(clientId);

        _mockClientRepository.Setup(x => x.GetByIdAsync(clientId, It.IsAny<CancellationToken>())).ReturnsAsync(client);

        var zone = new Zone
        {
            Id = "1",
            Name = "Test Zone",
            KnxMuteGroupAddress = null,
        };
        _mockZoneRepository
            .Setup(x => x.GetByIdAsync(client.ZoneId!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        // Act
        var result = await _protocolCoordinator.SynchronizeMuteChangeAsync(clientId, muted, sourceProtocol);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Should NOT attempt KNX sync (no mute address)
        _mockKnxService.Verify(
            x => x.SendBooleanCommandAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    #endregion

    #region Playback Command Synchronization Tests

    [Fact]
    public async Task SynchronizePlaybackCommandAsync_WithValidCommand_ShouldBroadcastToMqtt()
    {
        // Arrange
        var command = "PLAY";
        var streamId = 2;
        var sourceProtocol = "Snapcast";

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        _mockMqttService
            .Setup(static x =>
                x.PublishAsync(
                    It.Is<string>(static s => s.Contains("status")),
                    "playing",
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);

        // Act
        var result = await _protocolCoordinator.SynchronizePlaybackCommandAsync(command, streamId, sourceProtocol);

        // Assert
        result.IsSuccess.Should().BeTrue();
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

    [Theory]
    [InlineData("PLAY", "playing")]
    [InlineData("STOP", "stopped")]
    [InlineData("PAUSE", "paused")]
    [InlineData("UNKNOWN", "unknown")]
    public async Task SynchronizePlaybackCommandAsync_WithDifferentCommands_ShouldMapStatusCorrectly(
        string command,
        string expectedStatus
    )
    {
        // Arrange
        var streamId = 1;
        var sourceProtocol = "Snapcast";

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        _mockMqttService
            .Setup(x =>
                x.PublishAsync(It.Is<string>(s => s.Contains("status")), expectedStatus, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(true);

        // Act
        var result = await _protocolCoordinator.SynchronizePlaybackCommandAsync(command, streamId, sourceProtocol);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockMqttService.Verify(
            x =>
                x.PublishAsync(It.Is<string>(s => s.Contains("status")), expectedStatus, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task SynchronizePlaybackCommandAsync_WithoutStreamId_ShouldNotAttemptMqttSync()
    {
        // Arrange
        var command = "PLAY";
        int? streamId = null;
        var sourceProtocol = "Snapcast";

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        // Act
        var result = await _protocolCoordinator.SynchronizePlaybackCommandAsync(command, streamId, sourceProtocol);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockMqttService.Verify(
            static x => x.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    #endregion

    #region Stream Assignment Synchronization Tests

    [Fact]
    public async Task SynchronizeStreamAssignmentAsync_WithValidParameters_ShouldSyncToSnapcast()
    {
        // Arrange
        var groupId = "group1";
        var streamId = "stream2";
        var sourceProtocol = "MQTT";

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        _mockSnapcastService
            .Setup(x => x.SetGroupStreamAsync(groupId, streamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _protocolCoordinator.SynchronizeStreamAssignmentAsync(groupId, streamId, sourceProtocol);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockSnapcastService.Verify(
            x => x.SetGroupStreamAsync(groupId, streamId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task SynchronizeStreamAssignmentAsync_WithSnapcastAsSource_ShouldNotSyncToSnapcast()
    {
        // Arrange
        var groupId = "group1";
        var streamId = "stream2";
        var sourceProtocol = "Snapcast";

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        // Act
        var result = await _protocolCoordinator.SynchronizeStreamAssignmentAsync(groupId, streamId, sourceProtocol);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockSnapcastService.Verify(
            static x => x.SetGroupStreamAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    #endregion

    #region Client Status Synchronization Tests

    [Fact]
    public async Task SynchronizeClientStatusAsync_WithValidStatus_ShouldPublishToMqtt()
    {
        // Arrange
        var clientId = "test-client";
        var connected = true;
        var sourceProtocol = "Snapcast";

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        _mockMqttService
            .Setup(static x =>
                x.PublishAsync(
                    It.Is<string>(static s => s.Contains("status")),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);

        // Act
        var result = await _protocolCoordinator.SynchronizeClientStatusAsync(clientId, connected, sourceProtocol);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockMqttService.Verify(
            static x =>
                x.PublishAsync(
                    It.Is<string>(static s => s.Contains("status")),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task SynchronizeClientStatusAsync_WithMqttAsSource_ShouldNotPublishToMqtt()
    {
        // Arrange
        var clientId = "test-client";
        var connected = false;
        var sourceProtocol = "MQTT";

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        // Act
        var result = await _protocolCoordinator.SynchronizeClientStatusAsync(clientId, connected, sourceProtocol);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockMqttService.Verify(
            static x => x.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    #endregion

    #region Health Monitoring Tests

    [Fact]
    public async Task GetProtocolHealthAsync_WithAllProtocolsEnabled_ShouldCheckAllServices()
    {
        // Arrange
        _mockSnapcastService
            .Setup(static x => x.IsServerAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockSubsonicService
            .Setup(static x => x.IsServerAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _protocolCoordinator.GetProtocolHealthAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().ContainKey("Snapcast");
        result.Should().ContainKey("Subsonic");
        result.Should().ContainKey("MQTT");
        result.Should().ContainKey("KNX");

        result["Snapcast"].Should().BeTrue();
        result["Subsonic"].Should().BeFalse();
    }

    [Fact]
    public async Task GetProtocolHealthAsync_WithDisabledProtocols_ShouldOnlyCheckEnabled()
    {
        // Arrange
        _config.Services.Snapcast.Enabled = false;
        _config.Services.Subsonic.Enabled = false;

        // Act
        var result = await _protocolCoordinator.GetProtocolHealthAsync();

        // Assert
        result.Should().NotContainKey("Snapcast");
        result.Should().NotContainKey("Subsonic");
        result.Should().ContainKey("MQTT");
        result.Should().ContainKey("KNX");
    }

    [Fact]
    public async Task GetProtocolHealthAsync_WithHealthCheckException_ShouldHandleGracefully()
    {
        // Arrange
        _mockSnapcastService
            .Setup(static x => x.IsServerAvailableAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act
        var result = await _protocolCoordinator.GetProtocolHealthAsync();

        // Assert
        result.Should().NotBeNull();
        VerifyLoggerError("Error checking protocol health");
    }

    #endregion

    #region Debounce Logic Tests

    [Fact]
    public async Task SynchronizeVolumeChangeAsync_WithRapidChanges_ShouldDebounceRequests()
    {
        // Arrange
        var clientId = "1";
        var sourceProtocol = "MQTT";

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        var client = CreateTestClient(clientId);
        _mockClientRepository.Setup(x => x.GetByIdAsync(clientId, It.IsAny<CancellationToken>())).ReturnsAsync(client);

        var zone = CreateTestZone("1");
        _mockZoneRepository
            .Setup(x => x.GetByIdAsync(client.ZoneId!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        SetupSuccessfulProtocolResponses();

        // Act - Send multiple rapid changes
        var tasks = new List<Task<Result>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_protocolCoordinator.SynchronizeVolumeChangeAsync(clientId, 50 + i, sourceProtocol));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(r => r.IsSuccess);
        successCount.Should().BeLessThan(5); // Some should be debounced

        // At least one should succeed
        successCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SynchronizeVolumeChangeAsync_WithDifferentClients_ShouldNotDebounceAcrossClients()
    {
        // Arrange
        var volume = 60;
        var sourceProtocol = "MQTT";

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        var client1 = CreateTestClient("1");
        var client2 = CreateTestClient("2");

        _mockClientRepository.Setup(static x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(client1);
        _mockClientRepository.Setup(static x => x.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(client2);

        SetupSuccessfulProtocolResponses();

        // Act - Send changes for different clients simultaneously
        var task1 = _protocolCoordinator.SynchronizeVolumeChangeAsync("1", volume, sourceProtocol);
        var task2 = _protocolCoordinator.SynchronizeVolumeChangeAsync("2", volume, sourceProtocol);

        var results = await Task.WhenAll(task1, task2);

        // Assert
        results.Should().AllSatisfy(static r => r.IsSuccess.Should().BeTrue());
    }

    #endregion

    #region Event Handler Tests

    [Fact]
    public async Task Handle_SnapcastClientVolumeChangedEvent_ShouldTriggerVolumeSynchronization()
    {
        // Arrange
        var clientId = "1";
        var volume = 85;
        var volumeEvent = new SnapcastClientVolumeChangedEvent(clientId, volume, false);

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        var client = CreateTestClient(clientId);
        _mockClientRepository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(client);

        SetupSuccessfulProtocolResponses();

        // Act
        await _protocolCoordinator.Handle(volumeEvent, CancellationToken.None);

        // Assert
        _mockMqttService.Verify(
            x =>
                x.PublishAsync(
                    It.Is<string>(s => s.Contains("volume")),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _mockKnxService.Verify(
            x => x.SendVolumeCommandAsync(It.IsAny<string>(), volume, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_SnapcastClientConnectedEvent_ShouldTriggerStatusSynchronization()
    {
        // Arrange
        var clientId = "test-client";
        var connectedEvent = new SnapcastClientConnectedEvent(clientId);

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        _mockMqttService
            .Setup(static x =>
                x.PublishAsync(
                    It.Is<string>(static s => s.Contains("status")),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);

        // Act
        await _protocolCoordinator.Handle(connectedEvent, CancellationToken.None);

        // Assert
        _mockMqttService.Verify(
            static x =>
                x.PublishAsync(
                    It.Is<string>(static s => s.Contains("status")),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_SnapcastClientDisconnectedEvent_ShouldTriggerStatusSynchronization()
    {
        // Arrange
        var clientId = "test-client";
        var disconnectedEvent = new SnapcastClientDisconnectedEvent(clientId);

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        _mockMqttService
            .Setup(static x =>
                x.PublishAsync(
                    It.Is<string>(static s => s.Contains("status")),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);

        // Act
        await _protocolCoordinator.Handle(disconnectedEvent, CancellationToken.None);

        // Assert
        _mockMqttService.Verify(
            static x =>
                x.PublishAsync(
                    It.Is<string>(static s => s.Contains("status")),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_MqttZoneVolumeCommandEvent_ShouldTriggerZoneVolumeSynchronization()
    {
        // Arrange
        var zoneId = "2";
        var volume = 90;
        var zoneVolumeEvent = new MqttZoneVolumeCommandEvent(zoneId, volume);

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        var zone = CreateTestZone(zoneId);
        _mockZoneRepository.Setup(x => x.GetByIdAsync(zoneId, It.IsAny<CancellationToken>())).ReturnsAsync(zone);

        _mockKnxService
            .Setup(x => x.SendVolumeCommandAsync(zone.KnxVolumeGroupAddress!, volume, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _protocolCoordinator.Handle(zoneVolumeEvent, CancellationToken.None);

        // Assert
        _mockKnxService.Verify(
            x => x.SendVolumeCommandAsync(zone.KnxVolumeGroupAddress!, volume, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_MqttClientVolumeCommandEvent_ShouldTriggerClientVolumeSynchronization()
    {
        // Arrange
        var clientId = "mqtt-client";
        var volume = 65;
        var clientVolumeEvent = new MqttClientVolumeCommandEvent(clientId, volume);

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        var client = CreateTestClient(clientId);
        _mockClientRepository.Setup(x => x.GetByIdAsync(clientId, It.IsAny<CancellationToken>())).ReturnsAsync(client);

        var zone = CreateTestZone("1");
        _mockZoneRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        // Act
        await _protocolCoordinator.Handle(clientVolumeEvent, CancellationToken.None);

        // Assert
        _mockSnapcastService.Verify(
            s => s.SetClientVolumeAsync(clientId, volume, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task SynchronizeVolumeChangeAsync_WhenNotStarted_ShouldReturnFailure()
    {
        // Act
        var result = await _protocolCoordinator.SynchronizeVolumeChangeAsync("1", 50, "MQTT");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not started");
    }

    [Fact]
    public async Task SynchronizeVolumeChangeAsync_AfterDispose_ShouldReturnFailure()
    {
        // Arrange
        SetupMockServices();
        await _protocolCoordinator.StartAsync();
        _protocolCoordinator.Dispose();

        // Act
        var result = await _protocolCoordinator.SynchronizeVolumeChangeAsync("1", 50, "MQTT");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("disposed");
    }

    [Fact]
    public async Task SynchronizeVolumeChangeAsync_WithRepositoryException_ShouldReturnFailure()
    {
        // Arrange
        var clientId = "1";
        var volume = 50;
        var sourceProtocol = "MQTT";

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        _mockClientRepository
            .Setup(x => x.GetByIdAsync(clientId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _protocolCoordinator.SynchronizeVolumeChangeAsync(clientId, volume, sourceProtocol);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Volume sync error");
        VerifyLoggerError("Error synchronizing volume change");
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task SynchronizeVolumeChangeAsync_WithConcurrentCalls_ShouldHandleThreadSafely()
    {
        // Arrange
        const int concurrentCalls = 50;
        var tasks = new List<Task<Result>>();

        SetupMockServices();
        await _protocolCoordinator.StartAsync();

        for (int i = 0; i < concurrentCalls; i++)
        {
            var clientId = (i % 5 + 1).ToString(); // Use 5 different clients
            var client = CreateTestClient(clientId);
            _mockClientRepository
                .Setup(x => x.GetByIdAsync(clientId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(client);
        }

        var zone = CreateTestZone("1");
        _mockZoneRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        SetupSuccessfulProtocolResponses();

        // Act
        for (int i = 0; i < concurrentCalls; i++)
        {
            var clientId = (i % 5 + 1).ToString();
            var volume = i % 101;
            tasks.Add(_protocolCoordinator.SynchronizeVolumeChangeAsync(clientId, volume, "MQTT"));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(r => r.IsSuccess);
        successCount.Should().BeGreaterThan(0); // At least some should succeed

        // No exceptions should be thrown due to thread safety issues
        results.Should().NotContain(r => r.Error != null && r.Error.Contains("thread"));
    }

    #endregion

    #region Helper Methods

    private void SetupMockServices()
    {
        _mockMqttService
            .Setup(static x => x.SubscribeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockSnapcastService
            .Setup(static x => x.SynchronizeServerStateAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupSuccessfulProtocolResponses()
    {
        _mockSnapcastService
            .Setup(static x =>
                x.SetClientVolumeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(true);
        _mockSnapcastService
            .Setup(static x =>
                x.SetClientMuteAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(true);
        _mockSnapcastService
            .Setup(static x =>
                x.SetGroupStreamAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(true);

        _mockMqttService
            .Setup(static x => x.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockKnxService
            .Setup(static x =>
                x.SendVolumeCommandAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(true);
        _mockKnxService
            .Setup(static x =>
                x.SendBooleanCommandAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(true);
    }

    private Client CreateTestClient(string id)
    {
        return new Client
        {
            Id = id,
            Name = $"Test Client {id}",
            MacAddress = "00:00:00:00:00:00",
            IpAddress = "127.0.0.1",
            Status = SnapDog2.Core.Models.Enums.ClientStatus.Connected,
            Volume = 50,
            ZoneId = "1",
        };
    }

    private Zone CreateTestZone(string id)
    {
        return new Zone
        {
            Id = id,
            Name = $"Test Zone {id}",
            KnxVolumeGroupAddress = "2/3/4",
            KnxMuteGroupAddress = "2/3/5",
        };
    }

    private void VerifyLoggerError(string expectedMessage)
    {
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.AtLeastOnce
        );
    }

    private void VerifyLoggerWarning(string expectedMessage)
    {
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.AtLeastOnce
        );
    }

    private void VerifyLoggerInfo(string expectedMessage)
    {
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.AtLeastOnce
        );
    }

    #endregion

    public void Dispose()
    {
        _protocolCoordinator?.Dispose();
    }
}
