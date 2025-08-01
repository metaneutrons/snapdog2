namespace SnapDog2.Tests.Unit.Core.Configuration;

using FluentAssertions;
using SnapDog2.Core.Configuration;

public class RadioStationConfigTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithNullValues()
    {
        // Act
        var config = new RadioStationConfig();

        // Assert
        config.Name.Should().BeNull();
        config.Url.Should().BeNull();
    }
}
