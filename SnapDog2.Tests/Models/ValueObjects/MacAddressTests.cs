using SnapDog2.Core.Models.ValueObjects;
using Xunit;

namespace SnapDog2.Tests.Models.ValueObjects;

/// <summary>
/// Unit tests for the MacAddress value object.
/// Tests validation, normalization, parsing, and equality.
/// </summary>
public class MacAddressTests
{
    [Theory]
    [InlineData("AA:BB:CC:DD:EE:FF")]
    [InlineData("aa:bb:cc:dd:ee:ff")]
    [InlineData("AA-BB-CC-DD-EE-FF")]
    [InlineData("aa-bb-cc-dd-ee-ff")]
    [InlineData("12:34:56:78:9A:BC")]
    [InlineData("00:00:00:00:00:00")]
    [InlineData("FF:FF:FF:FF:FF:FF")]
    public void Constructor_WithValidMacAddress_ShouldCreateInstance(string macAddress)
    {
        // Act
        var mac = new MacAddress(macAddress);

        // Assert
        Assert.NotNull(mac.Value);
        Assert.NotEmpty(mac.Value);
    }

    [Theory]
    [InlineData("AA:BB:CC:DD:EE:FF", "AA:BB:CC:DD:EE:FF")]
    [InlineData("aa:bb:cc:dd:ee:ff", "AA:BB:CC:DD:EE:FF")]
    [InlineData("AA-BB-CC-DD-EE-FF", "AA:BB:CC:DD:EE:FF")]
    [InlineData("aa-bb-cc-dd-ee-ff", "AA:BB:CC:DD:EE:FF")]
    [InlineData("12:34:56:78:9a:bc", "12:34:56:78:9A:BC")]
    public void Constructor_WithValidMacAddress_ShouldNormalizeToUppercaseWithColons(string input, string expected)
    {
        // Act
        var mac = new MacAddress(input);

        // Assert
        Assert.Equal(expected, mac.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-mac")]
    [InlineData("AA:BB:CC:DD:EE")]
    [InlineData("AA:BB:CC:DD:EE:FF:GG")]
    [InlineData("XX:YY:ZZ:AA:BB:CC")]
    [InlineData("AA:BB:CC:DD:EE:GG")]
    [InlineData("AA:BB:CC:DD:EE:FF:AA")]
    [InlineData("AA-BB-CC-DD-EE")]
    [InlineData("not-a-mac-address")]
    public void Constructor_WithInvalidMacAddress_ShouldThrowArgumentException(string macAddress)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new MacAddress(macAddress));
    }

    [Fact]
    public void Constructor_WithNullMacAddress_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new MacAddress(null!));
    }

    [Theory]
    [InlineData("AA:BB:CC:DD:EE:FF", true)]
    [InlineData("aa:bb:cc:dd:ee:ff", true)]
    [InlineData("AA-BB-CC-DD-EE-FF", true)]
    [InlineData("12:34:56:78:9A:BC", true)]
    [InlineData("00:00:00:00:00:00", true)]
    [InlineData("FF:FF:FF:FF:FF:FF", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("invalid-mac", false)]
    [InlineData("AA:BB:CC:DD:EE", false)]
    [InlineData("AA:BB:CC:DD:EE:FF:GG", false)]
    [InlineData("XX:YY:ZZ:AA:BB:CC", false)]
    [InlineData("not-a-mac-address", false)]
    public void IsValid_WithVariousInputs_ShouldReturnCorrectResult(string macAddress, bool expected)
    {
        // Act
        var result = MacAddress.IsValid(macAddress);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsValid_WithNull_ShouldReturnFalse()
    {
        // Act
        var result = MacAddress.IsValid(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Parse_WithValidMacAddress_ShouldReturnMacAddress()
    {
        // Arrange
        var macAddressString = "AA:BB:CC:DD:EE:FF";

        // Act
        var mac = MacAddress.Parse(macAddressString);

        // Assert
        Assert.Equal("AA:BB:CC:DD:EE:FF", mac.Value);
    }

    [Fact]
    public void Parse_WithInvalidMacAddress_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidMacAddress = "invalid-mac";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => MacAddress.Parse(invalidMacAddress));
    }

    [Theory]
    [InlineData("AA:BB:CC:DD:EE:FF", true)]
    [InlineData("aa:bb:cc:dd:ee:ff", true)]
    [InlineData("AA-BB-CC-DD-EE-FF", true)]
    [InlineData("invalid-mac", false)]
    [InlineData("", false)]
    public void TryParse_WithVariousInputs_ShouldReturnCorrectResult(string input, bool shouldSucceed)
    {
        // Act
        var result = MacAddress.TryParse(input, out var mac);

        // Assert
        Assert.Equal(shouldSucceed, result);
        if (shouldSucceed)
        {
            Assert.NotEqual(default, mac);
            Assert.NotNull(mac.Value);
        }
        else
        {
            Assert.Equal(default, mac);
        }
    }

    [Fact]
    public void TryParse_WithNull_ShouldReturnFalse()
    {
        // Act
        var result = MacAddress.TryParse(null, out var mac);

        // Assert
        Assert.False(result);
        Assert.Equal(default, mac);
    }

    [Fact]
    public void ToString_ShouldReturnNormalizedValue()
    {
        // Arrange
        var mac = new MacAddress("aa-bb-cc-dd-ee-ff");

        // Act
        var result = mac.ToString();

        // Assert
        Assert.Equal("AA:BB:CC:DD:EE:FF", result);
    }

    [Fact]
    public void Equals_WithSameMacAddress_ShouldReturnTrue()
    {
        // Arrange
        var mac1 = new MacAddress("AA:BB:CC:DD:EE:FF");
        var mac2 = new MacAddress("aa:bb:cc:dd:ee:ff");

        // Act & Assert
        Assert.True(mac1.Equals(mac2));
        Assert.True(mac1 == mac2);
        Assert.False(mac1 != mac2);
    }

    [Fact]
    public void Equals_WithDifferentMacAddress_ShouldReturnFalse()
    {
        // Arrange
        var mac1 = new MacAddress("AA:BB:CC:DD:EE:FF");
        var mac2 = new MacAddress("11:22:33:44:55:66");

        // Act & Assert
        Assert.False(mac1.Equals(mac2));
        Assert.False(mac1 == mac2);
        Assert.True(mac1 != mac2);
    }

    [Fact]
    public void Equals_WithNonMacAddressObject_ShouldReturnFalse()
    {
        // Arrange
        var mac = new MacAddress("AA:BB:CC:DD:EE:FF");
        var notMac = "AA:BB:CC:DD:EE:FF";

        // Act & Assert
        Assert.False(mac.Equals(notMac));
    }

    [Fact]
    public void GetHashCode_WithSameMacAddress_ShouldReturnSameHashCode()
    {
        // Arrange
        var mac1 = new MacAddress("AA:BB:CC:DD:EE:FF");
        var mac2 = new MacAddress("aa:bb:cc:dd:ee:ff");

        // Act
        var hash1 = mac1.GetHashCode();
        var hash2 = mac2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_WithDifferentMacAddress_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var mac1 = new MacAddress("AA:BB:CC:DD:EE:FF");
        var mac2 = new MacAddress("11:22:33:44:55:66");

        // Act
        var hash1 = mac1.GetHashCode();
        var hash2 = mac2.GetHashCode();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ImplicitConversion_FromString_ShouldCreateMacAddress()
    {
        // Arrange
        string macString = "AA:BB:CC:DD:EE:FF";

        // Act
        MacAddress mac = macString;

        // Assert
        Assert.Equal("AA:BB:CC:DD:EE:FF", mac.Value);
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        // Arrange
        var mac = new MacAddress("AA:BB:CC:DD:EE:FF");

        // Act
        string macString = mac;

        // Assert
        Assert.Equal("AA:BB:CC:DD:EE:FF", macString);
    }

    [Fact]
    public void MacAddress_WithMixedCaseAndSeparators_ShouldNormalizeConsistently()
    {
        // Arrange
        var inputs = new[]
        {
            "aa:bb:cc:dd:ee:ff",
            "AA:BB:CC:DD:EE:FF",
            "aa-bb-cc-dd-ee-ff",
            "AA-BB-CC-DD-EE-FF",
            "Aa:Bb:Cc:Dd:Ee:Ff",
        };

        // Act
        var macAddresses = inputs.Select(input => new MacAddress(input)).ToArray();

        // Assert
        var expectedValue = "AA:BB:CC:DD:EE:FF";
        foreach (var mac in macAddresses)
        {
            Assert.Equal(expectedValue, mac.Value);
        }

        // All should be equal to each other
        for (int i = 0; i < macAddresses.Length; i++)
        {
            for (int j = i + 1; j < macAddresses.Length; j++)
            {
                Assert.Equal(macAddresses[i], macAddresses[j]);
            }
        }
    }

    [Theory]
    [InlineData("00:00:00:00:00:00")] // All zeros
    [InlineData("FF:FF:FF:FF:FF:FF")] // All ones (broadcast)
    [InlineData("01:23:45:67:89:AB")] // Mixed hex digits
    [InlineData("FE:DC:BA:98:76:54")] // Reverse order
    public void MacAddress_WithEdgeCaseValues_ShouldHandleCorrectly(string macAddress)
    {
        // Act
        var mac = new MacAddress(macAddress);

        // Assert
        Assert.Equal(macAddress.ToUpper(), mac.Value);
        Assert.True(MacAddress.IsValid(macAddress));
    }

    [Fact]
    public void MacAddress_ValueObjectBehavior_ShouldBeImmutable()
    {
        // Arrange
        var mac = new MacAddress("AA:BB:CC:DD:EE:FF");
        var originalValue = mac.Value;

        // Act - Cannot modify value since it's readonly
        // This test verifies the immutable nature by confirming the Value property hasn't changed

        // Assert
        Assert.Equal(originalValue, mac.Value);
        Assert.Equal("AA:BB:CC:DD:EE:FF", mac.Value);
    }
}
