namespace SnapDog2.Server.Features.Shared.Factories;

using System.Globalization;
using SnapDog2.Core.Enums;
using SnapDog2.Server.Features.Clients.Commands.Config;
using SnapDog2.Server.Features.Clients.Commands.Volume;
using SnapDog2.Server.Features.Zones.Commands.Playback;
using SnapDog2.Server.Features.Zones.Commands.Playlist;
using SnapDog2.Server.Features.Zones.Commands.Track;
using SnapDog2.Server.Features.Zones.Commands.Volume;

/// <summary>
/// Centralized factory for creating Cortex.Mediator commands.
/// Provides type-safe command creation with consistent parameter validation.
/// </summary>
public static class CommandFactory
{
    #region Zone Playback Commands

    /// <summary>
    /// Creates a PlayCommand for the specified zone.
    /// </summary>
    public static PlayCommand CreatePlayCommand(int zoneId, CommandSource source = CommandSource.Internal)
    {
        return new PlayCommand { ZoneId = zoneId, Source = source };
    }

    /// <summary>
    /// Creates a PlayCommand with a specific track index.
    /// </summary>
    public static PlayCommand CreatePlayTrackCommand(
        int zoneId,
        int trackIndex,
        CommandSource source = CommandSource.Internal
    )
    {
        return new PlayCommand
        {
            ZoneId = zoneId,
            TrackIndex = trackIndex,
            Source = source,
        };
    }

    /// <summary>
    /// Creates a PlayCommand with a media URL.
    /// </summary>
    public static PlayCommand CreatePlayUrlCommand(
        int zoneId,
        string mediaUrl,
        CommandSource source = CommandSource.Internal
    )
    {
        return new PlayCommand
        {
            ZoneId = zoneId,
            MediaUrl = mediaUrl,
            Source = source,
        };
    }

    /// <summary>
    /// Creates a PauseCommand for the specified zone.
    /// </summary>
    public static PauseCommand CreatePauseCommand(int zoneId, CommandSource source = CommandSource.Internal)
    {
        return new PauseCommand { ZoneId = zoneId, Source = source };
    }

    /// <summary>
    /// Creates a StopCommand for the specified zone.
    /// </summary>
    public static StopCommand CreateStopCommand(int zoneId, CommandSource source = CommandSource.Internal)
    {
        return new StopCommand { ZoneId = zoneId, Source = source };
    }

    #endregion

    #region Zone Volume Commands

    /// <summary>
    /// Creates a SetZoneVolumeCommand with the specified volume level.
    /// </summary>
    public static SetZoneVolumeCommand CreateSetZoneVolumeCommand(
        int zoneId,
        int volume,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetZoneVolumeCommand
        {
            ZoneId = zoneId,
            Volume = Math.Clamp(volume, 0, 100),
            Source = source,
        };
    }

    /// <summary>
    /// Creates a VolumeUpCommand with the specified step.
    /// </summary>
    public static VolumeUpCommand CreateVolumeUpCommand(
        int zoneId,
        int step = 5,
        CommandSource source = CommandSource.Internal
    )
    {
        return new VolumeUpCommand
        {
            ZoneId = zoneId,
            Step = Math.Clamp(step, 1, 50),
            Source = source,
        };
    }

    /// <summary>
    /// Creates a VolumeDownCommand with the specified step.
    /// </summary>
    public static VolumeDownCommand CreateVolumeDownCommand(
        int zoneId,
        int step = 5,
        CommandSource source = CommandSource.Internal
    )
    {
        return new VolumeDownCommand
        {
            ZoneId = zoneId,
            Step = Math.Clamp(step, 1, 50),
            Source = source,
        };
    }

    /// <summary>
    /// Creates a SetZoneMuteCommand with the specified mute state.
    /// </summary>
    public static SetZoneMuteCommand CreateSetZoneMuteCommand(
        int zoneId,
        bool enabled,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetZoneMuteCommand
        {
            ZoneId = zoneId,
            Enabled = enabled,
            Source = source,
        };
    }

    /// <summary>
    /// Creates a ToggleZoneMuteCommand.
    /// </summary>
    public static ToggleZoneMuteCommand CreateToggleZoneMuteCommand(
        int zoneId,
        CommandSource source = CommandSource.Internal
    )
    {
        return new ToggleZoneMuteCommand { ZoneId = zoneId, Source = source };
    }

    #endregion

    #region Zone Track Commands

    /// <summary>
    /// Creates a SetTrackCommand with the specified track index.
    /// </summary>
    public static SetTrackCommand CreateSetTrackCommand(
        int zoneId,
        int trackIndex,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetTrackCommand
        {
            ZoneId = zoneId,
            TrackIndex = Math.Max(1, trackIndex),
            Source = source,
        };
    }

    /// <summary>
    /// Creates a NextTrackCommand.
    /// </summary>
    public static NextTrackCommand CreateNextTrackCommand(int zoneId, CommandSource source = CommandSource.Internal)
    {
        return new NextTrackCommand { ZoneId = zoneId, Source = source };
    }

    /// <summary>
    /// Creates a PreviousTrackCommand.
    /// </summary>
    public static PreviousTrackCommand CreatePreviousTrackCommand(
        int zoneId,
        CommandSource source = CommandSource.Internal
    )
    {
        return new PreviousTrackCommand { ZoneId = zoneId, Source = source };
    }

    /// <summary>
    /// Creates a SetTrackRepeatCommand with the specified repeat state.
    /// </summary>
    public static SetTrackRepeatCommand CreateSetTrackRepeatCommand(
        int zoneId,
        bool enabled,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetTrackRepeatCommand
        {
            ZoneId = zoneId,
            Enabled = enabled,
            Source = source,
        };
    }

    /// <summary>
    /// Creates a ToggleTrackRepeatCommand.
    /// </summary>
    public static ToggleTrackRepeatCommand CreateToggleTrackRepeatCommand(
        int zoneId,
        CommandSource source = CommandSource.Internal
    )
    {
        return new ToggleTrackRepeatCommand { ZoneId = zoneId, Source = source };
    }

    #endregion

    #region Zone Playlist Commands

    /// <summary>
    /// Creates a SetPlaylistCommand with the specified playlist index.
    /// </summary>
    public static SetPlaylistCommand CreateSetPlaylistCommand(
        int zoneId,
        int playlistIndex,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetPlaylistCommand
        {
            ZoneId = zoneId,
            PlaylistIndex = Math.Max(1, playlistIndex),
            Source = source,
        };
    }

    /// <summary>
    /// Creates a SetPlaylistCommand with the specified playlist ID.
    /// </summary>
    public static SetPlaylistCommand CreateSetPlaylistCommand(
        int zoneId,
        string playlistId,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetPlaylistCommand
        {
            ZoneId = zoneId,
            PlaylistId = playlistId,
            Source = source,
        };
    }

    /// <summary>
    /// Creates a NextPlaylistCommand.
    /// </summary>
    public static NextPlaylistCommand CreateNextPlaylistCommand(
        int zoneId,
        CommandSource source = CommandSource.Internal
    )
    {
        return new NextPlaylistCommand { ZoneId = zoneId, Source = source };
    }

    /// <summary>
    /// Creates a PreviousPlaylistCommand.
    /// </summary>
    public static PreviousPlaylistCommand CreatePreviousPlaylistCommand(
        int zoneId,
        CommandSource source = CommandSource.Internal
    )
    {
        return new PreviousPlaylistCommand { ZoneId = zoneId, Source = source };
    }

    /// <summary>
    /// Creates a SetPlaylistShuffleCommand with the specified shuffle state.
    /// </summary>
    public static SetPlaylistShuffleCommand CreateSetPlaylistShuffleCommand(
        int zoneId,
        bool enabled,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetPlaylistShuffleCommand
        {
            ZoneId = zoneId,
            Enabled = enabled,
            Source = source,
        };
    }

    /// <summary>
    /// Creates a TogglePlaylistShuffleCommand.
    /// </summary>
    public static TogglePlaylistShuffleCommand CreateTogglePlaylistShuffleCommand(
        int zoneId,
        CommandSource source = CommandSource.Internal
    )
    {
        return new TogglePlaylistShuffleCommand { ZoneId = zoneId, Source = source };
    }

    /// <summary>
    /// Creates a SetPlaylistRepeatCommand with the specified repeat state.
    /// </summary>
    public static SetPlaylistRepeatCommand CreateSetPlaylistRepeatCommand(
        int zoneId,
        bool enabled,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetPlaylistRepeatCommand
        {
            ZoneId = zoneId,
            Enabled = enabled,
            Source = source,
        };
    }

    /// <summary>
    /// Creates a TogglePlaylistRepeatCommand.
    /// </summary>
    public static TogglePlaylistRepeatCommand CreateTogglePlaylistRepeatCommand(
        int zoneId,
        CommandSource source = CommandSource.Internal
    )
    {
        return new TogglePlaylistRepeatCommand { ZoneId = zoneId, Source = source };
    }

    #endregion

    #region Client Commands

    /// <summary>
    /// Creates a SetClientVolumeCommand with the specified volume level.
    /// </summary>
    public static SetClientVolumeCommand CreateSetClientVolumeCommand(
        int clientId,
        int volume,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetClientVolumeCommand
        {
            ClientId = clientId,
            Volume = Math.Clamp(volume, 0, 100),
            Source = source,
        };
    }

    /// <summary>
    /// Creates a SetClientMuteCommand with the specified mute state.
    /// </summary>
    public static SetClientMuteCommand CreateSetClientMuteCommand(
        int clientId,
        bool enabled,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetClientMuteCommand
        {
            ClientId = clientId,
            Enabled = enabled,
            Source = source,
        };
    }

    /// <summary>
    /// Creates a ToggleClientMuteCommand.
    /// </summary>
    public static ToggleClientMuteCommand CreateToggleClientMuteCommand(
        int clientId,
        CommandSource source = CommandSource.Internal
    )
    {
        return new ToggleClientMuteCommand { ClientId = clientId, Source = source };
    }

    /// <summary>
    /// Creates a SetClientLatencyCommand with the specified latency.
    /// </summary>
    public static SetClientLatencyCommand CreateSetClientLatencyCommand(
        int clientId,
        int latencyMs,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetClientLatencyCommand
        {
            ClientId = clientId,
            LatencyMs = Math.Clamp(latencyMs, 0, 10000),
            Source = source,
        };
    }

    /// <summary>
    /// Creates an AssignClientToZoneCommand.
    /// </summary>
    public static AssignClientToZoneCommand CreateAssignClientToZoneCommand(
        int clientId,
        int zoneId,
        CommandSource source = CommandSource.Internal
    )
    {
        return new AssignClientToZoneCommand
        {
            ClientId = clientId,
            ZoneId = zoneId,
            Source = source,
        };
    }

    #endregion

    #region Parsing Helpers

    /// <summary>
    /// Parses a string payload to create appropriate zone commands.
    /// Used by protocol adapters (MQTT, KNX) for unified command creation.
    /// </summary>
    public static object? CreateZoneCommandFromPayload(int zoneId, string command, string payload, CommandSource source)
    {
        return command.ToLowerInvariant() switch
        {
            // Playback commands
            "play" => CreatePlayCommand(zoneId, source),
            "pause" => CreatePauseCommand(zoneId, source),
            "stop" => CreateStopCommand(zoneId, source),

            // Volume commands
            "volume" when TryParseInt(payload, out var volume) => CreateSetZoneVolumeCommand(zoneId, volume, source),
            "volume" when payload.Equals("+", StringComparison.OrdinalIgnoreCase) => CreateVolumeUpCommand(
                zoneId,
                5,
                source
            ),
            "volume" when payload.Equals("-", StringComparison.OrdinalIgnoreCase) => CreateVolumeDownCommand(
                zoneId,
                5,
                source
            ),
            "volume" when TryParseVolumeStep(payload, out var step, out var direction) => direction > 0
                ? CreateVolumeUpCommand(zoneId, step, source)
                : CreateVolumeDownCommand(zoneId, step, source),

            // Mute commands
            "mute" when TryParseBool(payload, out var mute) => CreateSetZoneMuteCommand(zoneId, mute, source),
            "mute" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) => CreateToggleZoneMuteCommand(
                zoneId,
                source
            ),

            // Track commands
            "track" when TryParseInt(payload, out var trackIndex) => CreateSetTrackCommand(zoneId, trackIndex, source),
            "track" when payload.Equals("+", StringComparison.OrdinalIgnoreCase) => CreateNextTrackCommand(
                zoneId,
                source
            ),
            "track" when payload.Equals("-", StringComparison.OrdinalIgnoreCase) => CreatePreviousTrackCommand(
                zoneId,
                source
            ),
            "next" => CreateNextTrackCommand(zoneId, source),
            "previous" => CreatePreviousTrackCommand(zoneId, source),

            // Track repeat commands
            "track_repeat" when TryParseBool(payload, out var repeat) => CreateSetTrackRepeatCommand(
                zoneId,
                repeat,
                source
            ),
            "track_repeat" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) =>
                CreateToggleTrackRepeatCommand(zoneId, source),

            // Playlist commands
            "playlist" when TryParseInt(payload, out var playlistIndex) => CreateSetPlaylistCommand(
                zoneId,
                playlistIndex,
                source
            ),
            "playlist" when payload.Equals("+", StringComparison.OrdinalIgnoreCase) => CreateNextPlaylistCommand(
                zoneId,
                source
            ),
            "playlist" when payload.Equals("-", StringComparison.OrdinalIgnoreCase) => CreatePreviousPlaylistCommand(
                zoneId,
                source
            ),
            "playlist" when !string.IsNullOrEmpty(payload) => CreateSetPlaylistCommand(zoneId, payload, source),

            // Playlist shuffle commands
            "shuffle" when TryParseBool(payload, out var shuffle) => CreateSetPlaylistShuffleCommand(
                zoneId,
                shuffle,
                source
            ),
            "shuffle" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) =>
                CreateTogglePlaylistShuffleCommand(zoneId, source),

            // Playlist repeat commands
            "playlist_repeat" when TryParseBool(payload, out var playlistRepeat) => CreateSetPlaylistRepeatCommand(
                zoneId,
                playlistRepeat,
                source
            ),
            "playlist_repeat" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) =>
                CreateTogglePlaylistRepeatCommand(zoneId, source),

            _ => null,
        };
    }

    /// <summary>
    /// Parses a string payload to create appropriate client commands.
    /// Used by protocol adapters (MQTT, KNX) for unified command creation.
    /// </summary>
    public static object? CreateClientCommandFromPayload(
        int clientId,
        string command,
        string payload,
        CommandSource source
    )
    {
        return command.ToLowerInvariant() switch
        {
            // Volume commands
            "volume" when TryParseInt(payload, out var volume) => CreateSetClientVolumeCommand(
                clientId,
                volume,
                source
            ),

            // Mute commands
            "mute" when TryParseBool(payload, out var mute) => CreateSetClientMuteCommand(clientId, mute, source),
            "mute" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) => CreateToggleClientMuteCommand(
                clientId,
                source
            ),

            // Latency commands
            "latency" when TryParseInt(payload, out var latency) => CreateSetClientLatencyCommand(
                clientId,
                latency,
                source
            ),

            // Zone assignment commands
            "zone" when TryParseInt(payload, out var zoneId) => CreateAssignClientToZoneCommand(
                clientId,
                zoneId,
                source
            ),

            _ => null,
        };
    }

    #endregion

    #region Private Parsing Helpers

    private static bool TryParseInt(string value, out int result)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    private static bool TryParseBool(string value, out bool result)
    {
        result = value.ToLowerInvariant() switch
        {
            "true" or "1" or "on" or "yes" => true,
            "false" or "0" or "off" or "no" => false,
            _ => false,
        };

        return value.ToLowerInvariant() is "true" or "1" or "on" or "yes" or "false" or "0" or "off" or "no";
    }

    private static bool TryParseVolumeStep(string payload, out int step, out int direction)
    {
        step = 5; // default
        direction = 0;

        if (payload.StartsWith("+", StringComparison.OrdinalIgnoreCase))
        {
            direction = 1;
            var stepStr = payload[1..];
            return string.IsNullOrEmpty(stepStr) || TryParseInt(stepStr, out step);
        }

        if (payload.StartsWith("-", StringComparison.OrdinalIgnoreCase))
        {
            direction = -1;
            var stepStr = payload[1..];
            return string.IsNullOrEmpty(stepStr) || TryParseInt(stepStr, out step);
        }

        return false;
    }

    #endregion
}

/// <summary>
/// Extension methods for CommandFactory to provide fluent API.
/// </summary>
public static class CommandFactoryExtensions
{
    /// <summary>
    /// Creates a command from MQTT topic and payload.
    /// </summary>
    public static object? CreateFromMqttTopic(string topic, string payload)
    {
        var parts = topic.Split('/');
        if (parts.Length < 4 || !parts[0].Equals("snapdog", StringComparison.OrdinalIgnoreCase))
            return null;

        var entityType = parts[1].ToLowerInvariant();
        var entityIdStr = parts[2];
        var command = parts[3].ToLowerInvariant();

        if (!int.TryParse(entityIdStr, out var entityId))
            return null;

        return entityType switch
        {
            "zone" => CommandFactory.CreateZoneCommandFromPayload(entityId, command, payload, CommandSource.Mqtt),
            "client" => CommandFactory.CreateClientCommandFromPayload(entityId, command, payload, CommandSource.Mqtt),
            _ => null,
        };
    }

    /// <summary>
    /// Creates a command from KNX group address and value.
    /// </summary>
    public static object? CreateFromKnxGroupAddress(string groupAddress, object value, int entityId, string commandType)
    {
        var source = CommandSource.Knx;

        return commandType.ToLowerInvariant() switch
        {
            // Zone commands
            "zone_volume" when value is byte byteValue => CommandFactory.CreateSetZoneVolumeCommand(
                entityId,
                ConvertDpt5ToPercentage(byteValue),
                source
            ),
            "zone_mute" when value is bool boolValue => CommandFactory.CreateSetZoneMuteCommand(
                entityId,
                boolValue,
                source
            ),
            "zone_play" when value is bool playValue && playValue => CommandFactory.CreatePlayCommand(entityId, source),
            "zone_pause" when value is bool pauseValue && pauseValue => CommandFactory.CreatePauseCommand(
                entityId,
                source
            ),
            "zone_stop" when value is bool stopValue && stopValue => CommandFactory.CreateStopCommand(entityId, source),
            "zone_track" when value is byte trackValue => CommandFactory.CreateSetTrackCommand(
                entityId,
                trackValue,
                source
            ),

            // Client commands
            "client_volume" when value is byte clientVolumeValue => CommandFactory.CreateSetClientVolumeCommand(
                entityId,
                ConvertDpt5ToPercentage(clientVolumeValue),
                source
            ),
            "client_mute" when value is bool clientMuteValue => CommandFactory.CreateSetClientMuteCommand(
                entityId,
                clientMuteValue,
                source
            ),
            "client_zone" when value is byte zoneValue => CommandFactory.CreateAssignClientToZoneCommand(
                entityId,
                zoneValue,
                source
            ),

            _ => null,
        };
    }

    private static int ConvertDpt5ToPercentage(byte dptValue)
    {
        // DPT 5.001: 0-255 -> 0-100%
        return (int)Math.Round(dptValue / 255.0 * 100.0);
    }
}
