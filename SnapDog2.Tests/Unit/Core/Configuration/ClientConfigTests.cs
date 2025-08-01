namespace SnapDog2.Tests.Unit.Core.Configuration;

using FluentAssertions;
using SnapDog2.Core.Configuration;

public class ClientConfigTests
{
    [Fact]
    public void Constructor_ShouldInitializeNestedProperties()
    {
        // Act
        var config = new ClientConfig();

        // Assert
        config.Mqtt.Should().NotBeNull();
        config.Knx.Should().NotBeNull();
        config.DefaultZone.Should().Be(1);
    }

    [Fact]
    public void ClientMqttConfig_ShouldHaveDefaultValues()
    {
        // Act
        var config = new ClientMqttConfig();

        // Assert
        config.VolumeSetTopic.Should().Be("volume/set");
        config.MuteSetTopic.Should().Be("mute/set");
        config.LatencySetTopic.Should().Be("latency/set");
        config.ZoneSetTopic.Should().Be("zone/set");
        config.ConnectedTopic.Should().Be("connected");
        config.VolumeTopic.Should().Be("volume");
        config.MuteTopic.Should().Be("mute");
        config.LatencyTopic.Should().Be("latency");
        config.ZoneTopic.Should().Be("zone");
        config.StateTopic.Should().Be("state");
    }

    [Fact]
    public void ClientKnxConfig_ShouldHaveDefaultValues()
    {
        // Act
        var config = new ClientKnxConfig();

        // Assert
        config.Enabled.Should().BeFalse();
        config.Volume.Should().BeNull();
        config.VolumeStatus.Should().BeNull();
        config.Mute.Should().BeNull();
        config.MuteStatus.Should().BeNull();
    }
}
