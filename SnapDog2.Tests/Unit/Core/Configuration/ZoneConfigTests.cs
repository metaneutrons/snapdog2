namespace SnapDog2.Tests.Unit.Core.Configuration;

using FluentAssertions;
using SnapDog2.Core.Configuration;

public class ZoneConfigTests
{
    [Fact]
    public void Constructor_ShouldInitializeNestedProperties()
    {
        // Act
        var config = new ZoneConfig();

        // Assert
        config.Mqtt.Should().NotBeNull();
        config.Knx.Should().NotBeNull();
    }

    [Fact]
    public void ZoneMqttConfig_ShouldHaveDefaultValues()
    {
        // Act
        var config = new ZoneMqttConfig();

        // Assert
        config.ControlSetTopic.Should().Be("control/set");
        config.ControlTopic.Should().Be("control");
        config.TrackSetTopic.Should().Be("track/set");
        config.TrackTopic.Should().Be("track");
        config.VolumeSetTopic.Should().Be("volume/set");
        config.VolumeTopic.Should().Be("volume");
        config.MuteSetTopic.Should().Be("mute/set");
        config.MuteTopic.Should().Be("mute");
        config.StateTopic.Should().Be("state");
    }

    [Fact]
    public void ZoneKnxConfig_ShouldHaveDefaultValues()
    {
        // Act
        var config = new ZoneKnxConfig();

        // Assert
        config.Enabled.Should().BeFalse();
        config.Play.Should().BeNull();
        config.Pause.Should().BeNull();
        config.Stop.Should().BeNull();
        config.Volume.Should().BeNull();
        config.Mute.Should().BeNull();
    }
}
