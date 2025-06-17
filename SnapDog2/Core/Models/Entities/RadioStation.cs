using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;

namespace SnapDog2.Core.Models.Entities;

/// <summary>
/// Represents an internet radio station with name, URL, and codec information.
/// Immutable domain entity for the SnapDog2 multi-audio zone management system.
/// </summary>
public sealed record RadioStation
{
    /// <summary>
    /// Gets the unique identifier for the radio station.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the name of the radio station.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the stream URL of the radio station.
    /// </summary>
    public required StreamUrl Url { get; init; }

    /// <summary>
    /// Gets the audio codec used by the radio station.
    /// </summary>
    public required AudioCodec Codec { get; init; }

    /// <summary>
    /// Gets the description of the radio station.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the genre of the radio station.
    /// </summary>
    public string? Genre { get; init; }

    /// <summary>
    /// Gets the country or region of the radio station.
    /// </summary>
    public string? Country { get; init; }

    /// <summary>
    /// Gets the language of the radio station.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Gets the bitrate of the stream in kilobits per second.
    /// </summary>
    public int? BitrateKbps { get; init; }

    /// <summary>
    /// Gets the sample rate of the stream in Hz.
    /// </summary>
    public int? SampleRateHz { get; init; }

    /// <summary>
    /// Gets the number of audio channels.
    /// </summary>
    public int? Channels { get; init; }

    /// <summary>
    /// Gets the website URL of the radio station.
    /// </summary>
    public string? Website { get; init; }

    /// <summary>
    /// Gets the logo/icon URL of the radio station.
    /// </summary>
    public string? LogoUrl { get; init; }

    /// <summary>
    /// Gets additional tags associated with the radio station.
    /// </summary>
    public string? Tags { get; init; }

    /// <summary>
    /// Gets a value indicating whether the radio station is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Gets the priority for display ordering (higher = more priority).
    /// </summary>
    public int Priority { get; init; } = 1;

    /// <summary>
    /// Gets a value indicating whether the station requires authentication.
    /// </summary>
    public bool RequiresAuth { get; init; } = false;

    /// <summary>
    /// Gets the username for authentication (if required).
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets the password for authentication (if required).
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Gets the timestamp when the radio station was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the timestamp when the radio station was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the radio station was last played.
    /// </summary>
    public DateTime? LastPlayedAt { get; init; }

    /// <summary>
    /// Gets the number of times the radio station has been played.
    /// </summary>
    public int PlayCount { get; init; } = 0;

    /// <summary>
    /// Gets the timestamp when the station was last checked for availability.
    /// </summary>
    public DateTime? LastCheckedAt { get; init; }

    /// <summary>
    /// Gets a value indicating whether the station is currently available/online.
    /// </summary>
    public bool? IsOnline { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RadioStation"/> record.
    /// </summary>
    public RadioStation()
    {
        // Required properties must be set via object initializer
    }

    /// <summary>
    /// Creates a new radio station with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the radio station.</param>
    /// <param name="name">The name of the radio station.</param>
    /// <param name="url">The stream URL of the radio station.</param>
    /// <param name="codec">The audio codec used by the station.</param>
    /// <param name="description">Optional description of the radio station.</param>
    /// <returns>A new <see cref="RadioStation"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public static RadioStation Create(
        string id,
        string name,
        StreamUrl url,
        AudioCodec codec,
        string? description = null
    )
    {
        ValidateParameters(id, name);

        return new RadioStation
        {
            Id = id,
            Name = name,
            Url = url,
            Codec = codec,
            Description = description,
            CreatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current radio station with updated metadata.
    /// </summary>
    /// <param name="description">The updated description.</param>
    /// <param name="genre">The updated genre.</param>
    /// <param name="country">The updated country.</param>
    /// <param name="language">The updated language.</param>
    /// <returns>A new <see cref="RadioStation"/> instance with updated metadata.</returns>
    public RadioStation WithMetadata(
        string? description = null,
        string? genre = null,
        string? country = null,
        string? language = null
    )
    {
        return this with
        {
            Description = description ?? Description,
            Genre = genre ?? Genre,
            Country = country ?? Country,
            Language = language ?? Language,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current radio station with updated technical information.
    /// </summary>
    /// <param name="bitrateKbps">The bitrate in kilobits per second.</param>
    /// <param name="sampleRateHz">The sample rate in Hz.</param>
    /// <param name="channels">The number of audio channels.</param>
    /// <returns>A new <see cref="RadioStation"/> instance with updated technical information.</returns>
    public RadioStation WithTechnicalInfo(int? bitrateKbps = null, int? sampleRateHz = null, int? channels = null)
    {
        return this with
        {
            BitrateKbps = bitrateKbps ?? BitrateKbps,
            SampleRateHz = sampleRateHz ?? SampleRateHz,
            Channels = channels ?? Channels,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current radio station with updated URL.
    /// </summary>
    /// <param name="newUrl">The new stream URL.</param>
    /// <returns>A new <see cref="RadioStation"/> instance with updated URL.</returns>
    public RadioStation WithUrl(StreamUrl newUrl)
    {
        return this with { Url = newUrl, UpdatedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Creates a copy of the current radio station with updated enabled status.
    /// </summary>
    /// <param name="enabled">True to enable the station; false to disable.</param>
    /// <returns>A new <see cref="RadioStation"/> instance with updated enabled status.</returns>
    public RadioStation WithEnabled(bool enabled)
    {
        return this with { IsEnabled = enabled, UpdatedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Creates a copy of the current radio station with updated authentication settings.
    /// </summary>
    /// <param name="requiresAuth">Whether authentication is required.</param>
    /// <param name="username">The username for authentication.</param>
    /// <param name="password">The password for authentication.</param>
    /// <returns>A new <see cref="RadioStation"/> instance with updated authentication settings.</returns>
    public RadioStation WithAuth(bool requiresAuth, string? username = null, string? password = null)
    {
        return this with
        {
            RequiresAuth = requiresAuth,
            Username = requiresAuth ? username : null,
            Password = requiresAuth ? password : null,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current radio station with incremented play count and updated last played time.
    /// </summary>
    /// <returns>A new <see cref="RadioStation"/> instance with updated play statistics.</returns>
    public RadioStation WithPlayIncrement()
    {
        return this with { PlayCount = PlayCount + 1, LastPlayedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Creates a copy of the current radio station with updated online status.
    /// </summary>
    /// <param name="isOnline">Whether the station is currently online.</param>
    /// <returns>A new <see cref="RadioStation"/> instance with updated online status.</returns>
    public RadioStation WithOnlineStatus(bool isOnline)
    {
        return this with { IsOnline = isOnline, LastCheckedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Creates a copy of the current radio station with updated priority.
    /// </summary>
    /// <param name="priority">The new priority value.</param>
    /// <returns>A new <see cref="RadioStation"/> instance with updated priority.</returns>
    public RadioStation WithPriority(int priority)
    {
        return this with { Priority = priority, UpdatedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Gets a value indicating whether the radio station has been played.
    /// </summary>
    public bool HasBeenPlayed => PlayCount > 0;

    /// <summary>
    /// Gets a value indicating whether the radio station supports stereo audio.
    /// </summary>
    public bool IsStereo => Channels >= 2;

    /// <summary>
    /// Gets a value indicating whether the radio station is available (enabled and potentially online).
    /// </summary>
    public bool IsAvailable => IsEnabled && (IsOnline ?? true);

    /// <summary>
    /// Gets a value indicating whether the radio station has complete metadata.
    /// </summary>
    public bool HasCompleteMetadata =>
        !string.IsNullOrWhiteSpace(Genre)
        && !string.IsNullOrWhiteSpace(Country)
        && !string.IsNullOrWhiteSpace(Language);

    /// <summary>
    /// Gets a display name for the radio station with additional info.
    /// </summary>
    public string DisplayName
    {
        get
        {
            var parts = new List<string> { Name };

            if (!string.IsNullOrWhiteSpace(Country))
            {
                parts.Add($"({Country})");
            }

            if (!string.IsNullOrWhiteSpace(Genre))
            {
                parts.Add($"- {Genre}");
            }

            return string.Join(" ", parts);
        }
    }

    /// <summary>
    /// Gets the formatted bitrate string.
    /// </summary>
    public string FormattedBitrate => BitrateKbps.HasValue ? $"{BitrateKbps} kbps" : "Unknown";

    /// <summary>
    /// Gets the formatted codec and quality information.
    /// </summary>
    public string QualityInfo
    {
        get
        {
            var parts = new List<string> { Codec.ToString() };

            if (BitrateKbps.HasValue)
            {
                parts.Add($"{BitrateKbps} kbps");
            }

            if (IsStereo)
            {
                parts.Add("Stereo");
            }
            else if (Channels == 1)
            {
                parts.Add("Mono");
            }

            return string.Join(", ", parts);
        }
    }

    /// <summary>
    /// Validates the radio station parameters.
    /// </summary>
    /// <param name="id">The radio station ID to validate.</param>
    /// <param name="name">The radio station name to validate.</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    private static void ValidateParameters(string id, string name)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Radio station ID cannot be null or empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Radio station name cannot be null or empty.", nameof(name));
        }
    }
}
