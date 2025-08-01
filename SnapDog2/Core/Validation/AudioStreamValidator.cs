using FluentValidation;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;

namespace SnapDog2.Core.Validation;

/// <summary>
/// FluentValidation validator for AudioStream entity.
/// Validates all properties and business rules for audio streams.
/// </summary>
public sealed class AudioStreamValidator : AbstractValidator<AudioStream>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AudioStreamValidator"/> class.
    /// </summary>
    public AudioStreamValidator()
    {
        // Required string properties
        RuleFor(static x => x.Id)
            .NotEmpty()
            .WithMessage("Audio stream ID is required.")
            .MaximumLength(100)
            .WithMessage("Audio stream ID cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Audio stream ID can only contain alphanumeric characters, underscores, and hyphens.");

        RuleFor(static x => x.Name)
            .NotEmpty()
            .WithMessage("Audio stream name is required.")
            .MaximumLength(200)
            .WithMessage("Audio stream name cannot exceed 200 characters.")
            .MinimumLength(1)
            .WithMessage("Audio stream name must be at least 1 character long.");

        // URL validation (using the StreamUrl value object validation)
        RuleFor(static x => x.Url).NotNull().WithMessage("Audio stream URL is required.");

        // Codec validation
        RuleFor(static x => x.Codec).IsInEnum().WithMessage("Invalid audio codec specified.");

        // Bitrate validation
        RuleFor(static x => x.BitrateKbps)
            .GreaterThan(0)
            .WithMessage("Bitrate must be greater than 0 kbps.")
            .LessThanOrEqualTo(1411) // CD quality FLAC max
            .WithMessage("Bitrate cannot exceed 1411 kbps (CD quality limit).")
            .Must(BeValidBitrateForCodec)
            .WithMessage("Bitrate is not valid for the specified codec.");

        // Status validation
        RuleFor(static x => x.Status).IsInEnum().WithMessage("Invalid stream status specified.");

        // Optional sample rate validation
        RuleFor(static x => x.SampleRateHz)
            .GreaterThan(0)
            .WithMessage("Sample rate must be greater than 0 Hz.")
            .LessThanOrEqualTo(192000)
            .WithMessage("Sample rate cannot exceed 192000 Hz.")
            .Must(BeValidSampleRate)
            .WithMessage("Sample rate must be a standard audio sample rate.")
            .When(static x => x.SampleRateHz.HasValue);

        // Optional channels validation
        RuleFor(static x => x.Channels)
            .GreaterThan(0)
            .WithMessage("Number of channels must be greater than 0.")
            .LessThanOrEqualTo(8)
            .WithMessage("Number of channels cannot exceed 8.")
            .When(static x => x.Channels.HasValue);

        // Optional description validation
        RuleFor(static x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters.")
            .When(static x => !string.IsNullOrEmpty(x.Description));

        // Optional tags validation
        RuleFor(static x => x.Tags)
            .MaximumLength(500)
            .WithMessage("Tags cannot exceed 500 characters.")
            .When(static x => !string.IsNullOrEmpty(x.Tags));

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

        // Business rule: Stereo content validation
        RuleFor(static x => x)
            .Must(HaveValidStereoConfiguration)
            .WithMessage("Stereo streams must have at least 2 channels.")
            .When(static x => x.Channels.HasValue);

        // Business rule: Codec-specific validations
        RuleFor(static x => x)
            .Must(HaveValidCodecConfiguration)
            .WithMessage("Stream configuration is not valid for the specified codec.");
    }

    /// <summary>
    /// Validates that the bitrate is appropriate for the specified codec.
    /// </summary>
    /// <param name="stream">The audio stream to validate.</param>
    /// <param name="bitrate">The bitrate to validate.</param>
    /// <returns>True if the bitrate is valid for the codec; otherwise, false.</returns>
    private static bool BeValidBitrateForCodec(AudioStream stream, int bitrate)
    {
        return stream.Codec switch
        {
            AudioCodec.PCM => bitrate >= 64 && bitrate <= 1411, // Uncompressed
            AudioCodec.FLAC => bitrate >= 400 && bitrate <= 1411, // Lossless
            AudioCodec.MP3 => bitrate >= 32 && bitrate <= 320, // Lossy
            AudioCodec.AAC => bitrate >= 32 && bitrate <= 512, // Lossy
            AudioCodec.OGG => bitrate >= 32 && bitrate <= 500, // Lossy
            _ => true, // Unknown codec, allow any bitrate
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

        // Standard audio sample rates
        int[] validSampleRates = { 8000, 11025, 16000, 22050, 32000, 44100, 48000, 88200, 96000, 176400, 192000 };
        return validSampleRates.Contains(sampleRate.Value);
    }

    /// <summary>
    /// Validates that stereo streams have at least 2 channels.
    /// </summary>
    /// <param name="stream">The audio stream to validate.</param>
    /// <returns>True if the stereo configuration is valid; otherwise, false.</returns>
    private static bool HaveValidStereoConfiguration(AudioStream stream)
    {
        if (!stream.Channels.HasValue)
        {
            return true;
        }

        // If claiming to be stereo, must have at least 2 channels
        return !stream.IsStereo || stream.Channels >= 2;
    }

    /// <summary>
    /// Validates codec-specific configuration requirements.
    /// </summary>
    /// <param name="stream">The audio stream to validate.</param>
    /// <returns>True if the codec configuration is valid; otherwise, false.</returns>
    private static bool HaveValidCodecConfiguration(AudioStream stream)
    {
        return stream.Codec switch
        {
            AudioCodec.PCM => ValidatePcmConfiguration(stream),
            AudioCodec.FLAC => ValidateFlacConfiguration(stream),
            AudioCodec.MP3 => ValidateMp3Configuration(stream),
            AudioCodec.AAC => ValidateAacConfiguration(stream),
            AudioCodec.OGG => ValidateOggConfiguration(stream),
            _ => true, // Unknown codec, assume valid
        };
    }

    /// <summary>
    /// Validates PCM-specific configuration.
    /// </summary>
    /// <param name="stream">The audio stream to validate.</param>
    /// <returns>True if the PCM configuration is valid; otherwise, false.</returns>
    private static bool ValidatePcmConfiguration(AudioStream stream)
    {
        // PCM typically requires higher bitrates and standard sample rates
        return stream.BitrateKbps >= 64 && (!stream.SampleRateHz.HasValue || BeValidSampleRate(stream.SampleRateHz));
    }

    /// <summary>
    /// Validates FLAC-specific configuration.
    /// </summary>
    /// <param name="stream">The audio stream to validate.</param>
    /// <returns>True if the FLAC configuration is valid; otherwise, false.</returns>
    private static bool ValidateFlacConfiguration(AudioStream stream)
    {
        // FLAC is lossless, so bitrate should be in the higher range
        return stream.BitrateKbps >= 400;
    }

    /// <summary>
    /// Validates MP3-specific configuration.
    /// </summary>
    /// <param name="stream">The audio stream to validate.</param>
    /// <returns>True if the MP3 configuration is valid; otherwise, false.</returns>
    private static bool ValidateMp3Configuration(AudioStream stream)
    {
        // MP3 has specific bitrate constraints
        return stream.BitrateKbps >= 32 && stream.BitrateKbps <= 320;
    }

    /// <summary>
    /// Validates AAC-specific configuration.
    /// </summary>
    /// <param name="stream">The audio stream to validate.</param>
    /// <returns>True if the AAC configuration is valid; otherwise, false.</returns>
    private static bool ValidateAacConfiguration(AudioStream stream)
    {
        // AAC can handle a wide range of bitrates
        return stream.BitrateKbps >= 32 && stream.BitrateKbps <= 512;
    }

    /// <summary>
    /// Validates OGG Vorbis-specific configuration.
    /// </summary>
    /// <param name="stream">The audio stream to validate.</param>
    /// <returns>True if the OGG configuration is valid; otherwise, false.</returns>
    private static bool ValidateOggConfiguration(AudioStream stream)
    {
        // OGG Vorbis has specific bitrate constraints
        return stream.BitrateKbps >= 32 && stream.BitrateKbps <= 500;
    }
}
