using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;

namespace SnapDog2.Server.Models;

/// <summary>
/// Response model for audio stream operations.
/// Contains formatted data for API responses and client consumption.
/// </summary>
/// <param name="Id">The unique identifier of the audio stream.</param>
/// <param name="Name">The display name of the audio stream.</param>
/// <param name="Url">The URL of the audio stream source.</param>
/// <param name="Codec">The audio codec used by the stream.</param>
/// <param name="SampleRate">The sample rate of the audio stream in Hz.</param>
/// <param name="Status">The current status of the stream.</param>
/// <param name="Description">The description of the audio stream.</param>
/// <param name="CreatedAt">The timestamp when the stream was created.</param>
/// <param name="LastStartedAt">The timestamp when the stream was last started.</param>
public record AudioStreamResponse(
    string Id,
    string Name,
    string Url,
    string Codec,
    int SampleRate,
    string Status,
    string Description,
    DateTime CreatedAt,
    DateTime? LastStartedAt
)
{
    /// <summary>
    /// Gets the bitrate of the stream in kilobits per second.
    /// </summary>
    public int BitrateKbps { get; init; }

    /// <summary>
    /// Gets the number of audio channels.
    /// </summary>
    public int? Channels { get; init; }

    /// <summary>
    /// Gets additional metadata or tags associated with the stream.
    /// </summary>
    public string? Tags { get; init; }

    /// <summary>
    /// Gets the timestamp when the stream was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Gets a value indicating whether the stream is currently active (playing or starting).
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Gets a value indicating whether the stream supports stereo audio.
    /// </summary>
    public bool IsStereo { get; init; }

    /// <summary>
    /// Creates an AudioStreamResponse from an AudioStream entity.
    /// </summary>
    /// <param name="audioStream">The audio stream entity.</param>
    /// <returns>A new AudioStreamResponse instance.</returns>
    public static AudioStreamResponse FromEntity(AudioStream audioStream)
    {
        ArgumentNullException.ThrowIfNull(audioStream);

        return new AudioStreamResponse(
            audioStream.Id,
            audioStream.Name,
            audioStream.Url.ToString() ?? string.Empty,
            audioStream.Codec.ToString(),
            audioStream.SampleRateHz ?? 0,
            audioStream.Status.ToString(),
            audioStream.Description ?? string.Empty,
            audioStream.CreatedAt,
            GetLastStartedAt(audioStream)
        )
        {
            BitrateKbps = audioStream.BitrateKbps,
            Channels = audioStream.Channels,
            Tags = audioStream.Tags,
            UpdatedAt = audioStream.UpdatedAt,
            IsActive = audioStream.IsPlaying || audioStream.Status == StreamStatus.Starting,
            IsStereo = audioStream.IsStereo,
        };
    }

    /// <summary>
    /// Creates multiple AudioStreamResponse instances from a collection of AudioStream entities.
    /// </summary>
    /// <param name="audioStreams">The collection of audio stream entities.</param>
    /// <returns>A collection of AudioStreamResponse instances.</returns>
    public static IEnumerable<AudioStreamResponse> FromEntities(IEnumerable<AudioStream> audioStreams)
    {
        ArgumentNullException.ThrowIfNull(audioStreams);

        return audioStreams.Select(FromEntity);
    }

    /// <summary>
    /// Creates a summary AudioStreamResponse with basic information only.
    /// </summary>
    /// <param name="audioStream">The audio stream entity.</param>
    /// <returns>A simplified AudioStreamResponse instance.</returns>
    public static AudioStreamResponse CreateSummary(AudioStream audioStream)
    {
        ArgumentNullException.ThrowIfNull(audioStream);

        return new AudioStreamResponse(
            audioStream.Id,
            audioStream.Name,
            audioStream.Url.ToString() ?? string.Empty,
            audioStream.Codec.ToString(),
            audioStream.SampleRateHz ?? 0,
            audioStream.Status.ToString(),
            audioStream.Description ?? string.Empty,
            audioStream.CreatedAt,
            GetLastStartedAt(audioStream)
        )
        {
            BitrateKbps = audioStream.BitrateKbps,
            IsActive = audioStream.IsPlaying || audioStream.Status == StreamStatus.Starting,
        };
    }

    /// <summary>
    /// Gets the formatted codec information with additional details.
    /// </summary>
    /// <returns>Formatted codec string with bitrate and sample rate.</returns>
    public string GetFormattedCodecInfo()
    {
        var codecInfo = $"{Codec} {BitrateKbps} kbps";
        if (SampleRate > 0)
        {
            codecInfo += $" @ {SampleRate} Hz";
        }
        if (Channels.HasValue)
        {
            var channelInfo = Channels.Value switch
            {
                1 => "Mono",
                2 => "Stereo",
                _ => $"{Channels.Value} Channel",
            };
            codecInfo += $" ({channelInfo})";
        }
        return codecInfo;
    }

    /// <summary>
    /// Gets the formatted duration since creation.
    /// </summary>
    /// <returns>Human-readable duration string.</returns>
    public string GetTimeSinceCreation()
    {
        var duration = DateTime.UtcNow - CreatedAt;
        return FormatDuration(duration);
    }

    /// <summary>
    /// Gets the formatted duration since last update.
    /// </summary>
    /// <returns>Human-readable duration string or null if never updated.</returns>
    public string? GetTimeSinceLastUpdate()
    {
        if (!UpdatedAt.HasValue)
        {
            return null;
        }

        var duration = DateTime.UtcNow - UpdatedAt.Value;
        return FormatDuration(duration);
    }

    /// <summary>
    /// Determines the last started timestamp from the audio stream.
    /// This would typically come from event history or audit logs in a real implementation.
    /// </summary>
    /// <param name="audioStream">The audio stream entity.</param>
    /// <returns>The last started timestamp or null if never started.</returns>
    private static DateTime? GetLastStartedAt(AudioStream audioStream)
    {
        // In a real implementation, this would query event history or audit logs
        // For now, we'll use UpdatedAt if the stream is currently playing
        return audioStream.IsPlaying ? audioStream.UpdatedAt : null;
    }

    /// <summary>
    /// Formats a duration into a human-readable string.
    /// </summary>
    /// <param name="duration">The duration to format.</param>
    /// <returns>Formatted duration string.</returns>
    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
        {
            return $"{(int)duration.TotalDays}d {duration.Hours}h";
        }
        if (duration.TotalHours >= 1)
        {
            return $"{duration.Hours}h {duration.Minutes}m";
        }
        if (duration.TotalMinutes >= 1)
        {
            return $"{duration.Minutes}m {duration.Seconds}s";
        }
        return $"{duration.Seconds}s";
    }
}
