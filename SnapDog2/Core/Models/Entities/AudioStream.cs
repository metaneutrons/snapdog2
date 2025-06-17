using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;

namespace SnapDog2.Core.Models.Entities;

/// <summary>
/// Represents an audio stream with codec, bitrate, URL, and status information.
/// Immutable domain entity for the SnapDog2 multi-audio zone management system.
/// </summary>
public sealed record AudioStream
{
    /// <summary>
    /// Gets the unique identifier for the audio stream.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the display name of the audio stream.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the URL of the audio stream.
    /// </summary>
    public required StreamUrl Url { get; init; }

    /// <summary>
    /// Gets the audio codec used by the stream.
    /// </summary>
    public required AudioCodec Codec { get; init; }

    /// <summary>
    /// Gets the bitrate of the stream in kilobits per second.
    /// </summary>
    public required int BitrateKbps { get; init; }

    /// <summary>
    /// Gets the current status of the stream.
    /// </summary>
    public required StreamStatus Status { get; init; }

    /// <summary>
    /// Gets the sample rate of the audio stream in Hz.
    /// </summary>
    public int? SampleRateHz { get; init; }

    /// <summary>
    /// Gets the number of audio channels (1 for mono, 2 for stereo, etc.).
    /// </summary>
    public int? Channels { get; init; }

    /// <summary>
    /// Gets the description of the audio stream.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets additional metadata or tags associated with the stream.
    /// </summary>
    public string? Tags { get; init; }

    /// <summary>
    /// Gets the timestamp when the stream was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the timestamp when the stream was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioStream"/> record.
    /// </summary>
    public AudioStream()
    {
        // Required properties must be set via object initializer
    }

    /// <summary>
    /// Creates a new audio stream with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the stream.</param>
    /// <param name="name">The display name of the stream.</param>
    /// <param name="url">The URL of the stream.</param>
    /// <param name="codec">The audio codec used by the stream.</param>
    /// <param name="bitrateKbps">The bitrate in kilobits per second.</param>
    /// <param name="status">The current status of the stream.</param>
    /// <returns>A new <see cref="AudioStream"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public static AudioStream Create(
        string id,
        string name,
        StreamUrl url,
        AudioCodec codec,
        int bitrateKbps,
        StreamStatus status = StreamStatus.Stopped
    )
    {
        ValidateParameters(id, name, bitrateKbps);

        return new AudioStream
        {
            Id = id,
            Name = name,
            Url = url,
            Codec = codec,
            BitrateKbps = bitrateKbps,
            Status = status,
            CreatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current stream with updated status.
    /// </summary>
    /// <param name="newStatus">The new status to set.</param>
    /// <returns>A new <see cref="AudioStream"/> instance with updated status.</returns>
    public AudioStream WithStatus(StreamStatus newStatus)
    {
        return this with { Status = newStatus, UpdatedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Creates a copy of the current stream with updated bitrate.
    /// </summary>
    /// <param name="newBitrateKbps">The new bitrate in kilobits per second.</param>
    /// <returns>A new <see cref="AudioStream"/> instance with updated bitrate.</returns>
    /// <exception cref="ArgumentException">Thrown when bitrate is invalid.</exception>
    public AudioStream WithBitrate(int newBitrateKbps)
    {
        if (newBitrateKbps <= 0)
        {
            throw new ArgumentException("Bitrate must be greater than zero.", nameof(newBitrateKbps));
        }

        return this with
        {
            BitrateKbps = newBitrateKbps,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current stream with updated URL.
    /// </summary>
    /// <param name="newUrl">The new stream URL.</param>
    /// <returns>A new <see cref="AudioStream"/> instance with updated URL.</returns>
    public AudioStream WithUrl(StreamUrl newUrl)
    {
        return this with { Url = newUrl, UpdatedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Gets a value indicating whether the stream is currently playing.
    /// </summary>
    public bool IsPlaying => Status == StreamStatus.Playing;

    /// <summary>
    /// Gets a value indicating whether the stream is stopped.
    /// </summary>
    public bool IsStopped => Status == StreamStatus.Stopped;

    /// <summary>
    /// Gets a value indicating whether the stream has an error.
    /// </summary>
    public bool HasError => Status == StreamStatus.Error;

    /// <summary>
    /// Gets a value indicating whether the stream supports stereo audio.
    /// </summary>
    public bool IsStereo => Channels >= 2;

    /// <summary>
    /// Validates the stream parameters.
    /// </summary>
    /// <param name="id">The stream ID to validate.</param>
    /// <param name="name">The stream name to validate.</param>
    /// <param name="bitrateKbps">The bitrate to validate.</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    private static void ValidateParameters(string id, string name, int bitrateKbps)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Stream ID cannot be null or empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Stream name cannot be null or empty.", nameof(name));
        }

        if (bitrateKbps <= 0)
        {
            throw new ArgumentException("Bitrate must be greater than zero.", nameof(bitrateKbps));
        }
    }
}
