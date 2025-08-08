namespace SnapDog2.Tests.Unit.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SnapDog2.Core.Configuration;
using SnapDog2.Services;

public class StartupInformationServiceTests
{
    [Fact]
    public async Task StartAsync_WithApiEnabled_ShouldLogApiInformation()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<StartupInformationService>>();
        var config = CreateTestConfiguration(apiEnabled: true, apiPort: 8080, authEnabled: true, apiKeysCount: 2);
        var options = Options.Create(config);
        var service = new StartupInformationService(mockLogger.Object, options);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        VerifyLogContains(mockLogger, "🌐 API Server:");
        VerifyLogContains(mockLogger, "Status: Enabled on port 8080");
        VerifyLogContains(mockLogger, "Authentication: Enabled (2 API keys configured)");
    }

    [Fact]
    public async Task StartAsync_WithApiDisabled_ShouldLogApiDisabled()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<StartupInformationService>>();
        var config = CreateTestConfiguration(apiEnabled: false, apiPort: 5000, authEnabled: true, apiKeysCount: 0);
        var options = Options.Create(config);
        var service = new StartupInformationService(mockLogger.Object, options);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        VerifyLogContains(mockLogger, "🌐 API Server:");
        VerifyLogContains(mockLogger, "Status: Disabled");
        VerifyLogContains(mockLogger, "No HTTP endpoints will be available");
    }

    [Fact]
    public async Task StartAsync_WithAuthEnabledButNoKeys_ShouldLogWarning()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<StartupInformationService>>();
        var config = CreateTestConfiguration(apiEnabled: true, apiPort: 5000, authEnabled: true, apiKeysCount: 0);
        var options = Options.Create(config);
        var service = new StartupInformationService(mockLogger.Object, options);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        VerifyLogContains(mockLogger, "Authentication: Enabled (0 API keys configured)");
        VerifyLogContains(
            mockLogger,
            "⚠️  Authentication is enabled but no API keys are configured!",
            LogLevel.Warning
        );
    }

    private static SnapDogConfiguration CreateTestConfiguration(
        bool apiEnabled,
        int apiPort,
        bool authEnabled,
        int apiKeysCount
    )
    {
        var apiKeys = new List<string>();
        for (int i = 0; i < apiKeysCount; i++)
        {
            apiKeys.Add($"test-key-{i + 1}");
        }

        return new SnapDogConfiguration
        {
            System = new SystemConfig
            {
                Environment = "Test",
                LogLevel = "Information",
                DebugEnabled = false,
                HealthChecksEnabled = true,
                HealthChecksTimeout = 30,
            },
            Api = new ApiConfig
            {
                Enabled = apiEnabled,
                Port = apiPort,
                AuthEnabled = authEnabled,
                ApiKeys = apiKeys,
            },
            Services = new ServicesConfig
            {
                Snapcast = new SnapcastConfig
                {
                    Address = "localhost",
                    JsonRpcPort = 1704,
                    Timeout = 5,
                    AutoReconnect = true,
                    ReconnectInterval = 10,
                },
                Mqtt = new MqttConfig { Enabled = false },
                Knx = new KnxConfig { Enabled = false },
                Subsonic = new SubsonicConfig { Enabled = false },
            },
            SnapcastServer = new SnapcastServerConfig
            {
                Codec = "flac",
                SampleFormat = "48000:16:2",
                WebServerPort = 1780,
                WebSocketPort = 1780,
                JsonRpcPort = 1704,
            },
            Telemetry = new TelemetryConfig { Enabled = false },
            Zones = new List<ZoneConfig>(),
            Clients = new List<ClientConfig>(),
            RadioStations = new List<RadioStationConfig>(),
        };
    }

    private static void VerifyLogContains(
        Mock<ILogger<StartupInformationService>> mockLogger,
        string expectedMessage,
        LogLevel logLevel = LogLevel.Information
    )
    {
        mockLogger.Verify(
            x =>
                x.Log(
                    logLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.AtLeastOnce,
            $"Expected log message containing '{expectedMessage}' at level {logLevel} was not found"
        );
    }
}
