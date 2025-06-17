using FluentValidation;
using SnapDog2.Core.Models.ValueObjects;

namespace SnapDog2.Core.Validation;

/// <summary>
/// FluentValidation validator for MacAddress value object.
/// Validates MAC address format and business rules.
/// </summary>
public sealed class MacAddressValidator : AbstractValidator<MacAddress>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MacAddressValidator"/> class.
    /// </summary>
    public MacAddressValidator()
    {
        // Value validation - MAC address format
        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("MAC address value is required.")
            .Matches(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$")
            .WithMessage(
                "MAC address must be in the format XX:XX:XX:XX:XX:XX or XX-XX-XX-XX-XX-XX with hexadecimal digits."
            )
            .Must(BeNormalizedFormat)
            .WithMessage("MAC address should be normalized to uppercase with colon separators.")
            .Must(NotBeReservedAddress)
            .WithMessage("MAC address cannot be a reserved or special address.")
            .Must(NotBeBroadcastAddress)
            .WithMessage("MAC address cannot be the broadcast address (FF:FF:FF:FF:FF:FF).")
            .Must(NotBeNullAddress)
            .WithMessage("MAC address cannot be the null address (00:00:00:00:00:00).");

        // Business rules
        RuleFor(x => x)
            .Must(BeUnicastAddress)
            .WithMessage("MAC address should be a unicast address for individual devices.")
            .Must(NotBeVirtualMachineAddress)
            .WithMessage("MAC address appears to be from a virtual machine, which may cause conflicts.");
    }

    /// <summary>
    /// Validates that the MAC address is in normalized format (uppercase with colons).
    /// </summary>
    /// <param name="macAddress">The MAC address value to validate.</param>
    /// <returns>True if the MAC address is normalized; otherwise, false.</returns>
    private static bool BeNormalizedFormat(string macAddress)
    {
        if (string.IsNullOrEmpty(macAddress))
        {
            return false;
        }

        // Should be uppercase with colon separators
        return macAddress.Contains(':') && macAddress == macAddress.ToUpperInvariant();
    }

    /// <summary>
    /// Validates that the MAC address is not a reserved address.
    /// </summary>
    /// <param name="macAddress">The MAC address value to validate.</param>
    /// <returns>True if the MAC address is not reserved; otherwise, false.</returns>
    private static bool NotBeReservedAddress(string macAddress)
    {
        if (string.IsNullOrEmpty(macAddress))
        {
            return false;
        }

        // Remove separators and convert to uppercase for comparison
        var cleanAddress = macAddress.Replace(":", "").Replace("-", "").ToUpperInvariant();

        // Reserved addresses that should not be used for actual devices
        var reservedAddresses = new[]
        {
            "000000000000", // Null address
            "FFFFFFFFFFFF", // Broadcast address
            "010000000000", // Multicast base
            "333300000000", // IPv6 multicast base
        };

        return !reservedAddresses.Contains(cleanAddress);
    }

    /// <summary>
    /// Validates that the MAC address is not the broadcast address.
    /// </summary>
    /// <param name="macAddress">The MAC address value to validate.</param>
    /// <returns>True if the MAC address is not the broadcast address; otherwise, false.</returns>
    private static bool NotBeBroadcastAddress(string macAddress)
    {
        if (string.IsNullOrEmpty(macAddress))
        {
            return false;
        }

        var cleanAddress = macAddress.Replace(":", "").Replace("-", "").ToUpperInvariant();
        return cleanAddress != "FFFFFFFFFFFF";
    }

    /// <summary>
    /// Validates that the MAC address is not the null address.
    /// </summary>
    /// <param name="macAddress">The MAC address value to validate.</param>
    /// <returns>True if the MAC address is not the null address; otherwise, false.</returns>
    private static bool NotBeNullAddress(string macAddress)
    {
        if (string.IsNullOrEmpty(macAddress))
        {
            return false;
        }

        var cleanAddress = macAddress.Replace(":", "").Replace("-", "").ToUpperInvariant();
        return cleanAddress != "000000000000";
    }

    /// <summary>
    /// Validates that the MAC address is a unicast address (for individual devices).
    /// </summary>
    /// <param name="macAddress">The MAC address to validate.</param>
    /// <returns>True if the MAC address is unicast; otherwise, false.</returns>
    private static bool BeUnicastAddress(MacAddress macAddress)
    {
        if (string.IsNullOrEmpty(macAddress.Value))
        {
            return false;
        }

        // Get the first octet
        var firstOctet = macAddress.Value.Substring(0, 2);
        if (int.TryParse(firstOctet, System.Globalization.NumberStyles.HexNumber, null, out var value))
        {
            // Check if the least significant bit (LSB) of the first octet is 0 (unicast)
            // If LSB is 1, it's a multicast address
            return (value & 1) == 0;
        }

        return false;
    }

    /// <summary>
    /// Validates that the MAC address is not from a common virtual machine vendor.
    /// </summary>
    /// <param name="macAddress">The MAC address to validate.</param>
    /// <returns>True if the MAC address is not from a VM vendor; otherwise, false.</returns>
    private static bool NotBeVirtualMachineAddress(MacAddress macAddress)
    {
        if (string.IsNullOrEmpty(macAddress.Value))
        {
            return false;
        }

        // Common virtual machine OUI (Organizationally Unique Identifier) prefixes
        var vmPrefixes = new[]
        {
            "00:0C:29", // VMware
            "00:50:56", // VMware
            "00:05:69", // VMware
            "00:1C:14", // VMware
            "08:00:27", // VirtualBox
            "00:16:3E", // Xen
            "00:15:5D", // Microsoft Hyper-V
            "02:00:4C", // Docker/Containers
            "00:1B:21", // QEMU/KVM
        };

        var addressPrefix = macAddress.Value.Substring(0, 8); // First 3 octets
        return !vmPrefixes.Contains(addressPrefix);
    }
}
