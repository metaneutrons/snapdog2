using System.Net;
using FluentValidation;
using SnapDog2.Core.Models.ValueObjects;

namespace SnapDog2.Core.Validation;

/// <summary>
/// FluentValidation validator for IpAddress value object.
/// Validates IP address format and business rules for both IPv4 and IPv6.
/// </summary>
public sealed class IpAddressValidator : AbstractValidator<IpAddress>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IpAddressValidator"/> class.
    /// </summary>
    public IpAddressValidator()
    {
        // Value validation - IP address format
        RuleFor(static x => x.Value)
            .NotNull()
            .WithMessage("IP address value is required.")
            .Must(BeValidIpAddress)
            .WithMessage("IP address must be a valid IPv4 or IPv6 address.");

        // Business rules
        RuleFor(static x => x)
            .Must(NotBeReservedAddress)
            .WithMessage("IP address cannot be a reserved system address.")
            .Must(NotBeMulticastAddress)
            .WithMessage("IP address cannot be a multicast address for individual clients.")
            .Must(BeAppropriateForNetworking)
            .WithMessage("IP address should be appropriate for network communication.");

        // IPv4 specific validations
        RuleFor(static x => x)
            .Must(BeValidIPv4Range)
            .WithMessage("IPv4 address is not in a valid range for client communication.")
            .When(static x => x.IsIPv4);

        // IPv6 specific validations
        RuleFor(static x => x)
            .Must(BeValidIPv6Type)
            .WithMessage("IPv6 address type is not appropriate for client communication.")
            .When(static x => x.IsIPv6);
    }

    /// <summary>
    /// Validates that the IP address is a valid format.
    /// </summary>
    /// <param name="ipAddress">The IP address to validate.</param>
    /// <returns>True if the IP address is valid; otherwise, false.</returns>
    private static bool BeValidIpAddress(IPAddress? ipAddress)
    {
        return ipAddress != null;
    }

    /// <summary>
    /// Validates that the IP address is not a reserved system address.
    /// </summary>
    /// <param name="ipAddress">The IP address to validate.</param>
    /// <returns>True if the IP address is not reserved; otherwise, false.</returns>
    private static bool NotBeReservedAddress(IpAddress ipAddress)
    {
        if (ipAddress.Value == null)
        {
            return false;
        }

        var address = ipAddress.Value;

        // Check for specific reserved addresses
        if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
        {
            return false; // 0.0.0.0 or ::
        }

        if (address.Equals(IPAddress.Broadcast))
        {
            return false; // 255.255.255.255
        }

        if (ipAddress.IsIPv4)
        {
            return NotBeReservedIPv4Address(address);
        }

        if (ipAddress.IsIPv6)
        {
            return NotBeReservedIPv6Address(address);
        }

        return true;
    }

    /// <summary>
    /// Validates that the IPv4 address is not in a reserved range.
    /// </summary>
    /// <param name="address">The IPv4 address to validate.</param>
    /// <returns>True if the address is not reserved; otherwise, false.</returns>
    private static bool NotBeReservedIPv4Address(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        var b1 = bytes[0];
        var b2 = bytes[1];

        // 0.0.0.0/8 - "This" network
        if (b1 == 0)
        {
            return false;
        }

        // 127.0.0.0/8 - Loopback (allow for development)
        if (b1 == 127)
        {
            return true; // Allow loopback for development scenarios
        }

        // 169.254.0.0/16 - Link-local
        if (b1 == 169 && b2 == 254)
        {
            return false;
        }

        // 224.0.0.0/4 - Multicast (handled separately)
        // 240.0.0.0/4 - Reserved for future use
        if (b1 >= 240)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that the IPv6 address is not in a reserved range.
    /// </summary>
    /// <param name="address">The IPv6 address to validate.</param>
    /// <returns>True if the address is not reserved; otherwise, false.</returns>
    private static bool NotBeReservedIPv6Address(IPAddress address)
    {
        var bytes = address.GetAddressBytes();

        // ::1 - Loopback (allow for development)
        if (address.Equals(IPAddress.IPv6Loopback))
        {
            return true; // Allow loopback for development scenarios
        }

        // :: - Unspecified address
        if (address.Equals(IPAddress.IPv6Any))
        {
            return false;
        }

        // fe80::/10 - Link-local unicast (might be acceptable for local networks)
        if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80)
        {
            return true; // Allow link-local for local network scenarios
        }

        // fc00::/7 - Unique local unicast (private IPv6)
        if ((bytes[0] & 0xfe) == 0xfc)
        {
            return true; // Allow unique local addresses
        }

        return true; // Most other IPv6 addresses are acceptable
    }

    /// <summary>
    /// Validates that the IP address is not a multicast address.
    /// </summary>
    /// <param name="ipAddress">The IP address to validate.</param>
    /// <returns>True if the IP address is not multicast; otherwise, false.</returns>
    private static bool NotBeMulticastAddress(IpAddress ipAddress)
    {
        if (ipAddress.Value == null)
        {
            return false;
        }

        if (ipAddress.IsIPv4)
        {
            var bytes = ipAddress.Value.GetAddressBytes();
            // IPv4 multicast range: 224.0.0.0 to 239.255.255.255
            return bytes[0] < 224 || bytes[0] > 239;
        }

        if (ipAddress.IsIPv6)
        {
            // IPv6 multicast addresses start with FF
            return !ipAddress.Value.IsIPv6Multicast;
        }

        return true;
    }

    /// <summary>
    /// Validates that the IP address is appropriate for networking.
    /// </summary>
    /// <param name="ipAddress">The IP address to validate.</param>
    /// <returns>True if the IP address is appropriate; otherwise, false.</returns>
    private static bool BeAppropriateForNetworking(IpAddress ipAddress)
    {
        if (ipAddress.Value == null)
        {
            return false;
        }

        // Allow loopback for development
        if (ipAddress.IsLoopback)
        {
            return true;
        }

        // Check for valid unicast addresses
        if (ipAddress.IsIPv4)
        {
            return BeValidUnicastIPv4(ipAddress.Value);
        }

        if (ipAddress.IsIPv6)
        {
            return BeValidUnicastIPv6(ipAddress.Value);
        }

        return false;
    }

    /// <summary>
    /// Validates that the IPv4 address is in a valid range for client communication.
    /// </summary>
    /// <param name="ipAddress">The IP address to validate.</param>
    /// <returns>True if the IPv4 address is in a valid range; otherwise, false.</returns>
    private static bool BeValidIPv4Range(IpAddress ipAddress)
    {
        if (ipAddress.Value == null || !ipAddress.IsIPv4)
        {
            return false;
        }

        var bytes = ipAddress.Value.GetAddressBytes();
        var b1 = bytes[0];
        var b2 = bytes[1];

        // Allow private networks
        // 10.0.0.0/8
        if (b1 == 10)
        {
            return true;
        }

        // 172.16.0.0/12
        if (b1 == 172 && b2 >= 16 && b2 <= 31)
        {
            return true;
        }

        // 192.168.0.0/16
        if (b1 == 192 && b2 == 168)
        {
            return true;
        }

        // 127.0.0.0/8 - Loopback
        if (b1 == 127)
        {
            return true;
        }

        // Public IPv4 addresses (class A, B, C unicast)
        if (b1 >= 1 && b1 <= 223 && b1 != 127)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Validates that the IPv6 address type is appropriate for client communication.
    /// </summary>
    /// <param name="ipAddress">The IP address to validate.</param>
    /// <returns>True if the IPv6 address type is appropriate; otherwise, false.</returns>
    private static bool BeValidIPv6Type(IpAddress ipAddress)
    {
        if (ipAddress.Value == null || !ipAddress.IsIPv6)
        {
            return false;
        }

        var address = ipAddress.Value;

        // Allow loopback
        if (address.Equals(IPAddress.IPv6Loopback))
        {
            return true;
        }

        // Reject unspecified address
        if (address.Equals(IPAddress.IPv6Any))
        {
            return false;
        }

        // Reject multicast
        if (address.IsIPv6Multicast)
        {
            return false;
        }

        // Most other IPv6 unicast addresses are acceptable
        return true;
    }

    /// <summary>
    /// Validates that the IPv4 address is a valid unicast address.
    /// </summary>
    /// <param name="address">The IPv4 address to validate.</param>
    /// <returns>True if the address is valid unicast; otherwise, false.</returns>
    private static bool BeValidUnicastIPv4(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        var b1 = bytes[0];

        // Class A, B, C unicast ranges (excluding multicast and reserved)
        return b1 >= 1 && b1 <= 223;
    }

    /// <summary>
    /// Validates that the IPv6 address is a valid unicast address.
    /// </summary>
    /// <param name="address">The IPv6 address to validate.</param>
    /// <returns>True if the address is valid unicast; otherwise, false.</returns>
    private static bool BeValidUnicastIPv6(IPAddress address)
    {
        // IPv6 unicast: not multicast, not unspecified
        return !address.IsIPv6Multicast && !address.Equals(IPAddress.IPv6Any);
    }
}
