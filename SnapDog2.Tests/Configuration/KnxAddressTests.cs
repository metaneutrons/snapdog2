using SnapDog2.Core.Configuration;
using Xunit;

namespace SnapDog2.Tests.Configuration;

/// <summary>
/// Tests for KnxAddress value object and KnxAddressConverter functionality.
/// Validates KNX address parsing, conversion, and validation.
/// </summary>
public class KnxAddressTests
{
    [Fact]
    public void KnxAddress_Constructor_ShouldCreateValidAddress()
    {
        // Arrange & Act
        var address = new KnxAddress(2, 1, 1);

        // Assert
        Assert.Equal(2, address.Main);
        Assert.Equal(1, address.Middle);
        Assert.Equal(1, address.Sub);
    }

    [Theory]
    [InlineData(-1, 1, 1)]
    [InlineData(32, 1, 1)]
    [InlineData(2, -1, 1)]
    [InlineData(2, 8, 1)]
    [InlineData(2, 1, -1)]
    [InlineData(2, 1, 256)]
    public void KnxAddress_Constructor_ShouldThrowForInvalidValues(int main, int middle, int sub)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new KnxAddress(main, middle, sub));
    }

    [Theory]
    [InlineData("0/0/0", 0, 0, 0)]
    [InlineData("2/1/1", 2, 1, 1)]
    [InlineData("31/7/255", 31, 7, 255)]
    [InlineData("15/3/128", 15, 3, 128)]
    public void KnxAddress_Parse_ShouldParseValidAddresses(
        string input,
        int expectedMain,
        int expectedMiddle,
        int expectedSub
    )
    {
        // Arrange & Act
        var address = KnxAddress.Parse(input);

        // Assert
        Assert.Equal(expectedMain, address.Main);
        Assert.Equal(expectedMiddle, address.Middle);
        Assert.Equal(expectedSub, address.Sub);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("2/1")]
    [InlineData("2/1/1/1")]
    [InlineData("2/1/a")]
    [InlineData("a/1/1")]
    [InlineData("32/1/1")]
    [InlineData("2/8/1")]
    [InlineData("2/1/256")]
    [InlineData("-1/1/1")]
    public void KnxAddress_Parse_ShouldThrowForInvalidAddresses(string input)
    {
        // Arrange & Act & Assert
        Assert.Throws<FormatException>(() => KnxAddress.Parse(input));
    }

    [Theory]
    [InlineData("2/1/1", true)]
    [InlineData("0/0/0", true)]
    [InlineData("31/7/255", true)]
    [InlineData("", false)]
    [InlineData("2/1", false)]
    [InlineData("32/1/1", false)]
    [InlineData("2/8/1", false)]
    [InlineData("2/1/256", false)]
    [InlineData("a/1/1", false)]
    public void KnxAddress_TryParse_ShouldReturnCorrectResult(string input, bool expectedResult)
    {
        // Arrange & Act
        var result = KnxAddress.TryParse(input, out var address);

        // Assert
        Assert.Equal(expectedResult, result);
        if (expectedResult)
        {
            // For valid results, ensure the address is properly parsed
            Assert.True(result);
            // Don't check against default since "0/0/0" is valid but equals default
        }
        else
        {
            Assert.False(result);
            Assert.Equal(default(KnxAddress), address);
        }
    }

    [Fact]
    public void KnxAddress_ToString_ShouldReturnCorrectFormat()
    {
        // Arrange
        var address = new KnxAddress(2, 1, 1);

        // Act
        var result = address.ToString();

        // Assert
        Assert.Equal("2/1/1", result);
    }

    [Fact]
    public void KnxAddress_Equals_ShouldWorkCorrectly()
    {
        // Arrange
        var address1 = new KnxAddress(2, 1, 1);
        var address2 = new KnxAddress(2, 1, 1);
        var address3 = new KnxAddress(2, 1, 2);

        // Act & Assert
        Assert.Equal(address1, address2);
        Assert.NotEqual(address1, address3);
        Assert.True(address1 == address2);
        Assert.True(address1 != address3);
    }

    [Fact]
    public void KnxAddress_GetHashCode_ShouldBeConsistent()
    {
        // Arrange
        var address1 = new KnxAddress(2, 1, 1);
        var address2 = new KnxAddress(2, 1, 1);

        // Act & Assert
        Assert.Equal(address1.GetHashCode(), address2.GetHashCode());
    }

    [Fact]
    public void KnxAddress_ImplicitConversion_ShouldWork()
    {
        // Arrange & Act
        KnxAddress address = "2/1/1";
        string addressString = address;

        // Assert
        Assert.Equal(2, address.Main);
        Assert.Equal(1, address.Middle);
        Assert.Equal(1, address.Sub);
        Assert.Equal("2/1/1", addressString);
    }

    [Fact]
    public void KnxAddressConverter_ConvertFromString_ShouldHandleValidInput()
    {
        // Arrange & Act
        var result = KnxAddressConverter.ConvertFromString("2/1/1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("2/1/1", result.Value.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void KnxAddressConverter_ConvertFromString_ShouldReturnNullForEmptyInput(string? input)
    {
        // Arrange & Act
        var result = KnxAddressConverter.ConvertFromString(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void KnxAddressConverter_ConvertFromString_ShouldReturnNullForInvalidInput()
    {
        // Arrange & Act
        var result = KnxAddressConverter.ConvertFromString("invalid");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void KnxAddressConverter_ConvertToString_ShouldReturnCorrectString()
    {
        // Arrange
        var address = new KnxAddress(2, 1, 1);

        // Act
        var result = KnxAddressConverter.ConvertToString(address);

        // Assert
        Assert.Equal("2/1/1", result);
    }

    [Fact]
    public void KnxAddressConverter_ConvertToString_ShouldReturnEmptyForNull()
    {
        // Arrange & Act
        var result = KnxAddressConverter.ConvertToString(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("2/1/1", true)]
    [InlineData("0/0/0", true)]
    [InlineData("31/7/255", true)]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("invalid", false)]
    [InlineData("32/1/1", false)]
    [InlineData("2/8/1", false)]
    [InlineData("2/1/256", false)]
    public void KnxAddressConverter_IsValid_ShouldReturnCorrectResult(string? input, bool expectedValid)
    {
        // Arrange & Act
        var isValid = KnxAddressConverter.IsValid(input, out var errorMessage);

        // Assert
        Assert.Equal(expectedValid, isValid);
        if (expectedValid)
        {
            Assert.Equal(string.Empty, errorMessage);
        }
        else
        {
            Assert.NotEmpty(errorMessage);
        }
    }

    [Fact]
    public void KnxAddress_BoundaryValues_ShouldWork()
    {
        // Arrange & Act & Assert - Test boundary values
        var minAddress = new KnxAddress(0, 0, 0);
        Assert.Equal("0/0/0", minAddress.ToString());

        var maxAddress = new KnxAddress(31, 7, 255);
        Assert.Equal("31/7/255", maxAddress.ToString());

        // Test parsing boundary values
        Assert.True(KnxAddress.TryParse("0/0/0", out var parsedMin));
        Assert.Equal(minAddress, parsedMin);

        Assert.True(KnxAddress.TryParse("31/7/255", out var parsedMax));
        Assert.Equal(maxAddress, parsedMax);
    }
}
