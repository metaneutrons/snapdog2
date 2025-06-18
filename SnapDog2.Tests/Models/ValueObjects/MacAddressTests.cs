using SnapDog2.Core.Models.ValueObjects;
using Xunit;

namespace SnapDog2.Tests.Models.ValueObjects;

/// <summary>
/// Unit tests for the MacAddress value object.
/// Tests MAC address validation, parsing, formatting, equality, and type safety.
/// </summary>
public class MacAddressTests
{
    [Theory]
    [InlineData("AA:BB:CC:DD:EE:FF")]
    [InlineData("00:11:22:33:44:55")]
    [InlineData("FF:FF:FF:FF:FF:FF")]
    [InlineData("00:00:00:00:00:00")]
    [InlineData("12:34:56:78:9A:BC")]
    public void Constructor_WithValidMacAddress_ShouldCreateMacAddress(string validMacAddress)
    {
        // Act
        var macAddress = new MacAddress(validMacAddress);

        // Assert
        Assert.Equal(validMacAddress.ToUpperInvariant(), macAddress.ToString());
        Assert.Equal(validMacAddress.ToUpperInvariant(), macAddress.Value);
    }

    [Theory]
    [InlineData("aa:bb:cc:dd:ee:ff", "AA:BB:CC:DD:EE:FF")]
    [InlineData("AA-BB-CC-DD-EE-FF", "AA:BB:CC:DD:EE:FF")]
    [InlineData("aa-bb-cc-dd-ee-ff", "AA:BB:CC:DD:EE:FF")]
    public void Constructor_WithDifferentFormats_ShouldNormalizeToColonSeparatedUppercase(string input, string expected)
    {
        // Act
        var macAddress = new MacAddress(input);

        // Assert
        Assert.Equal(expected, macAddress.ToString());
        Assert.Equal(expected, macAddress.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Constructor_WithInvalidInput_ShouldThrowArgumentException(string? invalidMacAddress)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new MacAddress(invalidMacAddress!));
        Assert.Contains("MAC address cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("AA:BB:CC:DD:EE")] // Too short
    [InlineData("AA:BB:CC:DD:EE:FF:GG")] // Too long
    [InlineData("GG:BB:CC:DD:EE:FF")] // Invalid hex
    [InlineData("AA:BB:CC:DD:EE:ZZ")] // Invalid hex
    [InlineData("AA.BB.CC.DD.EE.FF")] // Wrong separator
    [InlineData("AABBCCDDEEF")] // Too short without separators
    [InlineData("AABBCCDDEEFFF")] // Too long without separators
    [InlineData("not-a-mac-address")] // Invalid format
    public void Constructor_WithInvalidMacFormat_ShouldThrowArgumentException(string invalidMacAddress)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new MacAddress(invalidMacAddress));
        Assert.Contains("Invalid MAC address format", exception.Message);
    }

    [Theory]
    [InlineData("AA:BB:CC:DD:EE:FF", true)]
    [InlineData("aa:bb:cc:dd:ee:ff", true)]
    [InlineData("AA-BB-CC-DD-EE-FF", true)]
    [InlineData("00:11:22:33:44:55", true)]
    [InlineData("AA:BB:CC:DD:EE", false)]
    [InlineData("GG:BB:CC:DD:EE:FF", false)]
    [InlineData("not-a-mac", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_WithDifferentInputs_ShouldReturnCorrectValue(string? macAddress, bool expectedValid)
    {
        // Act & Assert
        Assert.Equal(expectedValid, MacAddress.IsValid(macAddress));
    }

    [Theory]
    [InlineData("AA:BB:CC:DD:EE:FF")]
    [InlineData("aa:bb:cc:dd:ee:ff")]
    [InlineData("AA-BB-CC-DD-EE-FF")]
    public void Parse_WithValidMacAddress_ShouldReturnMacAddress(string validMacAddress)
    {
        // Act
        var macAddress = MacAddress.Parse(validMacAddress);

        // Assert
        Assert.NotNull(macAddress.Value);
        Assert.Contains(":", macAddress.ToString());
        Assert.Equal(macAddress.ToString().ToUpperInvariant(), macAddress.ToString());
    }

    [Theory]
    [InlineData("AA:BB:CC:DD:EE")]
    [InlineData("GG:BB:CC:DD:EE:FF")]
    [InlineData("not-a-mac")]
    [InlineData("")]
    public void Parse_WithInvalidMacAddress_ShouldThrowArgumentException(string invalidMacAddress)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => MacAddress.Parse(invalidMacAddress));
    }

    [Theory]
    [InlineData("AA:BB:CC:DD:EE:FF", true)]
    [InlineData("aa:bb:cc:dd:ee:ff", true)]
    [InlineData("AA-BB-CC-DD-EE-FF", true)]
    [InlineData("GG:BB:CC:DD:EE:FF", false)]
    [InlineData("AA:BB:CC:DD:EE", false)]
    [InlineData("not-a-mac", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TryParse_WithDifferentInputs_ShouldReturnCorrectResult(string? macAddress, bool expectedSuccess)
    {
        // Act
        var success = MacAddress.TryParse(macAddress, out var result);

        // Assert
        Assert.Equal(expectedSuccess, success);
        if (expectedSuccess)
        {
            Assert.NotNull(result.Value);
            Assert.Contains(":", result.ToString());
        }
        else
        {
            Assert.Equal(default, result);
        }
    }

    [Fact]
    public void Equals_WithSameMacAddress_ShouldReturnTrue()
    {
        // Arrange
        var mac1 = new MacAddress("AA:BB:CC:DD:EE:FF");
        var mac2 = new MacAddress("aa:bb:cc:dd:ee:ff"); // Different case

        // Act & Assert
        Assert.True(mac1.Equals(mac2));
        Assert.True(mac1 == mac2);
        Assert.False(mac1 != mac2);
        Assert.Equal(mac1.GetHashCode(), mac2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentMacAddress_ShouldReturnFalse()
    {
        // Arrange
        var mac1 = new MacAddress("AA:BB:CC:DD:EE:FF");
        var mac2 = new MacAddress("AA:BB:CC:DD:EE:FE");

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
        var obj = "AA:BB:CC:DD:EE:FF";

        // Act & Assert
        Assert.True(mac.Equals(obj));
    }

    [Fact]
    public void Equals_WithNullObject_ShouldReturnFalse()
    {
        // Arrange
        var mac = new MacAddress("AA:BB:CC:DD:EE:FF");

        // Act & Assert
        Assert.False(mac.Equals((object?)null));
    }

    [Fact]
    public void ImplicitConversion_FromString_ShouldWork()
    {
        // Act
        MacAddress mac = "AA:BB:CC:DD:EE:FF";

        // Assert
        Assert.Equal("AA:BB:CC:DD:EE:FF", mac.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldWork()
    {
        // Arrange
        var mac = new MacAddress("AA:BB:CC:DD:EE:FF");

        // Act
        string macString = mac;

        // Assert
        Assert.Equal("AA:BB:CC:DD:EE:FF", macString);
    }

    [Fact]
    public void GetHashCode_WithSameMacAddress_ShouldReturnSameHashCode()
    {
        // Arrange
        var mac1 = new MacAddress("AA:BB:CC:DD:EE:FF");
        var mac2 = new MacAddress("aa:bb:cc:dd:ee:ff");

        // Act & Assert
        Assert.Equal(mac1.GetHashCode(), mac2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnColonSeparatedUppercase()
    {
        // Arrange
        var mac = new MacAddress("aa-bb-cc-dd-ee-ff");

        // Act
        var result = mac.ToString();

        // Assert
        Assert.Equal("AA:BB:CC:DD:EE:FF", result);
    }

    [Fact]
    public void ValueObject_Immutability_ShouldBeImmutable()
    {
        // Arrange
        var originalMac = new MacAddress("AA:BB:CC:DD:EE:FF");
        var originalValue = originalMac.Value;

        // Act - Try to access and use the MAC address in various ways
        var stringValue = originalMac.ToString();
        var hashCode = originalMac.GetHashCode();

        // Assert - Original should remain unchanged
        Assert.Equal(originalValue, originalMac.Value);
        Assert.Equal("AA:BB:CC:DD:EE:FF", originalMac.ToString());
    }

    [Fact]
    public void MacAddress_WithComplexScenario_ShouldWorkCorrectly()
    {
        // Arrange - Test various MAC address operations
        var customMac = new MacAddress("aa-bb-cc-dd-ee-ff");
        var parsedMac = MacAddress.Parse("AA:BB:CC:DD:EE:FF");
        var anotherMac = new MacAddress("00:11:22:33:44:55");

        // Act & Assert
        Assert.Equal("AA:BB:CC:DD:EE:FF", customMac.ToString());
        Assert.Equal("AA:BB:CC:DD:EE:FF", parsedMac.ToString());
        Assert.Equal(customMac, parsedMac);

        // Test that they're different where expected
        Assert.NotEqual(customMac, anotherMac);

        // Test conversion roundtrip
        string macString = customMac;
        MacAddress convertedBack = macString;
        Assert.Equal(customMac, convertedBack);
    }

    [Theory]
    [InlineData("AA:BB:CC:DD:EE:FF", "AA:BB:CC:DD:EE:FF", true)]
    [InlineData("AA:BB:CC:DD:EE:FF", "aa:bb:cc:dd:ee:ff", true)]
    [InlineData("AA:BB:CC:DD:EE:FF", "AA-BB-CC-DD-EE-FF", true)]
    [InlineData("AA:BB:CC:DD:EE:FF", "AA:BB:CC:DD:EE:FE", false)]
    public void OperatorEquals_WithDifferentComparisons_ShouldReturnCorrectResult(
        string mac1String,
        string mac2String,
        bool expectedEqual
    )
    {
        // Arrange
        var mac1 = new MacAddress(mac1String);
        var mac2 = new MacAddress(mac2String);

        // Act & Assert
        Assert.Equal(expectedEqual, mac1 == mac2);
        Assert.Equal(!expectedEqual, mac1 != mac2);
    }

    [Fact]
    public void MacAddress_StructBehavior_ShouldBehaveAsValueType()
    {
        // Arrange
        var mac1 = new MacAddress("AA:BB:CC:DD:EE:FF");
        var mac2 = mac1; // This should copy the struct

        // Act - Since it's a struct, mac2 should be a copy
        var areEqual = mac1.Equals(mac2);
        var hashCodesEqual = mac1.GetHashCode() == mac2.GetHashCode();

        // Assert
        Assert.True(areEqual);
        Assert.True(hashCodesEqual);
        Assert.Equal(mac1.ToString(), mac2.ToString());
    }

    [Theory]
    [InlineData("00:50:56:00:00:01")] // VMware
    [InlineData("08:00:27:00:00:01")] // VirtualBox
    [InlineData("00:15:5D:00:00:01")] // Hyper-V
    [InlineData("52:54:00:00:00:01")] // QEMU/KVM
    public void MacAddress_WithVirtualMachineAddresses_ShouldWorkCorrectly(string vmMacAddress)
    {
        // Act
        var mac = new MacAddress(vmMacAddress);

        // Assert
        Assert.Equal(vmMacAddress.ToUpperInvariant(), mac.ToString());
        Assert.Equal(vmMacAddress.ToUpperInvariant(), mac.Value);
    }

    [Fact]
    public void MacAddress_CaseInsensitiveComparison_ShouldWork()
    {
        // Arrange
        var mac1 = new MacAddress("aa:bb:cc:dd:ee:ff");
        var mac2 = new MacAddress("AA:BB:CC:DD:EE:FF");
        var mac3 = new MacAddress("Aa:Bb:Cc:Dd:Ee:Ff");

        // Act & Assert
        Assert.Equal(mac1, mac2);
        Assert.Equal(mac2, mac3);
        Assert.Equal(mac1, mac3);
        Assert.True(mac1 == mac2);
        Assert.True(mac2 == mac3);
        Assert.True(mac1 == mac3);
    }

    [Fact]
    public void MacAddress_WithSpecialAddresses_ShouldWorkCorrectly()
    {
        // Arrange & Act
        var broadcastMac = new MacAddress("FF:FF:FF:FF:FF:FF");
        var zeroMac = new MacAddress("00:00:00:00:00:00");
        var multicastMac = new MacAddress("01:00:5E:00:00:01");

        // Assert
        Assert.Equal("FF:FF:FF:FF:FF:FF", broadcastMac.ToString());
        Assert.Equal("00:00:00:00:00:00", zeroMac.ToString());
        Assert.Equal("01:00:5E:00:00:01", multicastMac.ToString());

        Assert.NotEqual(broadcastMac, zeroMac);
        Assert.NotEqual(broadcastMac, multicastMac);
        Assert.NotEqual(zeroMac, multicastMac);
    }

    [Theory]
    [InlineData("aa:bb:cc:dd:ee:ff")]
    [InlineData("AA-BB-CC-DD-EE-FF")]
    [InlineData("Aa:Bb:Cc:Dd:Ee:Ff")]
    [InlineData("aA-bB-cC-dD-eE-fF")]
    public void MacAddress_NormalizationConsistency_ShouldAlwaysProduceSameOutput(string input)
    {
        // Act
        var mac = new MacAddress(input);

        // Assert
        Assert.Equal("AA:BB:CC:DD:EE:FF", mac.ToString());
        Assert.Equal("AA:BB:CC:DD:EE:FF", mac.Value);
    }

    [Fact]
    public void MacAddress_DefaultValue_ShouldBeHandledCorrectly()
    {
        // Arrange
        var defaultMac = default(MacAddress);

        // Act & Assert
        Assert.Null(defaultMac.Value);
        Assert.Null(defaultMac.ToString());
    }
}
