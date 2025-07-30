using FluentAssertions;
using SnapDog2.Infrastructure.Services.Models;
using System;
using Xunit;

namespace SnapDog2.Tests.Unit.Services;

/// <summary>
/// Comprehensive unit tests for KnxDptConverter covering all DPT (Data Point Type) conversions
/// with extensive edge case testing and precision validation.
/// </summary>
public class KnxDptConverterTests
{
    #region DPT 1.001 (Boolean/Switch) Tests

    [Theory]
    [InlineData(true, new byte[] { 1 })]
    [InlineData(false, new byte[] { 0 })]
    public void BooleanToDpt1001_WithValidValues_ShouldReturnCorrectBytes(bool value, byte[] expected)
    {
        // Act
        var result = KnxDptConverter.BooleanToDpt1001(value);

        // Assert
        result.Should().Equal(expected);
    }

    [Theory]
    [InlineData(new byte[] { 1 }, true)]
    [InlineData(new byte[] { 0 }, false)]
    [InlineData(new byte[] { 255 }, true)] // Any non-zero value should be true
    [InlineData(new byte[] { 42 }, true)]
    public void Dpt1001ToBoolean_WithValidBytes_ShouldReturnCorrectValue(byte[] bytes, bool expected)
    {
        // Act
        var result = KnxDptConverter.Dpt1001ToBoolean(bytes);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Dpt1001ToBoolean_WithEmptyOrNullArray_ShouldReturnFalse()
    {
        // Arrange
        var emptyBytes = Array.Empty<byte>();
        byte[] nullBytes = null!;

        // Act
        var resultEmpty = KnxDptConverter.Dpt1001ToBoolean(emptyBytes);
        var resultNull = KnxDptConverter.Dpt1001ToBoolean(nullBytes);

        // Assert
        resultEmpty.Should().BeFalse();
        resultNull.Should().BeFalse();
    }

    #endregion

    #region DPT 5.001 (Scaling/Percentage) Tests

    [Theory]
    [InlineData(0, new byte[] { 0 })]
    [InlineData(50, new byte[] { 128 })]
    [InlineData(100, new byte[] { 255 })]
    [InlineData(25, new byte[] { 64 })]
    [InlineData(75, new byte[] { 191 })]
    public void PercentToDpt5001_WithValidPercentages_ShouldReturnCorrectBytes(int percent, byte[] expected)
    {
        // Act
        var result = KnxDptConverter.PercentToDpt5001(percent);

        // Assert
        result.Should().Equal(expected);
    }

    [Theory]
    [InlineData(new byte[] { 0 }, 0)]
    [InlineData(new byte[] { 128 }, 50)]
    [InlineData(new byte[] { 255 }, 100)]
    [InlineData(new byte[] { 64 }, 25)]
    [InlineData(new byte[] { 191 }, 75)]
    public void Dpt5001ToPercent_WithValidBytes_ShouldReturnCorrectPercentage(byte[] bytes, int expected)
    {
        // Act
        var result = KnxDptConverter.Dpt5001ToPercent(bytes);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(101, 100)]
    public void PercentToDpt5001_WithOutOfRangePercentages_ShouldClampToValidRange(int invalidPercent, int expectedPercent)
    {
        // Act
        var resultBytes = KnxDptConverter.PercentToDpt5001(invalidPercent);
        var result = KnxDptConverter.Dpt5001ToPercent(resultBytes);

        // Assert
        result.Should().Be(expectedPercent);
    }

    [Fact]
    public void Dpt5001ToPercent_WithEmptyOrNullArray_ShouldReturnZero()
    {
        // Arrange
        var emptyBytes = Array.Empty<byte>();
        byte[] nullBytes = null!;

        // Act
        var resultEmpty = KnxDptConverter.Dpt5001ToPercent(emptyBytes);
        var resultNull = KnxDptConverter.Dpt5001ToPercent(nullBytes);

        // Assert
        resultEmpty.Should().Be(0);
        resultNull.Should().Be(0);
    }

    #endregion

    #region DPT 9.001 (2-byte Float) Tests

    [Theory]
    [InlineData(0.0f, new byte[] { 0, 0 })]
    [InlineData(20.48f, new byte[] { 0x08, 0x00 })]
    [InlineData(-30.72f, new byte[] { 0x88, 0x00 })]
    public void FloatToDpt9001_WithKnownValues_ShouldReturnExpectedBytes(float value, byte[] expected)
    {
        // Act
        var result = KnxDptConverter.FloatToDpt9001(value);

        // Assert
        result.Should().Equal(expected, because: "the conversion should be precise for these known values");
    }

    [Theory]
    [InlineData(new byte[] { 0, 0 }, 0.0f)]
    [InlineData(new byte[] { 0x08, 0x00 }, 20.48f)]
    [InlineData(new byte[] { 0x88, 0x00 }, -30.72f)]
    public void Dpt9001ToFloat_WithKnownBytes_ShouldReturnExpectedValues(byte[] bytes, float expected)
    {
        // Act
        var result = KnxDptConverter.Dpt9001ToFloat(bytes);

        // Assert
        result.Should().BeApproximately(expected, 0.01f);
    }

    #endregion
}