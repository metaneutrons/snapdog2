using System;
using System.Net;

namespace SnapDog2.Core.Models.ValueObjects;

/// <summary>
/// Represents an IP address with validation and parsing capabilities.
/// Immutable value object that supports both IPv4 and IPv6 addresses.
/// </summary>
public readonly struct IpAddress : IEquatable<IpAddress>
{
    /// <summary>
    /// Gets the underlying .NET IPAddress instance.
    /// </summary>
    public IPAddress Value { get; }

    /// <summary>
    /// Gets a value indicating whether this is an IPv4 address.
    /// </summary>
    public bool IsIPv4 => Value.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;

    /// <summary>
    /// Gets a value indicating whether this is an IPv6 address.
    /// </summary>
    public bool IsIPv6 => Value.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6;

    /// <summary>
    /// Gets a value indicating whether this is a loopback address.
    /// </summary>
    public bool IsLoopback => IPAddress.IsLoopback(Value);

    /// <summary>
    /// Initializes a new instance of the <see cref="IpAddress"/> struct.
    /// </summary>
    /// <param name="ipAddress">The IP address string to validate and parse.</param>
    /// <exception cref="ArgumentException">Thrown when the IP address format is invalid.</exception>
    public IpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            throw new ArgumentException("IP address cannot be null or empty.", nameof(ipAddress));
        }

        if (!IPAddress.TryParse(ipAddress, out var parsedAddress))
        {
            throw new ArgumentException($"Invalid IP address format: '{ipAddress}'.", nameof(ipAddress));
        }

        Value = parsedAddress;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IpAddress"/> struct.
    /// </summary>
    /// <param name="ipAddress">The .NET IPAddress instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when the IP address is null.</exception>
    public IpAddress(IPAddress ipAddress)
    {
        Value = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
    }

    /// <summary>
    /// Validates if a string represents a valid IP address.
    /// </summary>
    /// <param name="ipAddress">The IP address string to validate.</param>
    /// <returns>True if the IP address is valid; otherwise, false.</returns>
    public static bool IsValid(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return false;
        }

        return IPAddress.TryParse(ipAddress, out _);
    }

    /// <summary>
    /// Parses an IP address string and returns an <see cref="IpAddress"/> instance.
    /// </summary>
    /// <param name="ipAddress">The IP address string to parse.</param>
    /// <returns>An <see cref="IpAddress"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the IP address format is invalid.</exception>
    public static IpAddress Parse(string ipAddress)
    {
        return new IpAddress(ipAddress);
    }

    /// <summary>
    /// Tries to parse an IP address string and returns an <see cref="IpAddress"/> instance.
    /// </summary>
    /// <param name="ipAddress">The IP address string to parse.</param>
    /// <param name="result">The parsed <see cref="IpAddress"/> if successful.</param>
    /// <returns>True if parsing was successful; otherwise, false.</returns>
    public static bool TryParse(string? ipAddress, out IpAddress result)
    {
        result = default;

        if (!IsValid(ipAddress))
        {
            return false;
        }

        try
        {
            result = new IpAddress(ipAddress!);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the localhost IP address (127.0.0.1).
    /// </summary>
    public static IpAddress Localhost => new(IPAddress.Loopback);

    /// <summary>
    /// Gets the IPv6 localhost address (::1).
    /// </summary>
    public static IpAddress LocalhostIPv6 => new(IPAddress.IPv6Loopback);

    /// <summary>
    /// Gets the "any" IP address (0.0.0.0) that binds to all available interfaces.
    /// </summary>
    public static IpAddress Any => new(IPAddress.Any);

    /// <summary>
    /// Gets the IPv6 "any" address (::) that binds to all available interfaces.
    /// </summary>
    public static IpAddress IPv6Any => new(IPAddress.IPv6Any);

    /// <summary>
    /// Returns the string representation of the IP address.
    /// </summary>
    /// <returns>The IP address string.</returns>
    public override string ToString() => Value.ToString();

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="IpAddress"/>.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if the objects are equal; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is IpAddress other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="IpAddress"/> is equal to the current instance.
    /// </summary>
    /// <param name="other">The <see cref="IpAddress"/> to compare.</param>
    /// <returns>True if the IP addresses are equal; otherwise, false.</returns>
    public bool Equals(IpAddress other) => Value.Equals(other.Value);

    /// <summary>
    /// Returns the hash code for the current <see cref="IpAddress"/>.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Determines whether two <see cref="IpAddress"/> instances are equal.
    /// </summary>
    /// <param name="left">The first IP address.</param>
    /// <param name="right">The second IP address.</param>
    /// <returns>True if the IP addresses are equal; otherwise, false.</returns>
    public static bool operator ==(IpAddress left, IpAddress right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="IpAddress"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first IP address.</param>
    /// <param name="right">The second IP address.</param>
    /// <returns>True if the IP addresses are not equal; otherwise, false.</returns>
    public static bool operator !=(IpAddress left, IpAddress right) => !left.Equals(right);

    /// <summary>
    /// Implicitly converts a string to an <see cref="IpAddress"/>.
    /// </summary>
    /// <param name="ipAddress">The IP address string.</param>
    /// <returns>An <see cref="IpAddress"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the IP address format is invalid.</exception>
    public static implicit operator IpAddress(string ipAddress) => new(ipAddress);

    /// <summary>
    /// Implicitly converts an <see cref="IpAddress"/> to a string.
    /// </summary>
    /// <param name="ipAddress">The IP address.</param>
    /// <returns>The string representation of the IP address.</returns>
    public static implicit operator string(IpAddress ipAddress) => ipAddress.ToString();

    /// <summary>
    /// Implicitly converts a .NET IPAddress to an <see cref="IpAddress"/>.
    /// </summary>
    /// <param name="ipAddress">The .NET IPAddress instance.</param>
    /// <returns>An <see cref="IpAddress"/> instance.</returns>
    public static implicit operator IpAddress(IPAddress ipAddress) => new(ipAddress);

    /// <summary>
    /// Implicitly converts an <see cref="IpAddress"/> to a .NET IPAddress.
    /// </summary>
    /// <param name="ipAddress">The IP address.</param>
    /// <returns>The .NET IPAddress instance.</returns>
    public static implicit operator IPAddress(IpAddress ipAddress) => ipAddress.Value;
}
