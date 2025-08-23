namespace SnapDog2.Tests.Unit.Core.Configuration;

using FluentAssertions;
using SnapDog2.Core.Configuration;

public class ClientConfigTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_ShouldInitializeNestedProperties()
    {
        // Act
        var config = new ClientConfig();

        // Assert
        config.Knx.Should().NotBeNull();
        config.DefaultZone.Should().Be(1);
    }

    [Fact]
    [Trait("Category", "Unit")]
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
