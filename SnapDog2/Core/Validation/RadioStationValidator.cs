using FluentValidation;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;

namespace SnapDog2.Core.Validation;

/// <summary>
/// FluentValidation validator for RadioStation entity.
/// Validates all properties and business rules for internet radio stations.
/// </summary>
public sealed class RadioStationValidator : AbstractValidator<RadioStation>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RadioStationValidator"/> class.
    /// </summary>
    public RadioStationValidator()
    {
        // Required string properties
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Radio station ID is required.")
            .MaximumLength(100)
            .WithMessage("Radio station ID cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Radio station ID can only contain alphanumeric characters, underscores, and hyphens.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Radio station name is required.")
            .MaximumLength(200)
            .WithMessage("Radio station name cannot exceed 200 characters.")
            .MinimumLength(1)
            .WithMessage("Radio station name must be at least 1 character long.");

        // URL validation (using the StreamUrl value object validation)
        RuleFor(x => x.Url).NotNull().WithMessage("Radio station URL is required.");

        // Codec validation
        RuleFor(x => x.Codec).IsInEnum().WithMessage("Invalid audio codec specified.");

        // Optional metadata validations
        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Genre)
            .MaximumLength(100)
            .WithMessage("Genre cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Genre));

        RuleFor(x => x.Country)
            .MaximumLength(100)
            .WithMessage("Country cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z\s\-'\.]+$")
            .WithMessage("Country can only contain letters, spaces, hyphens, apostrophes, and periods.")
            .When(x => !string.IsNullOrEmpty(x.Country));

        RuleFor(x => x.Language)
            .MaximumLength(50)
            .WithMessage("Language cannot exceed 50 characters.")
            .Matches(@"^[a-zA-Z\s\-]+$")
            .WithMessage("Language can only contain letters, spaces, and hyphens.")
            .When(x => !string.IsNullOrEmpty(x.Language));

        // Audio technical specifications
        RuleFor(x => x.BitrateKbps)
            .GreaterThan(0)
            .WithMessage("Bitrate must be greater than 0 kbps.")
            .LessThanOrEqualTo(1411)
            .WithMessage("Bitrate cannot exceed 1411 kbps (CD quality limit).")
            .Must(BeValidBitrateForCodec)
            .WithMessage("Bitrate is not valid for the specified codec.")
            .When(x => x.BitrateKbps.HasValue);

        RuleFor(x => x.SampleRateHz)
            .GreaterThan(0)
            .WithMessage("Sample rate must be greater than 0 Hz.")
            .LessThanOrEqualTo(192000)
            .WithMessage("Sample rate cannot exceed 192000 Hz.")
            .Must(BeValidSampleRate)
            .WithMessage("Sample rate must be a standard audio sample rate.")
            .When(x => x.SampleRateHz.HasValue);

        RuleFor(x => x.Channels)
            .GreaterThan(0)
            .WithMessage("Number of channels must be greater than 0.")
            .LessThanOrEqualTo(8)
            .WithMessage("Number of channels cannot exceed 8.")
            .When(x => x.Channels.HasValue);

        // Website URL validation
        RuleFor(x => x.Website)
            .MaximumLength(500)
            .WithMessage("Website URL cannot exceed 500 characters.")
            .Must(BeValidWebsiteUrl)
            .WithMessage("Website must be a valid HTTP or HTTPS URL.")
            .When(x => !string.IsNullOrEmpty(x.Website));

        // Logo URL validation
        RuleFor(x => x.LogoUrl)
            .MaximumLength(500)
            .WithMessage("Logo URL cannot exceed 500 characters.")
            .Must(BeValidImageUrl)
            .WithMessage("Logo URL must be a valid HTTP or HTTPS URL pointing to an image.")
            .When(x => !string.IsNullOrEmpty(x.LogoUrl));

        // Tags validation
        RuleFor(x => x.Tags)
            .MaximumLength(500)
            .WithMessage("Tags cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Tags));

        // Priority validation
        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Priority must be at least 1.")
            .LessThanOrEqualTo(10)
            .WithMessage("Priority cannot exceed 10.");

        // Authentication validations
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required when authentication is enabled.")
            .MaximumLength(100)
            .WithMessage("Username cannot exceed 100 characters.")
            .When(x => x.RequiresAuth);

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required when authentication is enabled.")
            .MaximumLength(200)
            .WithMessage("Password cannot exceed 200 characters.")
            .When(x => x.RequiresAuth);

        RuleFor(x => x.Username)
            .Null()
            .WithMessage("Username should not be set when authentication is not required.")
            .When(x => !x.RequiresAuth);

        RuleFor(x => x.Password)
            .Null()
            .WithMessage("Password should not be set when authentication is not required.")
            .When(x => !x.RequiresAuth);

        // Play count validation
        RuleFor(x => x.PlayCount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Play count cannot be negative.")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Play count cannot exceed 1,000,000.");

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

        RuleFor(x => x.LastPlayedAt)
            .GreaterThanOrEqualTo(x => x.CreatedAt)
            .WithMessage("Last played timestamp must be after or equal to created timestamp.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Last played timestamp cannot be in the future.")
            .When(x => x.LastPlayedAt.HasValue);

        RuleFor(x => x.LastCheckedAt)
            .GreaterThanOrEqualTo(x => x.CreatedAt)
            .WithMessage("Last checked timestamp must be after or equal to created timestamp.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Last checked timestamp cannot be in the future.")
            .When(x => x.LastCheckedAt.HasValue);

        // Business rules
        RuleFor(x => x.LastPlayedAt)
            .NotNull()
            .WithMessage("Radio stations with play count > 0 must have a last played timestamp.")
            .When(x => x.PlayCount > 0);

        // Business rule: Online status should have recent check
        RuleFor(x => x.LastCheckedAt)
            .NotNull()
            .WithMessage("Radio stations with online status must have a last checked timestamp.")
            .Must(BeRecentTimestamp)
            .WithMessage("Online status check must be recent (within 24 hours).")
            .When(x => x.IsOnline.HasValue);

        // Business rule: Codec-specific validations
        RuleFor(x => x)
            .Must(HaveValidCodecConfiguration)
            .WithMessage("Radio station configuration is not valid for the specified codec.");
    }

    /// <summary>
    /// Validates that the bitrate is appropriate for the specified codec.
    /// </summary>
    /// <param name="station">The radio station to validate.</param>
    /// <param name="bitrate">The bitrate to validate.</param>
    /// <returns>True if the bitrate is valid for the codec; otherwise, false.</returns>
    private static bool BeValidBitrateForCodec(RadioStation station, int? bitrate)
    {
        if (!bitrate.HasValue)
        {
            return true;
        }

        return station.Codec switch
        {
            AudioCodec.PCM => bitrate >= 64 && bitrate <= 1411,
            AudioCodec.FLAC => bitrate >= 400 && bitrate <= 1411,
            AudioCodec.MP3 => bitrate >= 32 && bitrate <= 320,
            AudioCodec.AAC => bitrate >= 32 && bitrate <= 512,
            AudioCodec.OGG => bitrate >= 32 && bitrate <= 500,
            _ => true,
        };
    }

    /// <summary>
    /// Validates that the sample rate is a standard audio sample rate.
    /// </summary>
    /// <param name="sampleRate">The sample rate to validate.</param>
    /// <returns>True if the sample rate is standard; otherwise, false.</returns>
    private static bool BeValidSampleRate(int? sampleRate)
    {
        if (!sampleRate.HasValue)
        {
            return true;
        }

        int[] validSampleRates = { 8000, 11025, 16000, 22050, 32000, 44100, 48000, 88200, 96000, 176400, 192000 };
        return validSampleRates.Contains(sampleRate.Value);
    }

    /// <summary>
    /// Validates that the website URL is valid.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <returns>True if the URL is valid; otherwise, false.</returns>
    private static bool BeValidWebsiteUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return true;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.Scheme == "http" || uri.Scheme == "https";
        }

        return false;
    }

    /// <summary>
    /// Validates that the image URL is valid and points to an image.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <returns>True if the URL is valid; otherwise, false.</returns>
    private static bool BeValidImageUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return true;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                return false;
            }

            // Check if the URL path suggests an image file
            var path = uri.AbsolutePath.ToLowerInvariant();
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".bmp" };
            return imageExtensions.Any(ext => path.EndsWith(ext));
        }

        return false;
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
    /// Validates codec-specific configuration requirements.
    /// </summary>
    /// <param name="station">The radio station to validate.</param>
    /// <returns>True if the codec configuration is valid; otherwise, false.</returns>
    private static bool HaveValidCodecConfiguration(RadioStation station)
    {
        return station.Codec switch
        {
            AudioCodec.PCM => ValidatePcmConfiguration(station),
            AudioCodec.FLAC => ValidateFlacConfiguration(station),
            AudioCodec.MP3 => ValidateMp3Configuration(station),
            AudioCodec.AAC => ValidateAacConfiguration(station),
            AudioCodec.OGG => ValidateOggConfiguration(station),
            _ => true,
        };
    }

    /// <summary>
    /// Validates PCM-specific configuration.
    /// </summary>
    /// <param name="station">The radio station to validate.</param>
    /// <returns>True if the PCM configuration is valid; otherwise, false.</returns>
    private static bool ValidatePcmConfiguration(RadioStation station)
    {
        return !station.BitrateKbps.HasValue || station.BitrateKbps >= 64;
    }

    /// <summary>
    /// Validates FLAC-specific configuration.
    /// </summary>
    /// <param name="station">The radio station to validate.</param>
    /// <returns>True if the FLAC configuration is valid; otherwise, false.</returns>
    private static bool ValidateFlacConfiguration(RadioStation station)
    {
        return !station.BitrateKbps.HasValue || station.BitrateKbps >= 400;
    }

    /// <summary>
    /// Validates MP3-specific configuration.
    /// </summary>
    /// <param name="station">The radio station to validate.</param>
    /// <returns>True if the MP3 configuration is valid; otherwise, false.</returns>
    private static bool ValidateMp3Configuration(RadioStation station)
    {
        return !station.BitrateKbps.HasValue || (station.BitrateKbps >= 32 && station.BitrateKbps <= 320);
    }

    /// <summary>
    /// Validates AAC-specific configuration.
    /// </summary>
    /// <param name="station">The radio station to validate.</param>
    /// <returns>True if the AAC configuration is valid; otherwise, false.</returns>
    private static bool ValidateAacConfiguration(RadioStation station)
    {
        return !station.BitrateKbps.HasValue || (station.BitrateKbps >= 32 && station.BitrateKbps <= 512);
    }

    /// <summary>
    /// Validates OGG Vorbis-specific configuration.
    /// </summary>
    /// <param name="station">The radio station to validate.</param>
    /// <returns>True if the OGG configuration is valid; otherwise, false.</returns>
    private static bool ValidateOggConfiguration(RadioStation station)
    {
        return !station.BitrateKbps.HasValue || (station.BitrateKbps >= 32 && station.BitrateKbps <= 500);
    }
}
