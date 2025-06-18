using FluentValidation;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;
using SnapDog2.Server.Features.AudioStreams.Commands;

namespace SnapDog2.Server.Features.AudioStreams.Validators;

/// <summary>
/// Validator for CreateAudioStreamCommand using FluentValidation.
/// Validates all parameters and business rules for creating audio streams.
/// </summary>
public sealed class CreateAudioStreamValidator : AbstractValidator<CreateAudioStreamCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAudioStreamValidator"/> class.
    /// </summary>
    public CreateAudioStreamValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Stream name is required.")
            .Length(1, 100)
            .WithMessage("Stream name must be between 1 and 100 characters.")
            .Matches(@"^[a-zA-Z0-9\s\-_\.]+$")
            .WithMessage("Stream name can only contain letters, numbers, spaces, hyphens, underscores, and periods.");

        RuleFor(x => x.Url)
            .NotNull()
            .WithMessage("Stream URL is required.")
            .Must(BeValidStreamUrl)
            .WithMessage("Stream URL must be a valid URL with supported scheme (http, https, rtsp, rtmp, file, ftp).");

        RuleFor(x => x.Codec)
            .IsInEnum()
            .WithMessage("Audio codec must be a valid codec type (PCM, FLAC, MP3, AAC, OGG).");

        RuleFor(x => x.SampleRate)
            .GreaterThan(0)
            .WithMessage("Sample rate must be greater than 0.")
            .Must(BeValidSampleRate)
            .WithMessage(
                "Sample rate must be a standard audio sample rate (8000, 16000, 22050, 32000, 44100, 48000, 88200, 96000, 176400, 192000 Hz)."
            );

        RuleFor(x => x.Description).MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.BitrateKbps)
            .GreaterThan(0)
            .WithMessage("Bitrate must be greater than 0.")
            .LessThanOrEqualTo(320)
            .WithMessage("Bitrate cannot exceed 320 kbps.")
            .When(x => x.BitrateKbps.HasValue);

        RuleFor(x => x.Channels)
            .GreaterThan(0)
            .WithMessage("Channel count must be greater than 0.")
            .LessThanOrEqualTo(8)
            .WithMessage("Channel count cannot exceed 8 channels.")
            .When(x => x.Channels.HasValue);

        RuleFor(x => x.Tags)
            .MaximumLength(200)
            .WithMessage("Tags cannot exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Tags));

        RuleFor(x => x.RequestedBy)
            .NotEmpty()
            .WithMessage("RequestedBy is required.")
            .Length(1, 50)
            .WithMessage("RequestedBy must be between 1 and 50 characters.");

        // Codec-specific sample rate validation
        RuleFor(x => x)
            .Must(HaveCompatibleSampleRateForCodec)
            .WithMessage("Sample rate is not compatible with the selected codec.")
            .When(x => x.SampleRate > 0);

        // MP3-specific bitrate validation
        RuleFor(x => x.BitrateKbps)
            .Must((command, bitrate) => BeValidMp3Bitrate(command.Codec, bitrate))
            .WithMessage(
                "Bitrate is not valid for MP3 codec. Valid MP3 bitrates: 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320 kbps."
            )
            .When(x => x.Codec == AudioCodec.MP3 && x.BitrateKbps.HasValue);
    }

    /// <summary>
    /// Validates if the stream URL is valid.
    /// </summary>
    /// <param name="url">The stream URL to validate.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    private static bool BeValidStreamUrl(StreamUrl url)
    {
        if (url.Value == null)
        {
            return false;
        }

        return StreamUrl.IsValid(url.ToString());
    }

    /// <summary>
    /// Validates if the sample rate is a standard audio sample rate.
    /// </summary>
    /// <param name="sampleRate">The sample rate to validate.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    private static bool BeValidSampleRate(int sampleRate)
    {
        var validSampleRates = new[]
        {
            8000,
            11025,
            12000,
            16000,
            22050,
            24000,
            32000,
            44100,
            48000,
            64000,
            88200,
            96000,
            176400,
            192000,
        };

        return validSampleRates.Contains(sampleRate);
    }

    /// <summary>
    /// Validates if the sample rate is compatible with the specified codec.
    /// </summary>
    /// <param name="command">The create audio stream command.</param>
    /// <returns>True if compatible; otherwise, false.</returns>
    private static bool HaveCompatibleSampleRateForCodec(CreateAudioStreamCommand command)
    {
        var validSampleRates = command.Codec switch
        {
            AudioCodec.PCM => new[] { 8000, 16000, 22050, 32000, 44100, 48000, 88200, 96000, 176400, 192000 },
            AudioCodec.FLAC => new[] { 8000, 16000, 22050, 32000, 44100, 48000, 88200, 96000, 176400, 192000 },
            AudioCodec.MP3 => new[] { 8000, 11025, 12000, 16000, 22050, 24000, 32000, 44100, 48000 },
            AudioCodec.AAC => new[]
            {
                8000,
                11025,
                12000,
                16000,
                22050,
                24000,
                32000,
                44100,
                48000,
                64000,
                88200,
                96000,
            },
            AudioCodec.OGG => new[] { 8000, 11025, 16000, 22050, 32000, 44100, 48000 },
            _ => Array.Empty<int>(),
        };

        return validSampleRates.Contains(command.SampleRate);
    }

    /// <summary>
    /// Validates if the bitrate is valid for MP3 codec.
    /// </summary>
    /// <param name="codec">The audio codec.</param>
    /// <param name="bitrate">The bitrate in kbps.</param>
    /// <returns>True if valid for MP3; otherwise, false.</returns>
    private static bool BeValidMp3Bitrate(AudioCodec codec, int? bitrate)
    {
        if (codec != AudioCodec.MP3 || !bitrate.HasValue)
        {
            return true; // Not applicable
        }

        var validMp3Bitrates = new[] { 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320 };
        return validMp3Bitrates.Contains(bitrate.Value);
    }
}
