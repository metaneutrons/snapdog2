using Microsoft.Extensions.Configuration;
using SnapDog2.Core.Configuration;
using Xunit;

namespace SnapDog2.Tests.Unit.Configuration;

/// <summary>
/// Tests for SnapDog configuration classes to validate configuration loading and structure.
/// </summary>
public class ConfigurationTests
{
    private static SnapDogConfiguration LoadSnapDogConfigForTest(Dictionary<string, string?> configuration)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configuration)
            .Build();

        return config.Get<SnapDogConfiguration>() ?? throw new InvalidOperationException("Could not load configuration");
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

}
