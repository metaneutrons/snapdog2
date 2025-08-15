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
        config.TrackNextTopic.Should().Be("next");
        config.TrackPreviousTopic.Should().Be("previous");
        config.TrackTopic.Should().Be("track");
        config.TrackInfoTopic.Should().Be("track/info");
        config.TrackRepeatSetTopic.Should().Be("repeat/track");
        config.TrackRepeatTopic.Should().Be("repeat/track");
        config.PlaylistSetTopic.Should().Be("playlist/set");
        config.PlaylistNextTopic.Should().Be("playlist/next");
        config.PlaylistPreviousTopic.Should().Be("playlist/previous");
        config.PlaylistTopic.Should().Be("playlist");
        config.PlaylistInfoTopic.Should().Be("playlist/info");
        config.PlaylistRepeatSetTopic.Should().Be("repeat/set");
        config.PlaylistRepeatTopic.Should().Be("repeat");
        config.PlaylistShuffleSetTopic.Should().Be("shuffle/set");
        config.PlaylistShuffleTopic.Should().Be("shuffle");
        config.VolumeSetTopic.Should().Be("volume/set");
        config.VolumeUpTopic.Should().Be("volume/up");
        config.VolumeDownTopic.Should().Be("volume/down");
        config.VolumeTopic.Should().Be("volume");
        config.MuteSetTopic.Should().Be("mute/set");
        config.MuteToggleTopic.Should().Be("mute/toggle");
        config.MuteTopic.Should().Be("mute");
        config.StateTopic.Should().Be("state");
        config.ErrorTopic.Should().Be("error");
        config.StatusTopic.Should().Be("status");
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
