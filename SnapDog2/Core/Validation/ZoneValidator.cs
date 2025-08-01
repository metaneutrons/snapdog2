using FluentValidation;
using SnapDog2.Core.Models.Entities;

namespace SnapDog2.Core.Validation;

/// <summary>
/// FluentValidation validator for Zone entity.
/// Validates all properties and business rules for audio zones.
/// </summary>
public sealed class ZoneValidator : AbstractValidator<Zone>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneValidator"/> class.
    /// </summary>
    public ZoneValidator()
    {
        // Required string properties
        RuleFor(static x => x.Id)
            .NotEmpty()
            .WithMessage("Zone ID is required.")
            .MaximumLength(100)
            .WithMessage("Zone ID cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Zone ID can only contain alphanumeric characters, underscores, and hyphens.");

        RuleFor(static x => x.Name)
            .NotEmpty()
            .WithMessage("Zone name is required.")
            .MaximumLength(200)
            .WithMessage("Zone name cannot exceed 200 characters.")
            .MinimumLength(1)
            .WithMessage("Zone name must be at least 1 character long.");

        // Client IDs validation
        RuleFor(static x => x.ClientIds)
            .NotNull()
            .WithMessage("Client IDs collection cannot be null.")
            .Must(HaveUniqueClientIds)
            .WithMessage("Zone cannot contain duplicate client IDs.")
            .Must(HaveValidClientIdFormat)
            .WithMessage("All client IDs must be valid format.");

        RuleForEach(static x => x.ClientIds)
            .NotEmpty()
            .WithMessage("Client ID cannot be empty.")
            .MaximumLength(100)
            .WithMessage("Client ID cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Client ID can only contain alphanumeric characters, underscores, and hyphens.");

        // Current stream ID validation
        RuleFor(static x => x.CurrentStreamId)
            .MaximumLength(100)
            .WithMessage("Current stream ID cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Current stream ID can only contain alphanumeric characters, underscores, and hyphens.")
            .When(static x => !string.IsNullOrEmpty(x.CurrentStreamId));

        // Optional description validation
        RuleFor(static x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters.")
            .When(static x => !string.IsNullOrEmpty(x.Description));

        // Optional location validation
        RuleFor(static x => x.Location)
            .MaximumLength(200)
            .WithMessage("Location cannot exceed 200 characters.")
            .When(static x => !string.IsNullOrEmpty(x.Location));

        // Color validation (hex color format)
        RuleFor(static x => x.Color)
            .NotEmpty()
            .WithMessage("Zone color is required.")
            .Matches(@"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$")
            .WithMessage("Color must be a valid hex color code (e.g., #007bff or #fff).");

        // Icon validation
        RuleFor(static x => x.Icon)
            .NotEmpty()
            .WithMessage("Zone icon is required.")
            .MaximumLength(50)
            .WithMessage("Icon identifier cannot exceed 50 characters.")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Icon identifier can only contain alphanumeric characters, underscores, and hyphens.");

        // Volume settings validation
        RuleFor(static x => x.DefaultVolume)
            .InclusiveBetween(0, 100)
            .WithMessage("Default volume must be between 0 and 100.")
            .Must(static (zone, defaultVolume) => defaultVolume >= zone.MinVolume && defaultVolume <= zone.MaxVolume)
            .WithMessage("Default volume must be between minimum and maximum volume.");

        RuleFor(static x => x.MinVolume)
            .InclusiveBetween(0, 100)
            .WithMessage("Minimum volume must be between 0 and 100.");

        RuleFor(static x => x.MaxVolume)
            .InclusiveBetween(0, 100)
            .WithMessage("Maximum volume must be between 0 and 100.");

        // Cross-property validation for volume ranges
        RuleFor(static x => x)
            .Must(HaveValidVolumeRange)
            .WithMessage("Minimum volume cannot be greater than maximum volume.");

        // Priority validation
        RuleFor(static x => x.Priority)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Zone priority must be at least 1.")
            .LessThanOrEqualTo(10)
            .WithMessage("Zone priority cannot exceed 10.");

        // Optional tags validation
        RuleFor(static x => x.Tags)
            .MaximumLength(500)
            .WithMessage("Tags cannot exceed 500 characters.")
            .When(static x => !string.IsNullOrEmpty(x.Tags));

        // Audio quality validation
        RuleFor(static x => x.AudioQuality)
            .NotEmpty()
            .WithMessage("Audio quality setting is required.")
            .Must(BeValidAudioQuality)
            .WithMessage("Audio quality must be one of: low, medium, high, lossless.");

        // Timestamp validations
        RuleFor(static x => x.CreatedAt)
            .NotEmpty()
            .WithMessage("Created timestamp is required.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Created timestamp cannot be in the future.");

        RuleFor(static x => x.UpdatedAt)
            .GreaterThanOrEqualTo(static x => x.CreatedAt)
            .WithMessage("Updated timestamp must be after or equal to created timestamp.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Updated timestamp cannot be in the future.")
            .When(static x => x.UpdatedAt.HasValue);

        // Business rules
        RuleFor(static x => x)
            .Must(HaveReasonableVolumeRange)
            .WithMessage("Volume range (max - min) should not exceed 80 to ensure usability.");

        RuleFor(static x => x.ClientIds)
            .Must(NotExceedMaximumClients)
            .WithMessage("Zone cannot have more than 50 clients assigned.");

        // Business rule: Disabled zones should not have current streams
        RuleFor(static x => x.CurrentStreamId)
            .Null()
            .WithMessage("Disabled zones cannot have a current stream assigned.")
            .When(static x => !x.IsEnabled);
    }

    /// <summary>
    /// Validates that all client IDs are unique within the zone.
    /// </summary>
    /// <param name="clientIds">The collection of client IDs to validate.</param>
    /// <returns>True if all client IDs are unique; otherwise, false.</returns>
    private static bool HaveUniqueClientIds(IEnumerable<string> clientIds)
    {
        if (clientIds == null)
        {
            return true;
        }

        var clientList = clientIds.ToList();
        return clientList.Count == clientList.Distinct().Count();
    }

    /// <summary>
    /// Validates that all client IDs have valid format.
    /// </summary>
    /// <param name="clientIds">The collection of client IDs to validate.</param>
    /// <returns>True if all client IDs have valid format; otherwise, false.</returns>
    private static bool HaveValidClientIdFormat(IEnumerable<string> clientIds)
    {
        if (clientIds == null)
        {
            return true;
        }

        var clientIdRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9_-]+$");
        return clientIds.All(id => !string.IsNullOrEmpty(id) && id.Length <= 100 && clientIdRegex.IsMatch(id));
    }

    /// <summary>
    /// Validates that the audio quality setting is valid.
    /// </summary>
    /// <param name="audioQuality">The audio quality setting to validate.</param>
    /// <returns>True if the audio quality is valid; otherwise, false.</returns>
    private static bool BeValidAudioQuality(string audioQuality)
    {
        if (string.IsNullOrEmpty(audioQuality))
        {
            return false;
        }

        var validQualities = new[] { "low", "medium", "high", "lossless" };
        return validQualities.Contains(audioQuality.ToLowerInvariant());
    }

    /// <summary>
    /// Validates that the minimum volume is not greater than maximum volume.
    /// </summary>
    /// <param name="zone">The zone to validate.</param>
    /// <returns>True if the volume range is valid; otherwise, false.</returns>
    private static bool HaveValidVolumeRange(Zone zone)
    {
        return zone.MinVolume <= zone.MaxVolume;
    }

    /// <summary>
    /// Validates that the volume range is reasonable for usability.
    /// </summary>
    /// <param name="zone">The zone to validate.</param>
    /// <returns>True if the volume range is reasonable; otherwise, false.</returns>
    private static bool HaveReasonableVolumeRange(Zone zone)
    {
        var range = zone.MaxVolume - zone.MinVolume;
        return range <= 80; // Allow reasonable volume range
    }

    /// <summary>
    /// Validates that the zone doesn't exceed the maximum number of clients.
    /// </summary>
    /// <param name="clientIds">The collection of client IDs to validate.</param>
    /// <returns>True if the client count is within limits; otherwise, false.</returns>
    private static bool NotExceedMaximumClients(IEnumerable<string> clientIds)
    {
        if (clientIds == null)
        {
            return true;
        }

        return clientIds.Count() <= 50; // Reasonable limit for zone size
    }
}
