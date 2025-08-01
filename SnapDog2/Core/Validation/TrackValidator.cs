using FluentValidation;
using SnapDog2.Core.Models.Entities;

namespace SnapDog2.Core.Validation;

/// <summary>
/// FluentValidation validator for Track entity.
/// Validates all properties and business rules for music tracks.
/// </summary>
public sealed class TrackValidator : AbstractValidator<Track>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TrackValidator"/> class.
    /// </summary>
    public TrackValidator()
    {
        // Required string properties
        RuleFor(static x => x.Id)
            .NotEmpty()
            .WithMessage("Track ID is required.")
            .MaximumLength(100)
            .WithMessage("Track ID cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Track ID can only contain alphanumeric characters, underscores, and hyphens.");

        RuleFor(static x => x.Title)
            .NotEmpty()
            .WithMessage("Track title is required.")
            .MaximumLength(500)
            .WithMessage("Track title cannot exceed 500 characters.")
            .MinimumLength(1)
            .WithMessage("Track title must be at least 1 character long.");

        // Optional metadata validations
        RuleFor(static x => x.Artist)
            .MaximumLength(300)
            .WithMessage("Artist name cannot exceed 300 characters.")
            .When(static x => !string.IsNullOrEmpty(x.Artist));

        RuleFor(static x => x.Album)
            .MaximumLength(300)
            .WithMessage("Album name cannot exceed 300 characters.")
            .When(static x => !string.IsNullOrEmpty(x.Album));

        RuleFor(static x => x.Genre)
            .MaximumLength(100)
            .WithMessage("Genre cannot exceed 100 characters.")
            .When(static x => !string.IsNullOrEmpty(x.Genre));

        RuleFor(static x => x.AlbumArtist)
            .MaximumLength(300)
            .WithMessage("Album artist cannot exceed 300 characters.")
            .When(static x => !string.IsNullOrEmpty(x.AlbumArtist));

        RuleFor(static x => x.Composer)
            .MaximumLength(300)
            .WithMessage("Composer cannot exceed 300 characters.")
            .When(static x => !string.IsNullOrEmpty(x.Composer));

        RuleFor(static x => x.Conductor)
            .MaximumLength(200)
            .WithMessage("Conductor cannot exceed 200 characters.")
            .When(static x => !string.IsNullOrEmpty(x.Conductor));

        RuleFor(static x => x.Label)
            .MaximumLength(200)
            .WithMessage("Label cannot exceed 200 characters.")
            .When(static x => !string.IsNullOrEmpty(x.Label));

        // Year validation
        RuleFor(static x => x.Year)
            .GreaterThanOrEqualTo(1900)
            .WithMessage("Year must be 1900 or later.")
            .LessThanOrEqualTo(DateTime.UtcNow.Year + 1)
            .WithMessage("Year cannot be more than one year in the future.")
            .When(static x => x.Year.HasValue);

        // Track number validations
        RuleFor(static x => x.TrackNumber)
            .GreaterThan(0)
            .WithMessage("Track number must be greater than 0.")
            .LessThanOrEqualTo(999)
            .WithMessage("Track number cannot exceed 999.")
            .When(static x => x.TrackNumber.HasValue);

        RuleFor(static x => x.TotalTracks)
            .GreaterThan(0)
            .WithMessage("Total tracks must be greater than 0.")
            .LessThanOrEqualTo(999)
            .WithMessage("Total tracks cannot exceed 999.")
            .When(static x => x.TotalTracks.HasValue);

        // Cross-validation for track numbers
        RuleFor(static x => x.TrackNumber)
            .LessThanOrEqualTo(static x => x.TotalTracks)
            .WithMessage("Track number cannot be greater than total tracks.")
            .When(static x => x.TrackNumber.HasValue && x.TotalTracks.HasValue);

        // Duration validation
        RuleFor(static x => x.DurationSeconds)
            .GreaterThan(0)
            .WithMessage("Duration must be greater than 0 seconds.")
            .LessThanOrEqualTo(7200) // 2 hours max
            .WithMessage("Duration cannot exceed 2 hours (7200 seconds).")
            .When(static x => x.DurationSeconds.HasValue);

        // File path validation
        RuleFor(static x => x.FilePath)
            .MaximumLength(1000)
            .WithMessage("File path cannot exceed 1000 characters.")
            .Must(BeValidPath)
            .WithMessage("File path must be a valid file path or URL.")
            .When(static x => !string.IsNullOrEmpty(x.FilePath));

        // File size validation
        RuleFor(static x => x.FileSizeBytes)
            .GreaterThan(0)
            .WithMessage("File size must be greater than 0 bytes.")
            .LessThanOrEqualTo(5368709120L) // 5 GB max
            .WithMessage("File size cannot exceed 5 GB.")
            .When(static x => x.FileSizeBytes.HasValue);

        // Audio technical specifications
        RuleFor(static x => x.BitrateKbps)
            .GreaterThan(0)
            .WithMessage("Bitrate must be greater than 0 kbps.")
            .LessThanOrEqualTo(9216) // DSD max
            .WithMessage("Bitrate cannot exceed 9216 kbps.")
            .When(static x => x.BitrateKbps.HasValue);

        RuleFor(static x => x.SampleRateHz)
            .GreaterThan(0)
            .WithMessage("Sample rate must be greater than 0 Hz.")
            .LessThanOrEqualTo(2822400) // DSD256 max
            .WithMessage("Sample rate cannot exceed 2822400 Hz.")
            .Must(BeValidSampleRate)
            .WithMessage("Sample rate must be a standard audio sample rate.")
            .When(static x => x.SampleRateHz.HasValue);

        RuleFor(static x => x.Channels)
            .GreaterThan(0)
            .WithMessage("Number of channels must be greater than 0.")
            .LessThanOrEqualTo(8)
            .WithMessage("Number of channels cannot exceed 8.")
            .When(static x => x.Channels.HasValue);

        // Format validation
        RuleFor(static x => x.Format)
            .MaximumLength(20)
            .WithMessage("Format cannot exceed 20 characters.")
            .Must(BeValidAudioFormat)
            .WithMessage("Format must be a valid audio format.")
            .When(static x => !string.IsNullOrEmpty(x.Format));

        // ISRC validation (International Standard Recording Code)
        RuleFor(static x => x.ISRC)
            .Length(12)
            .WithMessage("ISRC must be exactly 12 characters.")
            .Matches(@"^[A-Z]{2}[A-Z0-9]{3}[0-9]{7}$")
            .WithMessage("ISRC must follow the format: 2 letters + 3 alphanumeric + 7 digits.")
            .When(static x => !string.IsNullOrEmpty(x.ISRC));

        // MusicBrainz ID validations (UUID format)
        RuleFor(static x => x.MusicBrainzTrackId)
            .Must(BeValidGuid)
            .WithMessage("MusicBrainz Track ID must be a valid GUID.")
            .When(static x => !string.IsNullOrEmpty(x.MusicBrainzTrackId));

        RuleFor(static x => x.MusicBrainzRecordingId)
            .Must(BeValidGuid)
            .WithMessage("MusicBrainz Recording ID must be a valid GUID.")
            .When(static x => !string.IsNullOrEmpty(x.MusicBrainzRecordingId));

        // Tags validation
        RuleFor(static x => x.Tags)
            .NotNull()
            .WithMessage("Tags collection cannot be null.")
            .Must(HaveValidTagKeys)
            .WithMessage("Tag keys must be valid (non-empty, max 100 characters).")
            .Must(HaveValidTagValues)
            .WithMessage("Tag values cannot exceed 500 characters.");

        // Artwork path validation
        RuleFor(static x => x.ArtworkPath)
            .MaximumLength(1000)
            .WithMessage("Artwork path cannot exceed 1000 characters.")
            .Must(BeValidPath)
            .WithMessage("Artwork path must be a valid file path or URL.")
            .When(static x => !string.IsNullOrEmpty(x.ArtworkPath));

        // Lyrics validation
        RuleFor(static x => x.Lyrics)
            .MaximumLength(50000)
            .WithMessage("Lyrics cannot exceed 50,000 characters.")
            .When(static x => !string.IsNullOrEmpty(x.Lyrics));

        // Play count validation
        RuleFor(static x => x.PlayCount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Play count cannot be negative.")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Play count cannot exceed 1,000,000.");

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

        RuleFor(static x => x.LastPlayedAt)
            .GreaterThanOrEqualTo(static x => x.CreatedAt)
            .WithMessage("Last played timestamp must be after or equal to created timestamp.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Last played timestamp cannot be in the future.")
            .When(static x => x.LastPlayedAt.HasValue);

        // Business rule: Play count consistency
        RuleFor(static x => x.LastPlayedAt)
            .NotNull()
            .WithMessage("Tracks with play count > 0 must have a last played timestamp.")
            .When(static x => x.PlayCount > 0);
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

        // Standard audio sample rates including high-resolution and DSD
        int[] validSampleRates =
        {
            8000,
            11025,
            16000,
            22050,
            32000,
            44100,
            48000,
            64000,
            88200,
            96000,
            176400,
            192000,
            352800,
            384000,
            705600,
            768000,
            1411200,
            1536000,
            2822400, // DSD rates
        };
        return validSampleRates.Contains(sampleRate.Value);
    }

    /// <summary>
    /// Validates that the audio format is recognized.
    /// </summary>
    /// <param name="format">The format to validate.</param>
    /// <returns>True if the format is valid; otherwise, false.</returns>
    private static bool BeValidAudioFormat(string? format)
    {
        if (string.IsNullOrEmpty(format))
        {
            return true;
        }

        var validFormats = new[]
        {
            "mp3",
            "flac",
            "wav",
            "aac",
            "ogg",
            "m4a",
            "wma",
            "opus",
            "aiff",
            "dsd",
            "dsf",
            "dff",
        };
        return validFormats.Contains(format.ToLowerInvariant());
    }

    /// <summary>
    /// Validates that the string is a valid GUID.
    /// </summary>
    /// <param name="guidString">The GUID string to validate.</param>
    /// <returns>True if the string is a valid GUID; otherwise, false.</returns>
    private static bool BeValidGuid(string? guidString)
    {
        return Guid.TryParse(guidString, out _);
    }

    /// <summary>
    /// Validates that the path is a valid file path or URL.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>True if the path is valid; otherwise, false.</returns>
    private static bool BeValidPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return true;
        }

        // Check if it's a valid URL
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
        {
            return uri.Scheme == "http" || uri.Scheme == "https" || uri.Scheme == "file";
        }

        // Check if it's a valid relative path (basic validation)
        try
        {
            var fullPath = Path.GetFullPath(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that all tag keys are valid.
    /// </summary>
    /// <param name="tags">The tags dictionary to validate.</param>
    /// <returns>True if all tag keys are valid; otherwise, false.</returns>
    private static bool HaveValidTagKeys(System.Collections.Immutable.ImmutableDictionary<string, string> tags)
    {
        return tags.Keys.All(static key => !string.IsNullOrEmpty(key) && key.Length <= 100);
    }

    /// <summary>
    /// Validates that all tag values are within size limits.
    /// </summary>
    /// <param name="tags">The tags dictionary to validate.</param>
    /// <returns>True if all tag values are valid; otherwise, false.</returns>
    private static bool HaveValidTagValues(System.Collections.Immutable.ImmutableDictionary<string, string> tags)
    {
        return tags.Values.All(static value => value == null || value.Length <= 500);
    }
}
