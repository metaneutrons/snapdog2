using EnvoyConfig;
using EnvoyConfig.Conversion;
using Microsoft.Extensions.Configuration;
using SnapDog2.Core.Configuration;
using Xunit;

namespace SnapDog2.Tests.Configuration;

/// <summary>
/// Tests for SnapDog configuration classes to validate configuration loading and structure.
/// Tests EnvoyConfig pattern integration and environment variable support.
/// </summary>
public class ConfigurationTests
{
    /// <summary>
    /// Loads SnapDog configuration using EnvoyConfig with test environment variables.
    /// This replaces the old ConfigurationLoader approach.
    /// </summary>
    /// <param name="environmentVariables">Dictionary of environment variables to set.</param>
    /// <returns>A fully configured SnapDogConfiguration instance.</returns>
    private static SnapDogConfiguration LoadSnapDogConfigForTest(Dictionary<string, string?> environmentVariables)
    {
        // Set environment variables for the test
        foreach (var kvp in environmentVariables)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }

        try
        {
            // Register custom type converters
            TypeConverterRegistry.RegisterConverter(typeof(KnxAddress), new KnxAddressConverter());
            TypeConverterRegistry.RegisterConverter(typeof(KnxAddress?), new KnxAddressConverter());

            // Set the global prefix for EnvoyConfig
            EnvConfig.GlobalPrefix = "SNAPDOG_";

            // Use EnvoyConfig's automatic loading
            return EnvConfig.Load<SnapDogConfiguration>();
        }
        finally
        {
            // Clean up environment variables after test
            foreach (var kvp in environmentVariables)
            {
                Environment.SetEnvironmentVariable(kvp.Key, null);
            }
        }
    }

    [Fact]
    public void SnapDogConfiguration_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var config = new SnapDogConfiguration();

        // Assert
        Assert.NotNull(config.System);
        Assert.NotNull(config.Telemetry);
        Assert.NotNull(config.Api);
        Assert.NotNull(config.Services);
        Assert.NotNull(config.Zones);
        Assert.NotNull(config.Clients);
        Assert.NotNull(config.RadioStations);

        // Check default values
        Assert.Equal("Development", config.System.Environment);
        Assert.Equal("Information", config.System.LogLevel);
        Assert.Equal("SnapDog2", config.System.ApplicationName);
        Assert.True(config.Telemetry.Enabled);
        Assert.Equal("snapdog2", config.Telemetry.ServiceName);
        Assert.Equal(5000, config.Api.Port);
        Assert.False(config.Api.HttpsEnabled);
    }

    [Fact]
    public void SystemConfiguration_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var config = new SystemConfiguration();

        // Assert
        Assert.Equal("Development", config.Environment);
        Assert.Equal("Information", config.LogLevel);
        Assert.Equal("SnapDog2", config.ApplicationName);
        Assert.Equal("1.0.0", config.Version);
        Assert.False(config.DebugEnabled);
        Assert.Equal("./data", config.DataPath);
        Assert.Equal("./config", config.ConfigPath);
        Assert.Equal("./logs", config.LogsPath);
    }

    [Fact]
    public void TelemetryConfiguration_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var config = new TelemetryConfiguration();

        // Assert
        Assert.True(config.Enabled);
        Assert.Equal("snapdog2", config.ServiceName);
        Assert.Equal(1.0, config.SamplingRate);
        Assert.True(config.PrometheusEnabled);
        Assert.Equal(9090, config.PrometheusPort);
        Assert.Equal("/metrics", config.PrometheusPath);
        Assert.True(config.JaegerEnabled);
        Assert.Equal("http://jaeger:14268", config.JaegerEndpoint);
        Assert.Equal("jaeger", config.JaegerAgentHost);
        Assert.Equal(6831, config.JaegerAgentPort);
    }

    [Fact]
    public void ApiConfiguration_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var config = new ApiConfiguration();

        // Assert
        Assert.Equal(5000, config.Port);
        Assert.False(config.HttpsEnabled);
        Assert.Equal(string.Empty, config.ApiKey);
        Assert.False(config.AuthEnabled);
        Assert.True(config.CorsEnabled);
        Assert.True(config.SwaggerEnabled);
        Assert.True(config.RateLimitEnabled);
        Assert.Equal(100, config.RateLimitRequestsPerMinute);
    }

    [Fact]
    public void ServicesConfiguration_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var config = new ServicesConfiguration();

        // Assert
        Assert.NotNull(config.Snapcast);
        Assert.NotNull(config.Mqtt);
        Assert.NotNull(config.Knx);
        Assert.NotNull(config.Subsonic);

        // Check Snapcast defaults
        Assert.Equal("localhost", config.Snapcast.Host);
        Assert.Equal(1705, config.Snapcast.Port);
        Assert.Equal(30, config.Snapcast.TimeoutSeconds);

        // Check MQTT defaults
        Assert.Equal("localhost", config.Mqtt.Broker);
        Assert.Equal(1883, config.Mqtt.Port);

        // Check KNX defaults
        Assert.False(config.Knx.Enabled);
        Assert.Equal("192.168.1.1", config.Knx.Gateway);
        Assert.Equal(3671, config.Knx.Port);
    }

    [Fact]
    public void ClientConfiguration_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var config = new ClientConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.Name);
        Assert.Equal(string.Empty, config.Mac);
        Assert.Equal(string.Empty, config.MqttBaseTopic);
        Assert.Equal(1, config.DefaultZone);
        Assert.NotNull(config.Mqtt);
        Assert.NotNull(config.Knx);
    }

    [Fact]
    public void MqttClientConfiguration_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var config = new MqttClientConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.VolumeSetTopic);
        Assert.Equal(string.Empty, config.MuteSetTopic);
        Assert.Equal(string.Empty, config.LatencySetTopic);
        Assert.Equal(string.Empty, config.ZoneSetTopic);
        Assert.Equal(string.Empty, config.ControlTopic);
        Assert.Equal(string.Empty, config.ConnectedTopic);
        Assert.Equal(string.Empty, config.VolumeTopic);
        Assert.Equal(string.Empty, config.MuteTopic);
        Assert.Equal(string.Empty, config.LatencyTopic);
        Assert.Equal(string.Empty, config.ZoneTopic);
        Assert.Equal(string.Empty, config.StateTopic);
    }

    [Fact]
    public void KnxClientConfiguration_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var config = new KnxClientConfiguration();

        // Assert
        Assert.False(config.Enabled);
        Assert.Null(config.Volume);
        Assert.Null(config.VolumeStatus);
        Assert.Null(config.VolumeUp);
        Assert.Null(config.VolumeDown);
        Assert.Null(config.Mute);
        Assert.Null(config.MuteStatus);
        Assert.Null(config.MuteToggle);
        Assert.Null(config.Latency);
        Assert.Null(config.LatencyStatus);
        Assert.Null(config.Zone);
        Assert.Null(config.ZoneStatus);
        Assert.Null(config.ConnectedStatus);
    }

    [Fact]
    public void RadioStationConfiguration_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var config = new RadioStationConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.Name);
        Assert.Equal(string.Empty, config.Url);
        Assert.Equal(string.Empty, config.Description);
        Assert.True(config.Enabled);
    }

    [Fact]
    public void ZoneConfiguration_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var config = new ZoneConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.Name);
        Assert.Equal(string.Empty, config.Description);
        Assert.True(config.Enabled);
    }

    [Fact]
    public void EnvoyConfig_ShouldLoadSnapDogConfig()
    {
        // Arrange
        var configDict = new Dictionary<string, string?>
        {
            { "SNAPDOG_SYSTEM_ENVIRONMENT", "Production" },
            { "SNAPDOG_SYSTEM_LOG_LEVEL", "Warning" },
            { "SNAPDOG_TELEMETRY_ENABLED", "false" },
            { "SNAPDOG_API_PORT", "8080" },
            { "SNAPDOG_SERVICES_SNAPCAST_HOST", "snapcast.example.com" },
        };

        // Act
        var config = LoadSnapDogConfigForTest(configDict);

        // Assert
        Assert.Equal("Production", config.System.Environment);
        Assert.Equal("Warning", config.System.LogLevel);
        Assert.False(config.Telemetry.Enabled);
        Assert.Equal(8080, config.Api.Port);
        Assert.Equal("snapcast.example.com", config.Services.Snapcast.Host);
    }

    [Fact]
    public void EnvoyConfig_ShouldLoadSnapDogConfigWithClients()
    {
        // Arrange
        var configDict = new Dictionary<string, string?>
        {
            { "SNAPDOG_CLIENT_1_NAME", "Living Room" },
            { "SNAPDOG_CLIENT_1_MAC", "aa:bb:cc:dd:ee:ff" },
            { "SNAPDOG_CLIENT_1_MQTT_BASETOPIC", "snapdog/livingroom" },
            { "SNAPDOG_CLIENT_1_DEFAULT_ZONE", "2" },
            { "SNAPDOG_CLIENT_1_KNX_ENABLED", "true" },
            { "SNAPDOG_CLIENT_1_KNX_VOLUME", "2/1/1" },
        };

        // Act
        var config = LoadSnapDogConfigForTest(configDict);

        // Assert
        Assert.Single(config.Clients);
        var client = config.Clients[0];
        Assert.Equal("Living Room", client.Name);
        Assert.Equal("aa:bb:cc:dd:ee:ff", client.Mac);
        Assert.Equal("snapdog/livingroom", client.MqttBaseTopic);
        Assert.Equal(2, client.DefaultZone);
        Assert.True(client.Knx.Enabled);
        Assert.NotNull(client.Knx.Volume);
        Assert.Equal("2/1/1", client.Knx.Volume.Value.ToString());
    }

    [Fact]
    public void EnvoyConfig_ShouldLoadSnapDogConfigWithRadioStations()
    {
        // Arrange
        var configDict = new Dictionary<string, string?>
        {
            { "SNAPDOG_RADIO_1_NAME", "Classical FM" },
            { "SNAPDOG_RADIO_1_URL", "http://stream.example.com/classical" },
            { "SNAPDOG_RADIO_1_DESCRIPTION", "Best classical music" },
            { "SNAPDOG_RADIO_1_ENABLED", "true" },
            { "SNAPDOG_RADIO_2_NAME", "Jazz 24/7" },
            { "SNAPDOG_RADIO_2_URL", "http://stream.example.com/jazz" },
            { "SNAPDOG_RADIO_2_ENABLED", "false" },
        };

        // Act
        var config = LoadSnapDogConfigForTest(configDict);

        // Assert
        Assert.Equal(2, config.RadioStations.Count);

        var station1 = config.RadioStations[0];
        Assert.Equal("Classical FM", station1.Name);
        Assert.Equal("http://stream.example.com/classical", station1.Url);
        Assert.Equal("Best classical music", station1.Description);
        Assert.True(station1.Enabled);

        var station2 = config.RadioStations[1];
        Assert.Equal("Jazz 24/7", station2.Name);
        Assert.Equal("http://stream.example.com/jazz", station2.Url);
        Assert.False(station2.Enabled);
    }

    [Fact]
    public void EnvoyConfig_ShouldLoadSnapDogConfigWithZones()
    {
        // Arrange
        var configDict = new Dictionary<string, string?>
        {
            { "SNAPDOG_ZONE_1_NAME", "Living Room" },
            { "SNAPDOG_ZONE_1_DESCRIPTION", "Main living area" },
            { "SNAPDOG_ZONE_1_ENABLED", "true" },
            { "SNAPDOG_ZONE_2_NAME", "Kitchen" },
            { "SNAPDOG_ZONE_2_ENABLED", "false" },
        };

        // Act
        var config = LoadSnapDogConfigForTest(configDict);

        // Assert
        Assert.Equal(2, config.Zones.Count);

        var zone1 = config.Zones[0];
        Assert.Equal("Living Room", zone1.Name);
        Assert.Equal("Main living area", zone1.Description);
        Assert.True(zone1.Enabled);

        var zone2 = config.Zones[1];
        Assert.Equal("Kitchen", zone2.Name);
        Assert.False(zone2.Enabled);
    }

    [Fact]
    public void EnvoyConfig_ShouldHandleEmptyConfiguration()
    {
        // Arrange
        var configDict = new Dictionary<string, string?>();

        // Act
        var config = LoadSnapDogConfigForTest(configDict);

        // Assert
        Assert.Equal("Development", config.System.Environment);
        Assert.Equal("Information", config.System.LogLevel);
        Assert.True(config.Telemetry.Enabled);
        Assert.Empty(config.Clients);
        Assert.Empty(config.RadioStations);
        Assert.Empty(config.Zones);
    }

    [Fact]
    public void EnvoyConfig_ShouldLoadAllClientConfigurations()
    {
        // Arrange - EnvoyConfig loads all clients, even with missing properties
        var configDict = new Dictionary<string, string?>
        {
            { "SNAPDOG_CLIENT_1_MAC", "aa:bb:cc:dd:ee:ff" },
            { "SNAPDOG_CLIENT_2_NAME", "Kitchen" },
            { "SNAPDOG_CLIENT_2_MAC", "11:22:33:44:55:66" },
        };

        // Act
        var config = LoadSnapDogConfigForTest(configDict);

        // Assert - EnvoyConfig creates both clients, even if first one has empty name
        Assert.Equal(2, config.Clients.Count);

        var client1 = config.Clients[0];
        Assert.Equal(string.Empty, client1.Name); // Default value for missing name
        Assert.Equal("aa:bb:cc:dd:ee:ff", client1.Mac);

        var client2 = config.Clients[1];
        Assert.Equal("Kitchen", client2.Name);
        Assert.Equal("11:22:33:44:55:66", client2.Mac);
    }
}
