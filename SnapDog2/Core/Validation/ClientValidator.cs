using FluentValidation;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;

namespace SnapDog2.Core.Validation;

/// <summary>
/// FluentValidation validator for Client entity.
/// Validates all properties and business rules for Snapcast clients.
/// </summary>
public sealed class ClientValidator : AbstractValidator<Client>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClientValidator"/> class.
    /// </summary>
    public ClientValidator()
    {
        // Required string properties
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Client ID is required.")
            .MaximumLength(100)
            .WithMessage("Client ID cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Client ID can only contain alphanumeric characters, underscores, and hyphens.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Client name is required.")
            .MaximumLength(200)
            .WithMessage("Client name cannot exceed 200 characters.")
            .MinimumLength(1)
            .WithMessage("Client name must be at least 1 character long.");

        // MAC address validation (using the MacAddress value object validation)
        RuleFor(x => x.MacAddress).NotNull().WithMessage("Client MAC address is required.");

        // IP address validation (using the IpAddress value object validation)
        RuleFor(x => x.IpAddress).NotNull().WithMessage("Client IP address is required.");

        // Status validation
        RuleFor(x => x.Status).IsInEnum().WithMessage("Invalid client status specified.");

        // Volume validation
        RuleFor(x => x.Volume).InclusiveBetween(0, 100).WithMessage("Volume must be between 0 and 100.");

        // Zone ID validation
        RuleFor(x => x.ZoneId)
            .MaximumLength(100)
            .WithMessage("Zone ID cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Zone ID can only contain alphanumeric characters, underscores, and hyphens.")
            .When(x => !string.IsNullOrEmpty(x.ZoneId));

        // Optional description validation
        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        // Optional location validation
        RuleFor(x => x.Location)
            .MaximumLength(200)
            .WithMessage("Location cannot exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Location));

        // Optional latency validation
        RuleFor(x => x.LatencyMs)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Latency cannot be negative.")
            .LessThanOrEqualTo(5000)
            .WithMessage("Latency cannot exceed 5000 milliseconds (5 seconds).")
            .When(x => x.LatencyMs.HasValue);

        // Timestamp validations
        RuleFor(x => x.CreatedAt)
            .NotEmpty()
            .WithMessage("Created timestamp is required.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Created timestamp cannot be in the future.");

        RuleFor(x => x.UpdatedAt)
            .GreaterThanOrEqualTo(x => x.CreatedAt)
            .WithMessage("Updated timestamp must be after or equal to created timestamp.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Updated timestamp cannot be in the future.")
            .When(x => x.UpdatedAt.HasValue);

        RuleFor(x => x.LastSeen)
            .GreaterThanOrEqualTo(x => x.CreatedAt)
            .WithMessage("Last seen timestamp must be after or equal to created timestamp.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Last seen timestamp cannot be in the future.")
            .When(x => x.LastSeen.HasValue);

        // Business rule: Connected clients should have a recent LastSeen timestamp
        RuleFor(x => x.LastSeen)
            .NotNull()
            .WithMessage("Connected clients must have a last seen timestamp.")
            .Must(BeRecentTimestamp)
            .WithMessage("Connected clients must have been seen within the last 24 hours.")
            .When(x => x.Status == ClientStatus.Connected);

        // Business rule: Disconnected clients should not be assigned to zones
        RuleFor(x => x.ZoneId)
            .Null()
            .WithMessage("Disconnected clients cannot be assigned to a zone.")
            .When(x => x.Status == ClientStatus.Disconnected);

        // Business rule: Volume consistency with mute status
        RuleFor(x => x)
            .Must(HaveConsistentVolumeAndMuteStatus)
            .WithMessage("Client volume and mute status must be consistent.")
            .When(x => x.IsMuted);

        // Business rule: IP address should be valid for network communication
        RuleFor(x => x.IpAddress)
            .Must(BeValidNetworkAddress)
            .WithMessage(
                "IP address must be a valid network address (not localhost or multicast for production clients)."
            )
            .When(x => x.Status == ClientStatus.Connected);
    }

    /// <summary>
    /// Validates that the timestamp is recent (within the last 24 hours).
    /// </summary>
    /// <param name="timestamp">The timestamp to validate.</param>
    /// <returns>True if the timestamp is recent; otherwise, false.</returns>
    private static bool BeRecentTimestamp(DateTime? timestamp)
    {
        if (!timestamp.HasValue)
        {
            return false;
        }

        var cutoff = DateTime.UtcNow.AddHours(-24);
        return timestamp.Value >= cutoff;
    }

    /// <summary>
    /// Validates that volume and mute status are consistent.
    /// </summary>
    /// <param name="client">The client to validate.</param>
    /// <returns>True if volume and mute status are consistent; otherwise, false.</returns>
    private static bool HaveConsistentVolumeAndMuteStatus(Client client)
    {
        // If muted, effective volume should be 0
        if (client.IsMuted)
        {
            return client.EffectiveVolume == 0;
        }

        // If not muted, effective volume should match actual volume
        return client.EffectiveVolume == client.Volume;
    }

    /// <summary>
    /// Validates that the IP address is appropriate for network communication.
    /// </summary>
    /// <param name="ipAddress">The IP address to validate.</param>
    /// <returns>True if the IP address is valid for network communication; otherwise, false.</returns>
    private static bool BeValidNetworkAddress(SnapDog2.Core.Models.ValueObjects.IpAddress ipAddress)
    {
        if (ipAddress.Value == null)
        {
            return false;
        }

        // Allow loopback for development/testing, but typically production clients should have real network addresses
        if (ipAddress.IsLoopback)
        {
            return true; // Allow for development scenarios
        }

        // Check for multicast addresses (not suitable for individual clients)
        var addressBytes = ipAddress.Value.GetAddressBytes();
        if (ipAddress.IsIPv4 && addressBytes[0] >= 224 && addressBytes[0] <= 239)
        {
            return false; // IPv4 multicast range
        }

        if (ipAddress.IsIPv6 && addressBytes[0] == 0xFF)
        {
            return false; // IPv6 multicast
        }

        // Check for private network ranges (these are valid for most installations)
        if (ipAddress.IsIPv4)
        {
            // 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16 are all valid private ranges
            var b1 = addressBytes[0];
            var b2 = addressBytes[1];

            if (b1 == 10)
            {
                return true; // 10.0.0.0/8
            }

            if (b1 == 172 && b2 >= 16 && b2 <= 31)
            {
                return true; // 172.16.0.0/12
            }

            if (b1 == 192 && b2 == 168)
            {
                return true; // 192.168.0.0/16
            }

            // Also allow other valid unicast addresses
            return b1 > 0 && b1 < 224; // Exclude 0.x.x.x and multicast
        }

        // For IPv6, allow most unicast addresses
        return !ipAddress.Value.IsIPv6Multicast;
    }
}
