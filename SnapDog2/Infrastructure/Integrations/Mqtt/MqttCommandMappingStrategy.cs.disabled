//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
namespace SnapDog2.Infrastructure.Integrations.Mqtt;

using Cortex.Mediator.Commands;
using SnapDog2.Server.Shared.Factories;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// Delegate for mapping MQTT commands to Cortex.Mediator commands.
/// </summary>
/// <param name="entityId">The entity ID (zone or client index).</param>
/// <param name="parameter">The command parameter (if any).</param>
/// <returns>The mapped command or null if mapping fails.</returns>
public delegate ICommand<Result>? MqttCommandMappingDelegate(int entityId, string parameter);

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
    public static ICommand<Result>? MapZoneCommand(string command, int zoneIndex, string parameter)
    {
        return ZoneCommandMap.TryGetValue(command, out var mapper) ? mapper(zoneIndex, parameter) : null;
    }

    /// <summary>
    /// Maps a client command using the command mapping dictionary.
    /// </summary>
    public static ICommand<Result>? MapClientCommand(string command, int clientIndex, string parameter)
    {
        return ClientCommandMap.TryGetValue(command, out var mapper) ? mapper(clientIndex, parameter) : null;
    }

    // Complex command mappers with parameter parsing
    private static ICommand<Result>? MapZonePlayCommand(int zoneIndex, string parameter)
    {
        if (string.IsNullOrEmpty(parameter))
        {
            return CommandFactory.CreatePlayCommand(zoneIndex, CommandSource.Mqtt);
        }

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

    private static ICommand<Result>? MapZoneVolumeCommand(int zoneIndex, string parameter)
    {
        return int.TryParse(parameter, out var volume)
            ? CommandFactory.CreateSetZoneVolumeCommand(zoneIndex, volume, CommandSource.Mqtt)
            : null;
    }

    private static ICommand<Result>? MapZoneTrackCommand(int zoneIndex, string parameter)
    {
        return int.TryParse(parameter, out var trackIndex)
            ? CommandFactory.CreateSetTrackCommand(zoneIndex, trackIndex, CommandSource.Mqtt)
            : null;
    }

    private static ICommand<Result>? MapZonePlaylistCommand(int zoneIndex, string parameter)
    {
        return int.TryParse(parameter, out var playlistIndex)
            ? CommandFactory.CreateSetPlaylistCommand(zoneIndex, playlistIndex, CommandSource.Mqtt)
            : null;
    }

    private static ICommand<Result>? MapClientVolumeCommand(int clientIndex, string parameter)
    {
        return int.TryParse(parameter, out var volume)
            ? CommandFactory.CreateSetClientVolumeCommand(clientIndex, volume, CommandSource.Mqtt)
            : null;
    }

    private static ICommand<Result>? MapClientZoneCommand(int clientIndex, string parameter)
    {
        return int.TryParse(parameter, out var zoneIndex)
            ? CommandFactory.CreateAssignClientToZoneCommand(clientIndex, zoneIndex, CommandSource.Mqtt)
            : null;
    }

    private static ICommand<Result>? MapClientLatencyCommand(int clientIndex, string parameter)
    {
        return int.TryParse(parameter, out var latency)
            ? CommandFactory.CreateSetClientLatencyCommand(clientIndex, latency, CommandSource.Mqtt)
            : null;
    }
}
