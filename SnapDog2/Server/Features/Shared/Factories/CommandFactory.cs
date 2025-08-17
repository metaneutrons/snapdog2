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
    public static PlayCommand CreatePlayCommand(int zoneIndex, CommandSource source = CommandSource.Internal)
    {
        return new PlayCommand { ZoneIndex = zoneIndex, Source = source };
    }

    /// <summary>
    /// Creates a PlayCommand with a specific track index.
    /// </summary>
    public static PlayCommand CreatePlayTrackCommand(
        int zoneIndex,
        int trackIndex,
        CommandSource source = CommandSource.Internal
    )
    {
        return new PlayCommand
        {
            ZoneIndex = zoneIndex,
            TrackIndex = trackIndex,
            Source = source,
        };
    }

    /// <summary>
    /// Creates a PlayCommand with a media URL.
    /// </summary>
    public static PlayCommand CreatePlayUrlCommand(
        int zoneIndex,
        string mediaUrl,
        CommandSource source = CommandSource.Internal
    )
    {
        return new PlayCommand
        {
            ZoneIndex = zoneIndex,
            MediaUrl = mediaUrl,
            Source = source,
        };
    }

    /// <summary>
    /// Creates a PauseCommand for the specified zone.
    /// </summary>
    public static PauseCommand CreatePauseCommand(int zoneIndex, CommandSource source = CommandSource.Internal)
    {
        return new PauseCommand { ZoneIndex = zoneIndex, Source = source };
    }

    /// <summary>
    /// Creates a StopCommand for the specified zone.
    /// </summary>
    public static StopCommand CreateStopCommand(int zoneIndex, CommandSource source = CommandSource.Internal)
    {
        return new StopCommand { ZoneIndex = zoneIndex, Source = source };
    }

    #endregion

    #region Zone Volume Commands

    /// <summary>
    /// Creates a SetZoneVolumeCommand with the specified volume level.
    /// </summary>
    public static SetZoneVolumeCommand CreateSetZoneVolumeCommand(
        int zoneIndex,
        int volume,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetZoneVolumeCommand
        {
            ZoneIndex = zoneIndex,
            Volume = Math.Clamp(volume, 0, 100),
            Source = source,
        };
    }

    /// <summary>
    /// Creates a VolumeUpCommand with the specified step.
    /// </summary>
    public static VolumeUpCommand CreateVolumeUpCommand(
        int zoneIndex,
        int step = 5,
        CommandSource source = CommandSource.Internal
    )
    {
        return new VolumeUpCommand
        {
            ZoneIndex = zoneIndex,
            Step = Math.Clamp(step, 1, 50),
            Source = source,
        };
    }

    /// <summary>
    /// Creates a VolumeDownCommand with the specified step.
    /// </summary>
    public static VolumeDownCommand CreateVolumeDownCommand(
        int zoneIndex,
        int step = 5,
        CommandSource source = CommandSource.Internal
    )
    {
        return new VolumeDownCommand
        {
            ZoneIndex = zoneIndex,
            Step = Math.Clamp(step, 1, 50),
            Source = source,
        };
    }

    /// <summary>
    /// Creates a SetZoneMuteCommand with the specified mute state.
    /// </summary>
    public static SetZoneMuteCommand CreateSetZoneMuteCommand(
        int zoneIndex,
        bool enabled,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetZoneMuteCommand
        {
            ZoneIndex = zoneIndex,
            Enabled = enabled,
            Source = source,
        };
    }

    /// <summary>
    /// Creates a ToggleZoneMuteCommand.
    /// </summary>
    public static ToggleZoneMuteCommand CreateToggleZoneMuteCommand(
        int zoneIndex,
        CommandSource source = CommandSource.Internal
    )
    {
        return new ToggleZoneMuteCommand { ZoneIndex = zoneIndex, Source = source };
    }

    #endregion

    #region Zone Track Commands

    /// <summary>
    /// Creates a SetTrackCommand with the specified track index.
    /// </summary>
    public static SetTrackCommand CreateSetTrackCommand(
        int zoneIndex,
        int trackIndex,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetTrackCommand
        {
            ZoneIndex = zoneIndex,
            TrackIndex = Math.Max(1, trackIndex),
            Source = source,
        };
    }

    /// <summary>
    /// Creates a NextTrackCommand.
    /// </summary>
    public static NextTrackCommand CreateNextTrackCommand(int zoneIndex, CommandSource source = CommandSource.Internal)
    {
        return new NextTrackCommand { ZoneIndex = zoneIndex, Source = source };
    }

    /// <summary>
    /// Creates a PreviousTrackCommand.
    /// </summary>
    public static PreviousTrackCommand CreatePreviousTrackCommand(
        int zoneIndex,
        CommandSource source = CommandSource.Internal
    )
    {
        return new PreviousTrackCommand { ZoneIndex = zoneIndex, Source = source };
    }

    /// <summary>
    /// Creates a SetTrackRepeatCommand with the specified repeat state.
    /// </summary>
    public static SetTrackRepeatCommand CreateSetTrackRepeatCommand(
        int zoneIndex,
        bool enabled,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetTrackRepeatCommand
        {
            ZoneIndex = zoneIndex,
            Enabled = enabled,
            Source = source,
        };
    }

    /// <summary>
    /// Creates a ToggleTrackRepeatCommand.
    /// </summary>
    public static ToggleTrackRepeatCommand CreateToggleTrackRepeatCommand(
        int zoneIndex,
        CommandSource source = CommandSource.Internal
    )
    {
        return new ToggleTrackRepeatCommand { ZoneIndex = zoneIndex, Source = source };
    }

    #endregion

    #region Zone Playlist Commands

    /// <summary>
    /// Creates a SetPlaylistCommand with the specified playlist index.
    /// </summary>
    public static SetPlaylistCommand CreateSetPlaylistCommand(
        int zoneIndex,
        int playlistIndex,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetPlaylistCommand
        {
            ZoneIndex = zoneIndex,
            PlaylistIndex = Math.Max(1, playlistIndex),
            Source = source,
        };
    }

    /// <summary>
    /// Creates a NextPlaylistCommand.
    /// </summary>
    public static NextPlaylistCommand CreateNextPlaylistCommand(
        int zoneIndex,
        CommandSource source = CommandSource.Internal
    )
    {
        return new NextPlaylistCommand { ZoneIndex = zoneIndex, Source = source };
    }

    /// <summary>
    /// Creates a PreviousPlaylistCommand.
    /// </summary>
    public static PreviousPlaylistCommand CreatePreviousPlaylistCommand(
        int zoneIndex,
        CommandSource source = CommandSource.Internal
    )
    {
        return new PreviousPlaylistCommand { ZoneIndex = zoneIndex, Source = source };
    }

    /// <summary>
    /// Creates a SetPlaylistShuffleCommand with the specified shuffle state.
    /// </summary>
    public static SetPlaylistShuffleCommand CreateSetPlaylistShuffleCommand(
        int zoneIndex,
        bool enabled,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetPlaylistShuffleCommand
        {
            ZoneIndex = zoneIndex,
            Enabled = enabled,
            Source = source,
        };
    }

    /// <summary>
    /// Creates a TogglePlaylistShuffleCommand.
    /// </summary>
    public static TogglePlaylistShuffleCommand CreateTogglePlaylistShuffleCommand(
        int zoneIndex,
        CommandSource source = CommandSource.Internal
    )
    {
        return new TogglePlaylistShuffleCommand { ZoneIndex = zoneIndex, Source = source };
    }

    /// <summary>
    /// Creates a SetPlaylistRepeatCommand with the specified repeat state.
    /// </summary>
    public static SetPlaylistRepeatCommand CreateSetPlaylistRepeatCommand(
        int zoneIndex,
        bool enabled,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetPlaylistRepeatCommand
        {
            ZoneIndex = zoneIndex,
            Enabled = enabled,
            Source = source,
        };
    }

    /// <summary>
    /// Creates a TogglePlaylistRepeatCommand.
    /// </summary>
    public static TogglePlaylistRepeatCommand CreateTogglePlaylistRepeatCommand(
        int zoneIndex,
        CommandSource source = CommandSource.Internal
    )
    {
        return new TogglePlaylistRepeatCommand { ZoneIndex = zoneIndex, Source = source };
    }

    #endregion

    #region Client Commands

    /// <summary>
    /// Creates a SetClientVolumeCommand with the specified volume level.
    /// </summary>
    public static SetClientVolumeCommand CreateSetClientVolumeCommand(
        int clientIndex,
        int volume,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetClientVolumeCommand
        {
            ClientIndex = clientIndex,
            Volume = Math.Clamp(volume, 0, 100),
            Source = source,
        };
    }

    /// <summary>
    /// Creates a SetClientMuteCommand with the specified mute state.
    /// </summary>
    public static SetClientMuteCommand CreateSetClientMuteCommand(
        int clientIndex,
        bool enabled,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetClientMuteCommand
        {
            ClientIndex = clientIndex,
            Enabled = enabled,
            Source = source,
        };
    }

    /// <summary>
    /// Creates a ToggleClientMuteCommand.
    /// </summary>
    public static ToggleClientMuteCommand CreateToggleClientMuteCommand(
        int clientIndex,
        CommandSource source = CommandSource.Internal
    )
    {
        return new ToggleClientMuteCommand { ClientIndex = clientIndex, Source = source };
    }

    /// <summary>
    /// Creates a SetClientLatencyCommand with the specified latency.
    /// </summary>
    public static SetClientLatencyCommand CreateSetClientLatencyCommand(
        int clientIndex,
        int latencyMs,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SetClientLatencyCommand
        {
            ClientIndex = clientIndex,
            LatencyMs = Math.Clamp(latencyMs, 0, 10000),
            Source = source,
        };
    }

    /// <summary>
    /// Creates an AssignClientToZoneCommand.
    /// </summary>
    public static AssignClientToZoneCommand CreateAssignClientToZoneCommand(
        int clientIndex,
        int zoneIndex,
        CommandSource source = CommandSource.Internal
    )
    {
        return new AssignClientToZoneCommand
        {
            ClientIndex = clientIndex,
            ZoneIndex = zoneIndex,
            Source = source,
        };
    }

    /// <summary>
    /// Creates a SeekPositionCommand.
    /// </summary>
    public static SeekPositionCommand CreateSeekPositionCommand(
        int zoneIndex,
        long positionMs,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SeekPositionCommand
        {
            ZoneIndex = zoneIndex,
            PositionMs = positionMs,
            Source = source,
        };
    }

    /// <summary>
    /// Creates a SeekProgressCommand.
    /// </summary>
    public static SeekProgressCommand CreateSeekProgressCommand(
        int zoneIndex,
        float progress,
        CommandSource source = CommandSource.Internal
    )
    {
        return new SeekProgressCommand
        {
            ZoneIndex = zoneIndex,
            Progress = progress,
            Source = source,
        };
    }

    /// <summary>
    /// Creates a PlayTrackByIndexCommand.
    /// </summary>
    public static PlayTrackByIndexCommand CreatePlayTrackByIndexCommand(
        int zoneIndex,
        int trackIndex,
        CommandSource source = CommandSource.Internal
    )
    {
        return new PlayTrackByIndexCommand
        {
            ZoneIndex = zoneIndex,
            TrackIndex = trackIndex,
            Source = source,
        };
    }

    #endregion

    #region Client Volume Commands

    /// <summary>
    /// Creates a ClientVolumeUpCommand.
    /// </summary>
    public static ClientVolumeUpCommand CreateClientVolumeUpCommand(
        int clientIndex,
        int step = 5,
        CommandSource source = CommandSource.Internal
    )
    {
        return new ClientVolumeUpCommand
        {
            ClientIndex = clientIndex,
            Step = step,
            Source = source,
        };
    }

    /// <summary>
    /// Creates a ClientVolumeDownCommand.
    /// </summary>
    public static ClientVolumeDownCommand CreateClientVolumeDownCommand(
        int clientIndex,
        int step = 5,
        CommandSource source = CommandSource.Internal
    )
    {
        return new ClientVolumeDownCommand
        {
            ClientIndex = clientIndex,
            Step = step,
            Source = source,
        };
    }

    #endregion

    #region Parsing Helpers

    /// <summary>
    /// Parses a string payload to create appropriate zone commands.
    /// Used by protocol adapters (MQTT, KNX) for unified command creation.
    /// </summary>
    public static object? CreateZoneCommandFromPayload(
        int zoneIndex,
        string command,
        string payload,
        CommandSource source
    )
    {
        return command.ToLowerInvariant() switch
        {
            // Playback commands
            "play" => CreatePlayCommand(zoneIndex, source),
            "pause" => CreatePauseCommand(zoneIndex, source),
            "stop" => CreateStopCommand(zoneIndex, source),

            // Volume commands
            "volume" when TryParseInt(payload, out var volume) => CreateSetZoneVolumeCommand(zoneIndex, volume, source),
            "volume" when payload.Equals("+", StringComparison.OrdinalIgnoreCase) => CreateVolumeUpCommand(
                zoneIndex,
                5,
                source
            ),
            "volume" when payload.Equals("-", StringComparison.OrdinalIgnoreCase) => CreateVolumeDownCommand(
                zoneIndex,
                5,
                source
            ),
            "volume" when TryParseVolumeStep(payload, out var step, out var direction) => direction > 0
                ? CreateVolumeUpCommand(zoneIndex, step, source)
                : CreateVolumeDownCommand(zoneIndex, step, source),

            // Volume navigation commands (dedicated topics)
            "volume/set" when TryParseInt(payload, out var volumeSet) => CreateSetZoneVolumeCommand(
                zoneIndex,
                volumeSet,
                source
            ),
            "volume/up" => CreateVolumeUpCommand(zoneIndex, 5, source),
            "volume/down" => CreateVolumeDownCommand(zoneIndex, 5, source),

            // Mute commands
            "mute" when TryParseBool(payload, out var mute) => CreateSetZoneMuteCommand(zoneIndex, mute, source),
            "mute" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) => CreateToggleZoneMuteCommand(
                zoneIndex,
                source
            ),

            // Mute navigation commands (dedicated topics)
            "mute/set" when TryParseBool(payload, out var muteSet) => CreateSetZoneMuteCommand(
                zoneIndex,
                muteSet,
                source
            ),
            "mute/set" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) => CreateToggleZoneMuteCommand(
                zoneIndex,
                source
            ),
            "mute/toggle" => CreateToggleZoneMuteCommand(zoneIndex, source),

            // Navigation commands (simplified)
            "next" => CreateNextTrackCommand(zoneIndex, source),
            "previous" => CreatePreviousTrackCommand(zoneIndex, source),

            // Track commands
            "track" when TryParseInt(payload, out var trackIndex) => CreateSetTrackCommand(
                zoneIndex,
                trackIndex,
                source
            ),
            "track" when payload.Equals("+", StringComparison.OrdinalIgnoreCase) => CreateNextTrackCommand(
                zoneIndex,
                source
            ),
            "track" when payload.Equals("-", StringComparison.OrdinalIgnoreCase) => CreatePreviousTrackCommand(
                zoneIndex,
                source
            ),

            // Track commands (hierarchical)
            "track/set" when TryParseInt(payload, out var trackSetIndex) => CreateSetTrackCommand(
                zoneIndex,
                trackSetIndex,
                source
            ),

            // Playlist commands
            "playlist" when TryParseInt(payload, out var playlistIndex) => CreateSetPlaylistCommand(
                zoneIndex,
                playlistIndex,
                source
            ),
            "playlist" when payload.Equals("+", StringComparison.OrdinalIgnoreCase) => CreateNextPlaylistCommand(
                zoneIndex,
                source
            ),
            "playlist" when payload.Equals("-", StringComparison.OrdinalIgnoreCase) => CreatePreviousPlaylistCommand(
                zoneIndex,
                source
            ),
            "playlist" when TryParseInt(payload, out var playlistIndexFromString) => CreateSetPlaylistCommand(
                zoneIndex,
                playlistIndexFromString,
                source
            ),

            // Playlist commands (hierarchical)
            "playlist/set" when TryParseInt(payload, out var playlistSetIndex) => CreateSetPlaylistCommand(
                zoneIndex,
                playlistSetIndex,
                source
            ),
            "playlist/next" => CreateNextPlaylistCommand(zoneIndex, source),
            "playlist/previous" => CreatePreviousPlaylistCommand(zoneIndex, source),

            // Playlist shuffle commands (simplified topic)
            "shuffle" when TryParseBool(payload, out var shuffle) => CreateSetPlaylistShuffleCommand(
                zoneIndex,
                shuffle,
                source
            ),
            "shuffle" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) =>
                CreateTogglePlaylistShuffleCommand(zoneIndex, source),

            // Repeat commands (improved hierarchical structure)
            "repeat/track" when TryParseBool(payload, out var trackRepeat) => CreateSetTrackRepeatCommand(
                zoneIndex,
                trackRepeat,
                source
            ),
            "repeat/track" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) =>
                CreateToggleTrackRepeatCommand(zoneIndex, source),
            "repeat/track/set" when TryParseBool(payload, out var trackRepeatSet) => CreateSetTrackRepeatCommand(
                zoneIndex,
                trackRepeatSet,
                source
            ),
            "repeat/track/set" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) =>
                CreateToggleTrackRepeatCommand(zoneIndex, source),

            // Playlist repeat commands (improved hierarchical structure)
            "repeat/playlist" when TryParseBool(payload, out var playlistRepeatNew) => CreateSetPlaylistRepeatCommand(
                zoneIndex,
                playlistRepeatNew,
                source
            ),
            "repeat/playlist" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) =>
                CreateTogglePlaylistRepeatCommand(zoneIndex, source),
            "repeat/playlist/set" when TryParseBool(payload, out var playlistRepeatSetNew) =>
                CreateSetPlaylistRepeatCommand(zoneIndex, playlistRepeatSetNew, source),
            "repeat/playlist/set" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) =>
                CreateTogglePlaylistRepeatCommand(zoneIndex, source),

            // Legacy repeat commands (for backward compatibility)
            "repeat" when TryParseBool(payload, out var playlistRepeat) => CreateSetPlaylistRepeatCommand(
                zoneIndex,
                playlistRepeat,
                source
            ),
            "repeat" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) =>
                CreateTogglePlaylistRepeatCommand(zoneIndex, source),
            "repeat/set" when TryParseBool(payload, out var playlistRepeatSet) => CreateSetPlaylistRepeatCommand(
                zoneIndex,
                playlistRepeatSet,
                source
            ),
            "repeat/set" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) =>
                CreateTogglePlaylistRepeatCommand(zoneIndex, source),

            // Shuffle commands (simplified topic)
            "shuffle/set" when TryParseBool(payload, out var shuffleSet) => CreateSetPlaylistShuffleCommand(
                zoneIndex,
                shuffleSet,
                source
            ),
            "shuffle/set" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) =>
                CreateTogglePlaylistShuffleCommand(zoneIndex, source),

            // Track position and progress commands (seek functionality)
            "track/position" when TryParseLong(payload, out var positionMs) => CreateSeekPositionCommand(
                zoneIndex,
                positionMs,
                source
            ),
            "track/position/set" when TryParseLong(payload, out var positionSetMs) => CreateSeekPositionCommand(
                zoneIndex,
                positionSetMs,
                source
            ),
            "track/progress" when TryParseFloat(payload, out var progress) => CreateSeekProgressCommand(
                zoneIndex,
                progress,
                source
            ),
            "track/progress/set" when TryParseFloat(payload, out var progressSet) => CreateSeekProgressCommand(
                zoneIndex,
                progressSet,
                source
            ),

            // Play specific track by index
            "play/track" when TryParseInt(payload, out var playTrackIndex) => CreatePlayTrackByIndexCommand(
                zoneIndex,
                playTrackIndex,
                source
            ),
            "track/play" when TryParseInt(payload, out var trackPlayIndex) => CreatePlayTrackByIndexCommand(
                zoneIndex,
                trackPlayIndex,
                source
            ),

            // Play URL directly
            "play/url" when !string.IsNullOrWhiteSpace(payload) => CreatePlayUrlCommand(zoneIndex, payload, source),
            "url/play" when !string.IsNullOrWhiteSpace(payload) => CreatePlayUrlCommand(zoneIndex, payload, source),

            _ => null,
        };
    }

    /// <summary>
    /// Parses a string payload to create appropriate client commands.
    /// Used by protocol adapters (MQTT, KNX) for unified command creation.
    /// </summary>
    public static object? CreateClientCommandFromPayload(
        int clientIndex,
        string command,
        string payload,
        CommandSource source
    )
    {
        return command.ToLowerInvariant() switch
        {
            // Volume commands
            "volume" when TryParseInt(payload, out var volume) => CreateSetClientVolumeCommand(
                clientIndex,
                volume,
                source
            ),
            "volume" when payload.Equals("+", StringComparison.OrdinalIgnoreCase) => CreateClientVolumeUpCommand(
                clientIndex,
                5,
                source
            ),
            "volume" when payload.Equals("-", StringComparison.OrdinalIgnoreCase) => CreateClientVolumeDownCommand(
                clientIndex,
                5,
                source
            ),
            "volume/set" when TryParseInt(payload, out var volumeSet) => CreateSetClientVolumeCommand(
                clientIndex,
                volumeSet,
                source
            ),
            "volume/up" => CreateClientVolumeUpCommand(clientIndex, 5, source),
            "volume/down" => CreateClientVolumeDownCommand(clientIndex, 5, source),

            // Mute commands
            "mute" when TryParseBool(payload, out var mute) => CreateSetClientMuteCommand(clientIndex, mute, source),
            "mute" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) => CreateToggleClientMuteCommand(
                clientIndex,
                source
            ),
            "mute/set" when TryParseBool(payload, out var muteSet) => CreateSetClientMuteCommand(
                clientIndex,
                muteSet,
                source
            ),
            "mute/set" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) =>
                CreateToggleClientMuteCommand(clientIndex, source),
            "mute/toggle" => CreateToggleClientMuteCommand(clientIndex, source),

            // Latency commands
            "latency" when TryParseInt(payload, out var latency) => CreateSetClientLatencyCommand(
                clientIndex,
                latency,
                source
            ),

            // Zone assignment commands
            "zone" when TryParseInt(payload, out var zoneIndex) => CreateAssignClientToZoneCommand(
                clientIndex,
                zoneIndex,
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

    private static bool TryParseLong(string value, out long result)
    {
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    private static bool TryParseFloat(string value, out float result)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
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

        if (!int.TryParse(entityIdStr, out var entityId))
            return null;

        // Handle hierarchical commands (e.g., repeat/track, repeat/set)
        var commandParts = parts.Skip(3).ToArray();
        var command = string.Join("/", commandParts).ToLowerInvariant();

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
