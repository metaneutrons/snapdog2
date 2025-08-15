namespace SnapDog2.Tests.Unit.Core.Configuration;

using FluentAssertions;
using SnapDog2.Core.Configuration;

public class SnapDogConfigurationTests
{
    [Fact]
    public void Constructor_ShouldInitializeAllProperties()
    {
        // Act
        var config = new SnapDogConfiguration();

        // Assert
        config.System.Should().NotBeNull();
        config.Telemetry.Should().NotBeNull();
        config.Api.Should().NotBeNull();
        config.Services.Should().NotBeNull();
    }

    [Fact]
    public void SystemConfig_ShouldHaveDefaultValues()
    {
        // Act
        var config = new SystemConfig();

        // Assert
        config.LogLevel.Should().Be("Information");
        config.Environment.Should().Be("Development");
        config.DebugEnabled.Should().BeFalse();
        config.HealthChecksEnabled.Should().BeTrue();
        config.HealthChecksTimeout.Should().Be(30);
        config.HealthChecksTags.Should().Be("ready,live");
        config.MqttBaseTopic.Should().Be("snapdog");
        config.MqttStatusTopic.Should().Be("status");
        config.MqttErrorTopic.Should().Be("error");
        config.MqttVersionTopic.Should().Be("version");
        config.MqttZonesTopic.Should().Be("system/zones");
        config.MqttStatsTopic.Should().Be("stats");
        config.LogFile.Should().BeNull();
    }

    [Fact]
    public void TelemetryConfig_ShouldHaveDefaultValues()
    {
        // Act
        var config = new TelemetryConfig();

        // Assert
        config.Enabled.Should().BeFalse();
        config.ServiceName.Should().Be("SnapDog2");
        config.SamplingRate.Should().Be(1.0);
        config.Otlp.Should().NotBeNull();
        config.Prometheus.Should().NotBeNull();
    }

    [Fact]
    public void ApiConfig_ShouldHaveDefaultValues()
    {
        // Act
        var config = new ApiConfig();

        // Assert
        config.AuthEnabled.Should().BeTrue();
        config.ApiKeys.Should().NotBeNull();
        config.ApiKeys.Should().BeEmpty();
    }

    [Fact]
    public void ServicesConfig_ShouldHaveDefaultValues()
    {
        // Act
        var config = new ServicesConfig();

        // Assert
        config.Snapcast.Should().NotBeNull();
        config.Mqtt.Should().NotBeNull();
        config.Knx.Should().NotBeNull();
        config.Subsonic.Should().NotBeNull();
    }
}
