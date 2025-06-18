using MediatR;
using SnapDog2.Core.Common;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;

namespace SnapDog2.Server.Features.AudioStreams.Commands;

/// <summary>
/// Command to create a new audio stream in the system.
/// Contains all necessary information to configure and initialize an audio stream.
/// </summary>
/// <param name="Name">The display name for the audio stream.</param>
/// <param name="Url">The URL of the audio stream source.</param>
/// <param name="Codec">The audio codec used by the stream.</param>
/// <param name="SampleRate">The sample rate of the audio stream in Hz.</param>
/// <param name="Description">Optional description of the audio stream.</param>
public record CreateAudioStreamCommand(
    string Name,
    StreamUrl Url,
    AudioCodec Codec,
    int SampleRate,
    string Description = ""
) : IRequest<Result<AudioStream>>
{
    /// <summary>
    /// Gets the bitrate in kilobits per second (optional, will be auto-detected if not specified).
    /// </summary>
    public int? BitrateKbps { get; init; }

    /// <summary>
    /// Gets the number of audio channels (optional, will be auto-detected if not specified).
    /// </summary>
    public int? Channels { get; init; }

    /// <summary>
    /// Gets additional metadata or tags for the stream (optional).
    /// </summary>
    public string? Tags { get; init; }

    /// <summary>
    /// Gets the user or system that requested the stream creation.
    /// </summary>
    public string RequestedBy { get; init; } = "System";

    /// <summary>
    /// Creates a new instance of the CreateAudioStreamCommand with required parameters.
    /// </summary>
    /// <param name="name">The display name for the audio stream.</param>
    /// <param name="url">The URL of the audio stream source.</param>
    /// <param name="codec">The audio codec used by the stream.</param>
    /// <param name="sampleRate">The sample rate of the audio stream in Hz.</param>
    /// <param name="description">Optional description of the audio stream.</param>
    /// <returns>A new CreateAudioStreamCommand instance.</returns>
    public static CreateAudioStreamCommand Create(
        string name,
        string url,
        AudioCodec codec,
        int sampleRate,
        string description = ""
    )
    {
        return new CreateAudioStreamCommand(name, new StreamUrl(url), codec, sampleRate, description);
    }

    /// <summary>
    /// Creates a new instance of the CreateAudioStreamCommand with all optional parameters.
    /// </summary>
    /// <param name="name">The display name for the audio stream.</param>
    /// <param name="url">The URL of the audio stream source.</param>
    /// <param name="codec">The audio codec used by the stream.</param>
    /// <param name="sampleRate">The sample rate of the audio stream in Hz.</param>
    /// <param name="description">Description of the audio stream.</param>
    /// <param name="bitrateKbps">The bitrate in kilobits per second.</param>
    /// <param name="channels">The number of audio channels.</param>
    /// <param name="tags">Additional metadata or tags.</param>
    /// <param name="requestedBy">The user or system that requested the stream creation.</param>
    /// <returns>A new CreateAudioStreamCommand instance.</returns>
    public static CreateAudioStreamCommand CreateDetailed(
        string name,
        string url,
        AudioCodec codec,
        int sampleRate,
        string description = "",
        int? bitrateKbps = null,
        int? channels = null,
        string? tags = null,
        string requestedBy = "System"
    )
    {
        return new CreateAudioStreamCommand(name, new StreamUrl(url), codec, sampleRate, description)
        {
            BitrateKbps = bitrateKbps,
            Channels = channels,
            Tags = tags,
            RequestedBy = requestedBy,
        };
    }
}
