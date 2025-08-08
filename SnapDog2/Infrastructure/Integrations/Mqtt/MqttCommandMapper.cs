namespace SnapDog2.Infrastructure.Integrations.Mqtt;

using Microsoft.Extensions.Logging;
using SnapDog2.Server.Features.Shared.Factories;

/// <summary>
/// MQTT command mapper using the centralized CommandFactory.
/// </summary>
public partial class MqttCommandMapper
{
    private readonly ILogger<MqttCommandMapper> _logger;

    [LoggerMessage(8001, LogLevel.Debug, "Mapping MQTT command: {Topic} -> {Payload}")]
    private partial void LogMappingCommand(string topic, string payload);

    [LoggerMessage(8002, LogLevel.Warning, "Failed to map MQTT topic: {Topic}")]
    private partial void LogMappingFailed(string topic);

    [LoggerMessage(8003, LogLevel.Error, "Error mapping MQTT command for topic {Topic}: {Error}")]
    private partial void LogMappingError(string topic, string error);

    public MqttCommandMapper(ILogger<MqttCommandMapper> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Maps MQTT topics to Cortex.Mediator commands using the centralized CommandFactory.
    /// </summary>
    public object? MapTopicToCommand(string topic, string payload)
    {
        try
        {
            LogMappingCommand(topic, payload);

            // Use the factory extension method for MQTT topic parsing
            var command = CommandFactoryExtensions.CreateFromMqttTopic(topic, payload);

            if (command == null)
            {
                LogMappingFailed(topic);
            }

            return command;
        }
        catch (Exception ex)
        {
            LogMappingError(topic, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Maps complex control topics that contain multiple commands in a single payload.
    /// Example: snapdog/zone/1/control/set with payload "play track 5"
    /// </summary>
    public object? MapControlTopicToCommand(string topic, string payload)
    {
        try
        {
            LogMappingCommand(topic, payload);

            var parts = topic.Split('/');
            if (
                parts.Length < 5
                || !parts[0].Equals("snapdog", StringComparison.OrdinalIgnoreCase)
                || !parts[3].Equals("control", StringComparison.OrdinalIgnoreCase)
                || !parts[4].Equals("set", StringComparison.OrdinalIgnoreCase)
            )
            {
                return null;
            }

            var entityType = parts[1].ToLowerInvariant();
            var entityIdStr = parts[2];

            if (!int.TryParse(entityIdStr, out var entityId))
                return null;

            // Parse complex control payloads
            var payloadParts = payload.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (payloadParts.Length == 0)
                return null;

            var command = payloadParts[0].ToLowerInvariant();
            var parameter = payloadParts.Length > 1 ? payloadParts[1] : string.Empty;

            return entityType switch
            {
                "zone" => MapZoneControlCommand(entityId, command, parameter),
                "client" => MapClientControlCommand(entityId, command, parameter),
                _ => null,
            };
        }
        catch (Exception ex)
        {
            LogMappingError(topic, ex.Message);
            return null;
        }
    }

    private object? MapZoneControlCommand(int zoneId, string command, string parameter)
    {
        return command switch
        {
            // Playback commands
            "play" when string.IsNullOrEmpty(parameter) => CommandFactory.CreatePlayCommand(
                zoneId,
                Core.Enums.CommandSource.Mqtt
            ),
            "play"
                when parameter.StartsWith("track", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(parameter[5..].Trim(), out var trackIndex) => CommandFactory.CreatePlayTrackCommand(
                zoneId,
                trackIndex,
                Core.Enums.CommandSource.Mqtt
            ),
            "play" when parameter.StartsWith("url", StringComparison.OrdinalIgnoreCase) =>
                CommandFactory.CreatePlayUrlCommand(zoneId, parameter[3..].Trim(), Core.Enums.CommandSource.Mqtt),
            "pause" => CommandFactory.CreatePauseCommand(zoneId, Core.Enums.CommandSource.Mqtt),
            "stop" => CommandFactory.CreateStopCommand(zoneId, Core.Enums.CommandSource.Mqtt),

            // Navigation commands
            "next" or "track_next" => CommandFactory.CreateNextTrackCommand(zoneId, Core.Enums.CommandSource.Mqtt),
            "previous" or "track_previous" => CommandFactory.CreatePreviousTrackCommand(
                zoneId,
                Core.Enums.CommandSource.Mqtt
            ),
            "playlist_next" => CommandFactory.CreateNextPlaylistCommand(zoneId, Core.Enums.CommandSource.Mqtt),
            "playlist_previous" => CommandFactory.CreatePreviousPlaylistCommand(zoneId, Core.Enums.CommandSource.Mqtt),

            // Volume commands
            "volume" when int.TryParse(parameter, out var volume) => CommandFactory.CreateSetZoneVolumeCommand(
                zoneId,
                volume,
                Core.Enums.CommandSource.Mqtt
            ),
            "volume_up" => CommandFactory.CreateVolumeUpCommand(zoneId, 5, Core.Enums.CommandSource.Mqtt),
            "volume_down" => CommandFactory.CreateVolumeDownCommand(zoneId, 5, Core.Enums.CommandSource.Mqtt),

            // Mute commands
            "mute_on" => CommandFactory.CreateSetZoneMuteCommand(zoneId, true, Core.Enums.CommandSource.Mqtt),
            "mute_off" => CommandFactory.CreateSetZoneMuteCommand(zoneId, false, Core.Enums.CommandSource.Mqtt),
            "mute_toggle" => CommandFactory.CreateToggleZoneMuteCommand(zoneId, Core.Enums.CommandSource.Mqtt),

            // Track repeat commands
            "track_repeat_on" => CommandFactory.CreateSetTrackRepeatCommand(
                zoneId,
                true,
                Core.Enums.CommandSource.Mqtt
            ),
            "track_repeat_off" => CommandFactory.CreateSetTrackRepeatCommand(
                zoneId,
                false,
                Core.Enums.CommandSource.Mqtt
            ),
            "track_repeat_toggle" => CommandFactory.CreateToggleTrackRepeatCommand(
                zoneId,
                Core.Enums.CommandSource.Mqtt
            ),

            // Shuffle commands
            "shuffle_on" => CommandFactory.CreateSetPlaylistShuffleCommand(zoneId, true, Core.Enums.CommandSource.Mqtt),
            "shuffle_off" => CommandFactory.CreateSetPlaylistShuffleCommand(
                zoneId,
                false,
                Core.Enums.CommandSource.Mqtt
            ),
            "shuffle_toggle" => CommandFactory.CreateTogglePlaylistShuffleCommand(
                zoneId,
                Core.Enums.CommandSource.Mqtt
            ),

            // Playlist repeat commands
            "playlist_repeat_on" => CommandFactory.CreateSetPlaylistRepeatCommand(
                zoneId,
                true,
                Core.Enums.CommandSource.Mqtt
            ),
            "playlist_repeat_off" => CommandFactory.CreateSetPlaylistRepeatCommand(
                zoneId,
                false,
                Core.Enums.CommandSource.Mqtt
            ),
            "playlist_repeat_toggle" => CommandFactory.CreateTogglePlaylistRepeatCommand(
                zoneId,
                Core.Enums.CommandSource.Mqtt
            ),

            // Track and playlist selection
            "track" when int.TryParse(parameter, out var trackNum) => CommandFactory.CreateSetTrackCommand(
                zoneId,
                trackNum,
                Core.Enums.CommandSource.Mqtt
            ),
            "playlist" when int.TryParse(parameter, out var playlistNum) => CommandFactory.CreateSetPlaylistCommand(
                zoneId,
                playlistNum,
                Core.Enums.CommandSource.Mqtt
            ),
            "playlist" when !string.IsNullOrEmpty(parameter) => CommandFactory.CreateSetPlaylistCommand(
                zoneId,
                parameter,
                Core.Enums.CommandSource.Mqtt
            ),

            _ => null,
        };
    }

    private object? MapClientControlCommand(int clientId, string command, string parameter)
    {
        return command switch
        {
            // Volume commands
            "volume" when int.TryParse(parameter, out var volume) => CommandFactory.CreateSetClientVolumeCommand(
                clientId,
                volume,
                Core.Enums.CommandSource.Mqtt
            ),

            // Mute commands
            "mute_on" => CommandFactory.CreateSetClientMuteCommand(clientId, true, Core.Enums.CommandSource.Mqtt),
            "mute_off" => CommandFactory.CreateSetClientMuteCommand(clientId, false, Core.Enums.CommandSource.Mqtt),
            "mute_toggle" => CommandFactory.CreateToggleClientMuteCommand(clientId, Core.Enums.CommandSource.Mqtt),

            // Zone assignment
            "zone" when int.TryParse(parameter, out var zoneId) => CommandFactory.CreateAssignClientToZoneCommand(
                clientId,
                zoneId,
                Core.Enums.CommandSource.Mqtt
            ),

            // Latency adjustment
            "latency" when int.TryParse(parameter, out var latency) => CommandFactory.CreateSetClientLatencyCommand(
                clientId,
                latency,
                Core.Enums.CommandSource.Mqtt
            ),

            _ => null,
        };
    }

    /// <summary>
    /// Validates that a topic follows the expected MQTT topic structure.
    /// </summary>
    public bool IsValidMqttTopic(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
            return false;

        var parts = topic.Split('/');

        // Basic structure: snapdog/{entity}/{id}/{command}[/set]
        if (parts.Length < 4 || parts.Length > 5)
            return false;

        // Must start with snapdog
        if (!parts[0].Equals("snapdog", StringComparison.OrdinalIgnoreCase))
            return false;

        // Entity type must be zone or client
        var entityType = parts[1].ToLowerInvariant();
        if (entityType != "zone" && entityType != "client")
            return false;

        // Entity ID must be a positive integer
        if (!int.TryParse(parts[2], out var entityId) || entityId <= 0)
            return false;

        // Command must not be empty
        if (string.IsNullOrWhiteSpace(parts[3]))
            return false;

        // If 5 parts, last part should be "set"
        if (parts.Length == 5 && !parts[4].Equals("set", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    /// <summary>
    /// Gets the entity type (zone/client) from an MQTT topic.
    /// </summary>
    public string? GetEntityType(string topic)
    {
        if (!IsValidMqttTopic(topic))
            return null;

        var parts = topic.Split('/');
        return parts[1].ToLowerInvariant();
    }

    /// <summary>
    /// Gets the entity ID from an MQTT topic.
    /// </summary>
    public int? GetEntityId(string topic)
    {
        if (!IsValidMqttTopic(topic))
            return null;

        var parts = topic.Split('/');
        return int.TryParse(parts[2], out var entityId) ? entityId : null;
    }

    /// <summary>
    /// Gets the command name from an MQTT topic.
    /// </summary>
    public string? GetCommandName(string topic)
    {
        if (!IsValidMqttTopic(topic))
            return null;

        var parts = topic.Split('/');
        return parts[3].ToLowerInvariant();
    }
}
