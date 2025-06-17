using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SnapDog2.Core.Models.ValueObjects;

/// <summary>
/// Represents a network device MAC address with validation.
/// Immutable value object that ensures MAC address format correctness.
/// </summary>
public readonly struct MacAddress : IEquatable<MacAddress>
{
    private static readonly Regex MacAddressRegex = new(
        @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Gets the normalized MAC address string in uppercase with colon separators.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MacAddress"/> struct.
    /// </summary>
    /// <param name="macAddress">The MAC address string to validate and normalize.</param>
    /// <exception cref="ArgumentException">Thrown when the MAC address format is invalid.</exception>
    public MacAddress(string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            throw new ArgumentException("MAC address cannot be null or empty.", nameof(macAddress));
        }

        if (!IsValid(macAddress))
        {
            throw new ArgumentException(
                $"Invalid MAC address format: '{macAddress}'. Expected format: 'XX:XX:XX:XX:XX:XX' or 'XX-XX-XX-XX-XX-XX'.",
                nameof(macAddress)
            );
        }

        // Normalize to uppercase with colon separators
        Value = Normalize(macAddress);
    }

    /// <summary>
    /// Validates if a string represents a valid MAC address.
    /// </summary>
    /// <param name="macAddress">The MAC address string to validate.</param>
    /// <returns>True if the MAC address is valid; otherwise, false.</returns>
    public static bool IsValid(string? macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            return false;
        }

        return MacAddressRegex.IsMatch(macAddress);
    }

    /// <summary>
    /// Parses a MAC address string and returns a <see cref="MacAddress"/> instance.
    /// </summary>
    /// <param name="macAddress">The MAC address string to parse.</param>
    /// <returns>A <see cref="MacAddress"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the MAC address format is invalid.</exception>
    public static MacAddress Parse(string macAddress)
    {
        return new MacAddress(macAddress);
    }

    /// <summary>
    /// Tries to parse a MAC address string and returns a <see cref="MacAddress"/> instance.
    /// </summary>
    /// <param name="macAddress">The MAC address string to parse.</param>
    /// <param name="result">The parsed <see cref="MacAddress"/> if successful.</param>
    /// <returns>True if parsing was successful; otherwise, false.</returns>
    public static bool TryParse(string? macAddress, out MacAddress result)
    {
        result = default;

        if (!IsValid(macAddress))
        {
            return false;
        }

        try
        {
            result = new MacAddress(macAddress!);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Normalizes a MAC address to uppercase with colon separators.
    /// </summary>
    /// <param name="macAddress">The MAC address to normalize.</param>
    /// <returns>The normalized MAC address string.</returns>
    private static string Normalize(string macAddress)
    {
        // Remove any separators and convert to uppercase
        var cleaned = macAddress.Replace(":", "").Replace("-", "").ToUpper(CultureInfo.InvariantCulture);

        // Add colon separators
        return string.Join(
            ":",
            cleaned.Substring(0, 2),
            cleaned.Substring(2, 2),
            cleaned.Substring(4, 2),
            cleaned.Substring(6, 2),
            cleaned.Substring(8, 2),
            cleaned.Substring(10, 2)
        );
    }

    /// <summary>
    /// Returns the string representation of the MAC address.
    /// </summary>
    /// <returns>The normalized MAC address string.</returns>
    public override string ToString() => Value;

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="MacAddress"/>.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if the objects are equal; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is MacAddress other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="MacAddress"/> is equal to the current instance.
    /// </summary>
    /// <param name="other">The <see cref="MacAddress"/> to compare.</param>
    /// <returns>True if the MAC addresses are equal; otherwise, false.</returns>
    public bool Equals(MacAddress other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the hash code for the current <see cref="MacAddress"/>.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => Value?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;

    /// <summary>
    /// Determines whether two <see cref="MacAddress"/> instances are equal.
    /// </summary>
    /// <param name="left">The first MAC address.</param>
    /// <param name="right">The second MAC address.</param>
    /// <returns>True if the MAC addresses are equal; otherwise, false.</returns>
    public static bool operator ==(MacAddress left, MacAddress right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="MacAddress"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first MAC address.</param>
    /// <param name="right">The second MAC address.</param>
    /// <returns>True if the MAC addresses are not equal; otherwise, false.</returns>
    public static bool operator !=(MacAddress left, MacAddress right) => !left.Equals(right);

    /// <summary>
    /// Implicitly converts a string to a <see cref="MacAddress"/>.
    /// </summary>
    /// <param name="macAddress">The MAC address string.</param>
    /// <returns>A <see cref="MacAddress"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the MAC address format is invalid.</exception>
    public static implicit operator MacAddress(string macAddress) => new(macAddress);

    /// <summary>
    /// Implicitly converts a <see cref="MacAddress"/> to a string.
    /// </summary>
    /// <param name="macAddress">The MAC address.</param>
    /// <returns>The string representation of the MAC address.</returns>
    public static implicit operator string(MacAddress macAddress) => macAddress.Value;
}
