namespace SnapDog2.Infrastructure.Integrations.Mqtt;

using SnapDog2.Core.Enums;
using SnapDog2.Server.Features.Shared.Factories;

/// <summary>
/// Delegate for mapping MQTT commands to Cortex.Mediator commands.
/// </summary>
/// <param name="entityId">The entity ID (zone or client index).</param>
/// <param name="parameter">The command parameter (if any).</param>
/// <returns>The mapped command or null if mapping fails.</returns>
public delegate object? MqttCommandMappingDelegate(int entityId, string parameter);

/// <summary>
/// Strategy for mapping MQTT commands to Cortex.Mediator commands using type-safe dictionaries.
/// </summary>
public static class MqttCommandMappingStrategy
{
    /// <summary>
    /// Zone command mappings using constants instead of magic strings.
    /// </summary>
    private static readonly Dictionary<string, MqttCommandMappingDelegate> ZoneCommandMap = new()
    {
        // Playback commands
        [MqttConstants.Commands.PLAY] = MapZonePlayCommand,
        [MqttConstants.Commands.PAUSE] = (zoneIndex, _) =>
            CommandFactory.CreatePauseCommand(zoneIndex, CommandSource.Mqtt),
        [MqttConstants.Commands.STOP] = (zoneIndex, _) =>
            CommandFactory.CreateStopCommand(zoneIndex, CommandSource.Mqtt),

        // Navigation commands
        [MqttConstants.Commands.NEXT] = (zoneIndex, _) =>
            CommandFactory.CreateNextTrackCommand(zoneIndex, CommandSource.Mqtt),
        [MqttConstants.Commands.TRACK_NEXT] = (zoneIndex, _) =>
            CommandFactory.CreateNextTrackCommand(zoneIndex, CommandSource.Mqtt),
        [MqttConstants.Commands.PREVIOUS] = (zoneIndex, _) =>
            CommandFactory.CreatePreviousTrackCommand(zoneIndex, CommandSource.Mqtt),
        [MqttConstants.Commands.TRACK_PREVIOUS] = (zoneIndex, _) =>
            CommandFactory.CreatePreviousTrackCommand(zoneIndex, CommandSource.Mqtt),
        [MqttConstants.Commands.PLAYLIST_NEXT] = (zoneIndex, _) =>
            CommandFactory.CreateNextPlaylistCommand(zoneIndex, CommandSource.Mqtt),
        [MqttConstants.Commands.PLAYLIST_PREVIOUS] = (zoneIndex, _) =>
            CommandFactory.CreatePreviousPlaylistCommand(zoneIndex, CommandSource.Mqtt),

        // Volume commands
        [MqttConstants.Commands.VOLUME] = MapZoneVolumeCommand,
        [MqttConstants.Commands.VOLUME_UP] = (zoneIndex, _) =>
            CommandFactory.CreateVolumeUpCommand(zoneIndex, 5, CommandSource.Mqtt),
        [MqttConstants.Commands.VOLUME_DOWN] = (zoneIndex, _) =>
            CommandFactory.CreateVolumeDownCommand(zoneIndex, 5, CommandSource.Mqtt),

        // Mute commands
        [MqttConstants.Commands.MUTE_ON] = (zoneIndex, _) =>
            CommandFactory.CreateSetZoneMuteCommand(zoneIndex, true, CommandSource.Mqtt),
        [MqttConstants.Commands.MUTE_OFF] = (zoneIndex, _) =>
            CommandFactory.CreateSetZoneMuteCommand(zoneIndex, false, CommandSource.Mqtt),
        [MqttConstants.Commands.MUTE_TOGGLE] = (zoneIndex, _) =>
            CommandFactory.CreateToggleZoneMuteCommand(zoneIndex, CommandSource.Mqtt),

        // Track repeat commands
        [MqttConstants.Commands.TRACK_REPEAT_ON] = (zoneIndex, _) =>
            CommandFactory.CreateSetTrackRepeatCommand(zoneIndex, true, CommandSource.Mqtt),
        [MqttConstants.Commands.TRACK_REPEAT_OFF] = (zoneIndex, _) =>
            CommandFactory.CreateSetTrackRepeatCommand(zoneIndex, false, CommandSource.Mqtt),
        [MqttConstants.Commands.TRACK_REPEAT_TOGGLE] = (zoneIndex, _) =>
            CommandFactory.CreateToggleTrackRepeatCommand(zoneIndex, CommandSource.Mqtt),

        // Playlist shuffle commands
        [MqttConstants.Commands.SHUFFLE_ON] = (zoneIndex, _) =>
            CommandFactory.CreateSetPlaylistShuffleCommand(zoneIndex, true, CommandSource.Mqtt),
        [MqttConstants.Commands.SHUFFLE_OFF] = (zoneIndex, _) =>
            CommandFactory.CreateSetPlaylistShuffleCommand(zoneIndex, false, CommandSource.Mqtt),
        [MqttConstants.Commands.SHUFFLE_TOGGLE] = (zoneIndex, _) =>
            CommandFactory.CreateTogglePlaylistShuffleCommand(zoneIndex, CommandSource.Mqtt),

        // Playlist repeat commands
        [MqttConstants.Commands.REPEAT_ON] = (zoneIndex, _) =>
            CommandFactory.CreateSetPlaylistRepeatCommand(zoneIndex, true, CommandSource.Mqtt),
        [MqttConstants.Commands.REPEAT_OFF] = (zoneIndex, _) =>
            CommandFactory.CreateSetPlaylistRepeatCommand(zoneIndex, false, CommandSource.Mqtt),
        [MqttConstants.Commands.REPEAT_TOGGLE] = (zoneIndex, _) =>
            CommandFactory.CreateTogglePlaylistRepeatCommand(zoneIndex, CommandSource.Mqtt),

        // Selection commands
        [MqttConstants.Commands.TRACK] = MapZoneTrackCommand,
        [MqttConstants.Commands.PLAYLIST] = MapZonePlaylistCommand,
    };

    /// <summary>
    /// Client command mappings using constants instead of magic strings.
    /// </summary>
    private static readonly Dictionary<string, MqttCommandMappingDelegate> ClientCommandMap = new()
    {
        // Volume commands
        [MqttConstants.Commands.VOLUME] = MapClientVolumeCommand,

        // Mute commands
        [MqttConstants.Commands.MUTE_ON] = (clientIndex, _) =>
            CommandFactory.CreateSetClientMuteCommand(clientIndex, true, CommandSource.Mqtt),
        [MqttConstants.Commands.MUTE_OFF] = (clientIndex, _) =>
            CommandFactory.CreateSetClientMuteCommand(clientIndex, false, CommandSource.Mqtt),
        [MqttConstants.Commands.MUTE_TOGGLE] = (clientIndex, _) =>
            CommandFactory.CreateToggleClientMuteCommand(clientIndex, CommandSource.Mqtt),

        // Configuration commands
        [MqttConstants.Commands.ZONE_ASSIGNMENT] = MapClientZoneCommand,
        [MqttConstants.Commands.LATENCY] = MapClientLatencyCommand,
    };

    /// <summary>
    /// Maps a zone command using the command mapping dictionary.
    /// </summary>
    public static object? MapZoneCommand(string command, int zoneIndex, string parameter)
    {
        return ZoneCommandMap.TryGetValue(command, out var mapper) ? mapper(zoneIndex, parameter) : null;
    }

    /// <summary>
    /// Maps a client command using the command mapping dictionary.
    /// </summary>
    public static object? MapClientCommand(string command, int clientIndex, string parameter)
    {
        return ClientCommandMap.TryGetValue(command, out var mapper) ? mapper(clientIndex, parameter) : null;
    }

    // Complex command mappers with parameter parsing
    private static object? MapZonePlayCommand(int zoneIndex, string parameter)
    {
        if (string.IsNullOrEmpty(parameter))
            return CommandFactory.CreatePlayCommand(zoneIndex, CommandSource.Mqtt);

        if (parameter.StartsWith(MqttConstants.Parameters.TRACK_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            var trackIndexStr = parameter[MqttConstants.Parameters.TRACK_PREFIX.Length..].Trim();
            return int.TryParse(trackIndexStr, out var trackIndex)
                ? CommandFactory.CreatePlayTrackByIndexCommand(zoneIndex, trackIndex, CommandSource.Mqtt)
                : null;
        }

        if (parameter.StartsWith(MqttConstants.Parameters.URL_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            var url = parameter[MqttConstants.Parameters.URL_PREFIX.Length..].Trim();
            return CommandFactory.CreatePlayUrlCommand(zoneIndex, url, CommandSource.Mqtt);
        }

        return null;
    }

    private static object? MapZoneVolumeCommand(int zoneIndex, string parameter)
    {
        return int.TryParse(parameter, out var volume)
            ? CommandFactory.CreateSetZoneVolumeCommand(zoneIndex, volume, CommandSource.Mqtt)
            : null;
    }

    private static object? MapZoneTrackCommand(int zoneIndex, string parameter)
    {
        return int.TryParse(parameter, out var trackIndex)
            ? CommandFactory.CreateSetTrackCommand(zoneIndex, trackIndex, CommandSource.Mqtt)
            : null;
    }

    private static object? MapZonePlaylistCommand(int zoneIndex, string parameter)
    {
        return int.TryParse(parameter, out var playlistIndex)
            ? CommandFactory.CreateSetPlaylistCommand(zoneIndex, playlistIndex, CommandSource.Mqtt)
            : null;
    }

    private static object? MapClientVolumeCommand(int clientIndex, string parameter)
    {
        return int.TryParse(parameter, out var volume)
            ? CommandFactory.CreateSetClientVolumeCommand(clientIndex, volume, CommandSource.Mqtt)
            : null;
    }

    private static object? MapClientZoneCommand(int clientIndex, string parameter)
    {
        return int.TryParse(parameter, out var zoneIndex)
            ? CommandFactory.CreateAssignClientToZoneCommand(clientIndex, zoneIndex, CommandSource.Mqtt)
            : null;
    }

    private static object? MapClientLatencyCommand(int clientIndex, string parameter)
    {
        return int.TryParse(parameter, out var latency)
            ? CommandFactory.CreateSetClientLatencyCommand(clientIndex, latency, CommandSource.Mqtt)
            : null;
    }
}
