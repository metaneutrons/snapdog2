namespace SnapDog2.Tests.Unit.Core.Configuration;

using FluentAssertions;
using SnapDog2.Core.Configuration;

public class ZoneConfigTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_ShouldInitializeNestedProperties()
    {
        // Act
        var config = new ZoneConfig();

        // Assert
        config.Knx.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
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
