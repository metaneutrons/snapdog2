using System.Net;
using SnapDog2.Core.Models.ValueObjects;
using Xunit;

namespace SnapDog2.Tests.Models.ValueObjects;

/// <summary>
/// Unit tests for the IpAddress value object.
/// Tests IP address validation, parsing, equality, and type safety.
/// </summary>
public class IpAddressTests
{
    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("127.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("0.0.0.0")]
    public void Constructor_WithValidIPv4Address_ShouldCreateIpAddress(string validIpAddress)
    {
        // Act
        var ipAddress = new IpAddress(validIpAddress);

        // Assert
        Assert.Equal(validIpAddress, ipAddress.ToString());
        Assert.True(ipAddress.IsIPv4);
        Assert.False(ipAddress.IsIPv6);
        Assert.NotNull(ipAddress.Value);
    }

    [Theory]
    [InlineData("2001:0db8:85a3:0000:0000:8a2e:0370:7334")]
    [InlineData("2001:db8:85a3::8a2e:370:7334")]
    [InlineData("::1")]
    [InlineData("::")]
    [InlineData("2001:db8::1")]
    [InlineData("fe80::1%lo0")]
    public void Constructor_WithValidIPv6Address_ShouldCreateIpAddress(string validIpAddress)
    {
        // Act
        var ipAddress = new IpAddress(validIpAddress);

        // Assert
        Assert.True(ipAddress.IsIPv6);
        Assert.False(ipAddress.IsIPv4);
        Assert.NotNull(ipAddress.Value);
    }

    [Fact]
    public void Constructor_WithIPAddressInstance_ShouldCreateIpAddress()
    {
        // Arrange
        var systemIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        // Act
        var ipAddress = new IpAddress(systemIpAddress);

        // Assert
        Assert.Equal(systemIpAddress, ipAddress.Value);
        Assert.Equal("192.168.1.100", ipAddress.ToString());
        Assert.True(ipAddress.IsIPv4);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Constructor_WithInvalidInput_ShouldThrowArgumentException(string? invalidIpAddress)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new IpAddress(invalidIpAddress!));
        Assert.Contains("IP address cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("256.1.1.1")]
    [InlineData("192.168.1.")]
    [InlineData("192.168.1.1.1")]
    [InlineData("not-an-ip")]
    [InlineData("192.168.-1.1")]
    [InlineData("192.168.1.256")]
    public void Constructor_WithInvalidIPFormat_ShouldThrowArgumentException(string invalidIpAddress)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new IpAddress(invalidIpAddress));
        Assert.Contains("Invalid IP address format", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullIPAddressInstance_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new IpAddress((System.Net.IPAddress)null!));
    }

    [Theory]
    [InlineData("192.168.1.1", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("2001:db8::1", true)]
    [InlineData("256.1.1.1", false)]
    [InlineData("not-an-ip", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_WithDifferentInputs_ShouldReturnCorrectValue(string? ipAddress, bool expectedValid)
    {
        // Act & Assert
        Assert.Equal(expectedValid, IpAddress.IsValid(ipAddress));
    }

    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("2001:db8::1")]
    public void Parse_WithValidIpAddress_ShouldReturnIpAddress(string validIpAddress)
    {
        // Act
        var ipAddress = IpAddress.Parse(validIpAddress);

        // Assert
        Assert.Equal(validIpAddress, ipAddress.ToString());
        Assert.NotNull(ipAddress.Value);
    }

    [Theory]
    [InlineData("256.1.1.1")]
    [InlineData("not-an-ip")]
    [InlineData("")]
    public void Parse_WithInvalidIpAddress_ShouldThrowArgumentException(string invalidIpAddress)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => IpAddress.Parse(invalidIpAddress));
    }

    [Theory]
    [InlineData("192.168.1.1", true)]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("256.1.1.1", false)]
    [InlineData("not-an-ip", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TryParse_WithDifferentInputs_ShouldReturnCorrectResult(string? ipAddress, bool expectedSuccess)
    {
        // Act
        var success = IpAddress.TryParse(ipAddress, out var result);

        // Assert
        Assert.Equal(expectedSuccess, success);
        if (expectedSuccess)
        {
            Assert.Equal(ipAddress, result.ToString());
        }
        else
        {
            Assert.Equal(default, result);
        }
    }

    [Fact]
    public void Localhost_ShouldReturnCorrectIPv4LoopbackAddress()
    {
        // Act
        var localhost = IpAddress.Localhost;

        // Assert
        Assert.Equal("127.0.0.1", localhost.ToString());
        Assert.True(localhost.IsIPv4);
        Assert.True(localhost.IsLoopback);
    }

    [Fact]
    public void LocalhostIPv6_ShouldReturnCorrectIPv6LoopbackAddress()
    {
        // Act
        var localhost = IpAddress.LocalhostIPv6;

        // Assert
        Assert.Equal("::1", localhost.ToString());
        Assert.True(localhost.IsIPv6);
        Assert.True(localhost.IsLoopback);
    }

    [Fact]
    public void Any_ShouldReturnCorrectIPv4AnyAddress()
    {
        // Act
        var any = IpAddress.Any;

        // Assert
        Assert.Equal("0.0.0.0", any.ToString());
        Assert.True(any.IsIPv4);
        Assert.False(any.IsLoopback);
    }

    [Fact]
    public void IPv6Any_ShouldReturnCorrectIPv6AnyAddress()
    {
        // Act
        var any = IpAddress.IPv6Any;

        // Assert
        Assert.Equal("::", any.ToString());
        Assert.True(any.IsIPv6);
        Assert.False(any.IsLoopback);
    }

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("192.168.1.1", false)]
    [InlineData("10.0.0.1", false)]
    [InlineData("2001:db8::1", false)]
    public void IsLoopback_WithDifferentAddresses_ShouldReturnCorrectValue(string ipAddress, bool expectedLoopback)
    {
        // Arrange
        var ip = new IpAddress(ipAddress);

        // Act & Assert
        Assert.Equal(expectedLoopback, ip.IsLoopback);
    }

    [Fact]
    public void Equals_WithSameIpAddress_ShouldReturnTrue()
    {
        // Arrange
        var ip1 = new IpAddress("192.168.1.1");
        var ip2 = new IpAddress("192.168.1.1");

        // Act & Assert
        Assert.True(ip1.Equals(ip2));
        Assert.True(ip1 == ip2);
        Assert.False(ip1 != ip2);
        Assert.Equal(ip1.GetHashCode(), ip2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentIpAddress_ShouldReturnFalse()
    {
        // Arrange
        var ip1 = new IpAddress("192.168.1.1");
        var ip2 = new IpAddress("192.168.1.2");

        // Act & Assert
        Assert.False(ip1.Equals(ip2));
        Assert.False(ip1 == ip2);
        Assert.True(ip1 != ip2);
    }

    [Fact]
    public void Equals_WithNonIpAddressObject_ShouldReturnFalse()
    {
        // Arrange
        var ip = new IpAddress("192.168.1.1");
        var obj = "192.168.1.1";

        // Act & Assert
        Assert.True(ip.Equals(obj));
    }

    [Fact]
    public void Equals_WithNullObject_ShouldReturnFalse()
    {
        // Arrange
        var ip = new IpAddress("192.168.1.1");

        // Act & Assert
        Assert.False(ip.Equals((object?)null));
    }

    [Fact]
    public void ImplicitConversion_FromString_ShouldWork()
    {
        // Act
        IpAddress ip = "192.168.1.1";

        // Assert
        Assert.Equal("192.168.1.1", ip.ToString());
        Assert.True(ip.IsIPv4);
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldWork()
    {
        // Arrange
        var ip = new IpAddress("192.168.1.1");

        // Act
        string ipString = ip;

        // Assert
        Assert.Equal("192.168.1.1", ipString);
    }

    [Fact]
    public void ImplicitConversion_FromSystemIPAddress_ShouldWork()
    {
        // Arrange
        var systemIp = System.Net.IPAddress.Parse("192.168.1.1");

        // Act
        IpAddress ip = systemIp;

        // Assert
        Assert.Equal("192.168.1.1", ip.ToString());
        Assert.Equal(systemIp, ip.Value);
    }

    [Fact]
    public void ImplicitConversion_ToSystemIPAddress_ShouldWork()
    {
        // Arrange
        var ip = new IpAddress("192.168.1.1");

        // Act
        System.Net.IPAddress systemIp = ip;

        // Assert
        Assert.Equal("192.168.1.1", systemIp.ToString());
        Assert.Equal(ip.Value, systemIp);
    }

    [Fact]
    public void GetHashCode_WithSameIpAddress_ShouldReturnSameHashCode()
    {
        // Arrange
        var ip1 = new IpAddress("192.168.1.1");
        var ip2 = new IpAddress("192.168.1.1");

        // Act & Assert
        Assert.Equal(ip1.GetHashCode(), ip2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnStringRepresentation()
    {
        // Arrange
        var ip = new IpAddress("192.168.1.100");

        // Act
        var result = ip.ToString();

        // Assert
        Assert.Equal("192.168.1.100", result);
    }

    [Fact]
    public void ValueObject_Immutability_ShouldBeImmutable()
    {
        // Arrange
        var originalIp = new IpAddress("192.168.1.1");
        var originalValue = originalIp.Value;

        // Act - Try to access and use the IP address in various ways
        var stringValue = originalIp.ToString();
        var isIPv4 = originalIp.IsIPv4;
        var isLoopback = originalIp.IsLoopback;

        // Assert - Original should remain unchanged
        Assert.Equal(originalValue, originalIp.Value);
        Assert.Equal("192.168.1.1", originalIp.ToString());
        Assert.True(originalIp.IsIPv4);
    }

    [Fact]
    public void IpAddress_WithComplexScenario_ShouldWorkCorrectly()
    {
        // Arrange - Test various IP address operations
        var ipv4Local = IpAddress.Localhost;
        var ipv6Local = IpAddress.LocalhostIPv6;
        var customIp = new IpAddress("10.0.0.100");
        var parsedIp = IpAddress.Parse("172.16.1.50");

        // Act & Assert
        Assert.True(ipv4Local.IsLoopback);
        Assert.True(ipv6Local.IsLoopback);
        Assert.False(customIp.IsLoopback);
        Assert.False(parsedIp.IsLoopback);

        Assert.True(ipv4Local.IsIPv4);
        Assert.True(ipv6Local.IsIPv6);
        Assert.True(customIp.IsIPv4);
        Assert.True(parsedIp.IsIPv4);

        // Test that they're all different
        Assert.NotEqual(ipv4Local, ipv6Local);
        Assert.NotEqual(ipv4Local, customIp);
        Assert.NotEqual(customIp, parsedIp);

        // Test conversion roundtrip
        string ipString = customIp;
        IpAddress convertedBack = ipString;
        Assert.Equal(customIp, convertedBack);
    }

    [Theory]
    [InlineData("192.168.1.1", "192.168.1.1", true)]
    [InlineData("192.168.1.1", "192.168.1.2", false)]
    [InlineData("::1", "::1", true)]
    [InlineData("::1", "::2", false)]
    [InlineData("192.168.1.1", "::1", false)]
    public void OperatorEquals_WithDifferentComparisons_ShouldReturnCorrectResult(
        string ip1String,
        string ip2String,
        bool expectedEqual
    )
    {
        // Arrange
        var ip1 = new IpAddress(ip1String);
        var ip2 = new IpAddress(ip2String);

        // Act & Assert
        Assert.Equal(expectedEqual, ip1 == ip2);
        Assert.Equal(!expectedEqual, ip1 != ip2);
    }

    [Fact]
    public void IpAddress_StructBehavior_ShouldBehaveAsValueType()
    {
        // Arrange
        var ip1 = new IpAddress("192.168.1.1");
        var ip2 = ip1; // This should copy the struct

        // Act - Since it's a struct, ip2 should be a copy
        var areEqual = ip1.Equals(ip2);
        var hashCodesEqual = ip1.GetHashCode() == ip2.GetHashCode();

        // Assert
        Assert.True(areEqual);
        Assert.True(hashCodesEqual);
        Assert.Equal(ip1.ToString(), ip2.ToString());
    }
}
