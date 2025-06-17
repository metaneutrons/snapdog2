using System.Collections.Immutable;

namespace SnapDog2.Core.Models.Entities;

/// <summary>
/// Represents an audio zone with ID, name, clients, and current stream information.
/// Immutable domain entity for the SnapDog2 multi-audio zone management system.
/// </summary>
public sealed record Zone
{
    /// <summary>
    /// Gets the unique identifier for the zone.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the display name of the zone.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the immutable collection of client IDs assigned to this zone.
    /// </summary>
    public ImmutableList<string> ClientIds { get; init; } = ImmutableList<string>.Empty;

    /// <summary>
    /// Gets the ID of the current audio stream playing in this zone.
    /// </summary>
    public string? CurrentStreamId { get; init; }

    /// <summary>
    /// Gets the description of the zone.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the physical location of the zone.
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// Gets the zone color (hex code) for UI display.
    /// </summary>
    public string Color { get; init; } = "#007bff";

    /// <summary>
    /// Gets the zone icon identifier for UI display.
    /// </summary>
    public string Icon { get; init; } = "speaker";

    /// <summary>
    /// Gets the default volume level for this zone (0-100).
    /// </summary>
    public int DefaultVolume { get; init; } = 50;

    /// <summary>
    /// Gets the maximum volume level for this zone (0-100).
    /// </summary>
    public int MaxVolume { get; init; } = 100;

    /// <summary>
    /// Gets the minimum volume level for this zone (0-100).
    /// </summary>
    public int MinVolume { get; init; } = 0;

    /// <summary>
    /// Gets a value indicating whether the zone is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Gets the zone priority for automatic assignments (higher = more priority).
    /// </summary>
    public int Priority { get; init; } = 1;

    /// <summary>
    /// Gets additional tags associated with the zone.
    /// </summary>
    public string? Tags { get; init; }

    /// <summary>
    /// Gets a value indicating whether stereo playback is supported.
    /// </summary>
    public bool StereoEnabled { get; init; } = true;

    /// <summary>
    /// Gets the audio quality setting for this zone.
    /// </summary>
    public string AudioQuality { get; init; } = "high";

    /// <summary>
    /// Gets a value indicating whether zone grouping is allowed.
    /// </summary>
    public bool GroupingEnabled { get; init; } = true;

    /// <summary>
    /// Gets the timestamp when the zone was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the timestamp when the zone was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Zone"/> record.
    /// </summary>
    public Zone()
    {
        // Required properties must be set via object initializer
    }

    /// <summary>
    /// Creates a new zone with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the zone.</param>
    /// <param name="name">The display name of the zone.</param>
    /// <param name="description">Optional description of the zone.</param>
    /// <returns>A new <see cref="Zone"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public static Zone Create(string id, string name, string? description = null)
    {
        ValidateParameters(id, name);

        return new Zone
        {
            Id = id,
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current zone with an added client.
    /// </summary>
    /// <param name="clientId">The client ID to add to the zone.</param>
    /// <returns>A new <see cref="Zone"/> instance with the client added.</returns>
    /// <exception cref="ArgumentException">Thrown when client ID is invalid or already exists.</exception>
    public Zone WithAddedClient(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException("Client ID cannot be null or empty.", nameof(clientId));
        }

        if (ClientIds.Contains(clientId))
        {
            throw new ArgumentException($"Client '{clientId}' is already assigned to zone '{Id}'.", nameof(clientId));
        }

        return this with
        {
            ClientIds = ClientIds.Add(clientId),
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current zone with a removed client.
    /// </summary>
    /// <param name="clientId">The client ID to remove from the zone.</param>
    /// <returns>A new <see cref="Zone"/> instance with the client removed.</returns>
    /// <exception cref="ArgumentException">Thrown when client ID is invalid or not found.</exception>
    public Zone WithRemovedClient(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException("Client ID cannot be null or empty.", nameof(clientId));
        }

        if (!ClientIds.Contains(clientId))
        {
            throw new ArgumentException($"Client '{clientId}' is not assigned to zone '{Id}'.", nameof(clientId));
        }

        return this with
        {
            ClientIds = ClientIds.Remove(clientId),
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current zone with updated client list.
    /// </summary>
    /// <param name="clientIds">The new list of client IDs.</param>
    /// <returns>A new <see cref="Zone"/> instance with updated client list.</returns>
    /// <exception cref="ArgumentNullException">Thrown when client IDs list is null.</exception>
    public Zone WithClients(IEnumerable<string> clientIds)
    {
        if (clientIds == null)
        {
            throw new ArgumentNullException(nameof(clientIds));
        }

        var validClientIds = clientIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
        var duplicates = validClientIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key);

        if (duplicates.Any())
        {
            throw new ArgumentException(
                $"Duplicate client IDs found: {string.Join(", ", duplicates)}",
                nameof(clientIds)
            );
        }

        return this with
        {
            ClientIds = validClientIds.ToImmutableList(),
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current zone with updated current stream.
    /// </summary>
    /// <param name="streamId">The stream ID to set as current, or null to clear.</param>
    /// <returns>A new <see cref="Zone"/> instance with updated current stream.</returns>
    public Zone WithCurrentStream(string? streamId)
    {
        return this with { CurrentStreamId = streamId, UpdatedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Creates a copy of the current zone with updated enabled status.
    /// </summary>
    /// <param name="enabled">True to enable the zone; false to disable.</param>
    /// <returns>A new <see cref="Zone"/> instance with updated enabled status.</returns>
    public Zone WithEnabled(bool enabled)
    {
        return this with { IsEnabled = enabled, UpdatedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Creates a copy of the current zone with updated volume settings.
    /// </summary>
    /// <param name="defaultVolume">The default volume level (0-100).</param>
    /// <param name="minVolume">The minimum volume level (0-100).</param>
    /// <param name="maxVolume">The maximum volume level (0-100).</param>
    /// <returns>A new <see cref="Zone"/> instance with updated volume settings.</returns>
    /// <exception cref="ArgumentException">Thrown when volume values are invalid.</exception>
    public Zone WithVolumeSettings(int defaultVolume, int minVolume = 0, int maxVolume = 100)
    {
        ValidateVolumeSettings(defaultVolume, minVolume, maxVolume);

        return this with
        {
            DefaultVolume = defaultVolume,
            MinVolume = minVolume,
            MaxVolume = maxVolume,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Gets a value indicating whether the zone has any clients assigned.
    /// </summary>
    public bool HasClients => ClientIds.Count > 0;

    /// <summary>
    /// Gets the number of clients assigned to this zone.
    /// </summary>
    public int ClientCount => ClientIds.Count;

    /// <summary>
    /// Gets a value indicating whether the zone has a current stream.
    /// </summary>
    public bool HasCurrentStream => !string.IsNullOrWhiteSpace(CurrentStreamId);

    /// <summary>
    /// Gets a value indicating whether the zone is active (enabled and has clients).
    /// </summary>
    public bool IsActive => IsEnabled && HasClients;

    /// <summary>
    /// Determines if the zone contains the specified client.
    /// </summary>
    /// <param name="clientId">The client ID to check.</param>
    /// <returns>True if the zone contains the client; otherwise, false.</returns>
    public bool ContainsClient(string clientId)
    {
        return !string.IsNullOrWhiteSpace(clientId) && ClientIds.Contains(clientId);
    }

    /// <summary>
    /// Validates the zone parameters.
    /// </summary>
    /// <param name="id">The zone ID to validate.</param>
    /// <param name="name">The zone name to validate.</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    private static void ValidateParameters(string id, string name)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Zone ID cannot be null or empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Zone name cannot be null or empty.", nameof(name));
        }
    }

    /// <summary>
    /// Validates volume settings.
    /// </summary>
    /// <param name="defaultVolume">The default volume to validate.</param>
    /// <param name="minVolume">The minimum volume to validate.</param>
    /// <param name="maxVolume">The maximum volume to validate.</param>
    /// <exception cref="ArgumentException">Thrown when volume settings are invalid.</exception>
    private static void ValidateVolumeSettings(int defaultVolume, int minVolume, int maxVolume)
    {
        if (minVolume < 0 || minVolume > 100)
        {
            throw new ArgumentException("Minimum volume must be between 0 and 100.", nameof(minVolume));
        }

        if (maxVolume < 0 || maxVolume > 100)
        {
            throw new ArgumentException("Maximum volume must be between 0 and 100.", nameof(maxVolume));
        }

        if (minVolume > maxVolume)
        {
            throw new ArgumentException("Minimum volume cannot be greater than maximum volume.");
        }

        if (defaultVolume < minVolume || defaultVolume > maxVolume)
        {
            throw new ArgumentException(
                $"Default volume must be between {minVolume} and {maxVolume}.",
                nameof(defaultVolume)
            );
        }
    }
}
