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

using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// Delegate for mapping MQTT commands to direct service calls.
/// </summary>
/// <param name="entityId">The entity ID (zone or client index).</param>
/// <param name="parameter">The command parameter (if any).</param>
/// <param name="zoneService">Zone service for zone operations.</param>
/// <param name="clientService">Client service for client operations.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The result of the service call.</returns>
public delegate Task<Result> MqttServiceCallDelegate(int entityId, string parameter, IZoneService zoneService, IClientService clientService, CancellationToken cancellationToken);

/// <summary>
/// Strategy for mapping MQTT commands to direct service calls.
/// </summary>
public static class MqttCommandMappingStrategy
{
    /// <summary>
    /// Zone command mappings using direct service calls.
    /// </summary>
    private static readonly Dictionary<string, MqttServiceCallDelegate> ZoneCommandMap = new()
    {
        // Playback commands
        [MqttConstants.Commands.PLAY] = MapZonePlayCommand,
        [MqttConstants.Commands.PAUSE] = (zoneIndex, _, zoneService, _, ct) => zoneService.PauseAsync(ct),
        [MqttConstants.Commands.STOP] = (zoneIndex, _, zoneService, _, ct) => zoneService.StopAsync(ct),

        // Navigation commands
        [MqttConstants.Commands.NEXT] = (zoneIndex, _, zoneService, _, ct) => zoneService.NextTrackAsync(ct),
        [MqttConstants.Commands.TRACK_NEXT] = (zoneIndex, _, zoneService, _, ct) => zoneService.NextTrackAsync(ct),
        [MqttConstants.Commands.PREVIOUS] = (zoneIndex, _, zoneService, _, ct) => zoneService.PreviousTrackAsync(ct),
        [MqttConstants.Commands.TRACK_PREVIOUS] = (zoneIndex, _, zoneService, _, ct) => zoneService.PreviousTrackAsync(ct),
        [MqttConstants.Commands.PLAYLIST_NEXT] = (zoneIndex, _, zoneService, _, ct) => zoneService.NextPlaylistAsync(ct),
        [MqttConstants.Commands.PLAYLIST_PREVIOUS] = (zoneIndex, _, zoneService, _, ct) => zoneService.PreviousPlaylistAsync(ct),

        // Volume commands
        [MqttConstants.Commands.VOLUME] = MapZoneVolumeCommand,
        [MqttConstants.Commands.VOLUME_UP] = (zoneIndex, _, zoneService, _, ct) => zoneService.VolumeUpAsync(5, ct),
        [MqttConstants.Commands.VOLUME_DOWN] = (zoneIndex, _, zoneService, _, ct) => zoneService.VolumeDownAsync(5, ct),

        // Mute commands
        [MqttConstants.Commands.MUTE_ON] = (zoneIndex, _, zoneService, _, ct) => zoneService.SetMuteAsync(true, ct),
        [MqttConstants.Commands.MUTE_OFF] = (zoneIndex, _, zoneService, _, ct) => zoneService.SetMuteAsync(false, ct),
        [MqttConstants.Commands.MUTE_TOGGLE] = (zoneIndex, _, zoneService, _, ct) => zoneService.ToggleMuteAsync(ct),

        // Track repeat commands
        [MqttConstants.Commands.TRACK_REPEAT_ON] = (zoneIndex, _, zoneService, _, ct) => zoneService.SetTrackRepeatAsync(true, ct),
        [MqttConstants.Commands.TRACK_REPEAT_OFF] = (zoneIndex, _, zoneService, _, ct) => zoneService.SetTrackRepeatAsync(false, ct),
        [MqttConstants.Commands.TRACK_REPEAT_TOGGLE] = (zoneIndex, _, zoneService, _, ct) => zoneService.ToggleTrackRepeatAsync(ct),

        // Playlist shuffle commands
        [MqttConstants.Commands.SHUFFLE_ON] = (zoneIndex, _, zoneService, _, ct) => zoneService.SetPlaylistShuffleAsync(true, ct),
        [MqttConstants.Commands.SHUFFLE_OFF] = (zoneIndex, _, zoneService, _, ct) => zoneService.SetPlaylistShuffleAsync(false, ct),
        [MqttConstants.Commands.SHUFFLE_TOGGLE] = (zoneIndex, _, zoneService, _, ct) => zoneService.TogglePlaylistShuffleAsync(ct),

        // Playlist repeat commands
        [MqttConstants.Commands.REPEAT_ON] = (zoneIndex, _, zoneService, _, ct) => zoneService.SetPlaylistRepeatAsync(true, ct),
        [MqttConstants.Commands.REPEAT_OFF] = (zoneIndex, _, zoneService, _, ct) => zoneService.SetPlaylistRepeatAsync(false, ct),
        [MqttConstants.Commands.REPEAT_TOGGLE] = (zoneIndex, _, zoneService, _, ct) => zoneService.TogglePlaylistRepeatAsync(ct),

        // Selection commands
        [MqttConstants.Commands.TRACK] = MapZoneTrackCommand,
        [MqttConstants.Commands.PLAYLIST] = MapZonePlaylistCommand,
    };

    /// <summary>
    /// Client command mappings using direct service calls.
    /// </summary>
    private static readonly Dictionary<string, MqttServiceCallDelegate> ClientCommandMap = new()
    {
        // Volume commands
        [MqttConstants.Commands.VOLUME] = MapClientVolumeCommand,
        [MqttConstants.Commands.VOLUME_UP] = (clientIndex, _, _, clientService, ct) => clientService.VolumeUpAsync(clientIndex, 5, ct),
        [MqttConstants.Commands.VOLUME_DOWN] = (clientIndex, _, _, clientService, ct) => clientService.VolumeDownAsync(clientIndex, 5, ct),

        // Mute commands
        [MqttConstants.Commands.MUTE_ON] = (clientIndex, _, _, clientService, ct) => clientService.SetMuteAsync(clientIndex, true, ct),
        [MqttConstants.Commands.MUTE_OFF] = (clientIndex, _, _, clientService, ct) => clientService.SetMuteAsync(clientIndex, false, ct),
        [MqttConstants.Commands.MUTE_TOGGLE] = (clientIndex, _, _, clientService, ct) => clientService.ToggleMuteAsync(clientIndex, ct),

        // Configuration commands
        [MqttConstants.Commands.ZONE_ASSIGNMENT] = MapClientZoneCommand,
        [MqttConstants.Commands.LATENCY] = MapClientLatencyCommand,
    };

    /// <summary>
    /// Executes a command from MQTT topic using direct service calls.
    /// </summary>
    public static async Task<Result> ExecuteFromMqttTopicAsync(
        string topic,
        string payload,
        IZoneService zoneService,
        IClientService clientService,
        CancellationToken cancellationToken = default)
    {
        // Parse topic to extract entity type, ID, and command
        var parts = topic.Split('/');
        if (parts.Length < 4)
        {
            return Result.Failure("Invalid MQTT topic format");
        }

        var entityType = parts[1]; // zones or clients
        if (!int.TryParse(parts[2], out var entityId))
        {
            return Result.Failure("Invalid entity ID");
        }

        var command = parts[3];

        // Route to appropriate service
        return entityType.ToLowerInvariant() switch
        {
            "zones" => await MapZoneCommand(command, entityId, payload, zoneService, clientService, cancellationToken),
            "clients" => await MapClientCommand(command, entityId, payload, zoneService, clientService, cancellationToken),
            _ => Result.Failure($"Unknown entity type: {entityType}")
        };
    }

    /// <summary>
    /// Maps a zone command using direct service calls.
    /// </summary>
    private static async Task<Result> MapZoneCommand(string command, int zoneIndex, string parameter, IZoneService zoneService, IClientService clientService, CancellationToken cancellationToken)
    {
        if (ZoneCommandMap.TryGetValue(command, out var mapper))
        {
            return await mapper(zoneIndex, parameter, zoneService, clientService, cancellationToken);
        }
        return Result.Failure($"Unknown zone command: {command}");
    }

    /// <summary>
    /// Maps a client command using direct service calls.
    /// </summary>
    private static async Task<Result> MapClientCommand(string command, int clientIndex, string parameter, IZoneService zoneService, IClientService clientService, CancellationToken cancellationToken)
    {
        if (ClientCommandMap.TryGetValue(command, out var mapper))
        {
            return await mapper(clientIndex, parameter, zoneService, clientService, cancellationToken);
        }
        return Result.Failure($"Unknown client command: {command}");
    }

    // Complex command mappers with parameter parsing
    private static async Task<Result> MapZonePlayCommand(int zoneIndex, string parameter, IZoneService zoneService, IClientService clientService, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(parameter))
        {
            return await zoneService.PlayAsync();
        }

        if (parameter.StartsWith(MqttConstants.Parameters.TRACK_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            var trackIndexStr = parameter[MqttConstants.Parameters.TRACK_PREFIX.Length..].Trim();
            if (int.TryParse(trackIndexStr, out var trackIndex))
            {
                return await zoneService.PlayTrackAsync(trackIndex, cancellationToken);
            }
        }

        if (parameter.StartsWith(MqttConstants.Parameters.URL_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            var url = parameter[MqttConstants.Parameters.URL_PREFIX.Length..].Trim();
            return await zoneService.PlayUrlAsync(url);
        }

        return Result.Failure("Invalid play command parameter");
    }

    private static async Task<Result> MapZoneVolumeCommand(int zoneIndex, string parameter, IZoneService zoneService, IClientService clientService, CancellationToken cancellationToken)
    {
        if (int.TryParse(parameter, out var volume))
        {
            return await zoneService.SetVolumeAsync(volume, cancellationToken);
        }
        return Result.Failure("Invalid volume parameter");
    }

    private static async Task<Result> MapZoneTrackCommand(int zoneIndex, string parameter, IZoneService zoneService, IClientService clientService, CancellationToken cancellationToken)
    {
        if (int.TryParse(parameter, out var trackIndex))
        {
            return await zoneService.SetTrackAsync(trackIndex, cancellationToken);
        }
        return Result.Failure("Invalid track parameter");
    }

    private static async Task<Result> MapZonePlaylistCommand(int zoneIndex, string parameter, IZoneService zoneService, IClientService clientService, CancellationToken cancellationToken)
    {
        if (int.TryParse(parameter, out var playlistIndex))
        {
            return await zoneService.SetPlaylistAsync(playlistIndex, cancellationToken);
        }
        return Result.Failure("Invalid playlist parameter");
    }

    private static async Task<Result> MapClientVolumeCommand(int clientIndex, string parameter, IZoneService zoneService, IClientService clientService, CancellationToken cancellationToken)
    {
        if (int.TryParse(parameter, out var volume))
        {
            return await clientService.SetVolumeAsync(clientIndex, volume, cancellationToken);
        }
        return Result.Failure("Invalid volume parameter");
    }

    private static async Task<Result> MapClientZoneCommand(int clientIndex, string parameter, IZoneService zoneService, IClientService clientService, CancellationToken cancellationToken)
    {
        if (int.TryParse(parameter, out var zoneIndex))
        {
            return await clientService.AssignToZoneAsync(clientIndex, zoneIndex, cancellationToken);
        }
        return Result.Failure("Invalid zone parameter");
    }

    private static async Task<Result> MapClientLatencyCommand(int clientIndex, string parameter, IZoneService zoneService, IClientService clientService, CancellationToken cancellationToken)
    {
        if (int.TryParse(parameter, out var latency))
        {
            return await clientService.SetLatencyAsync(clientIndex, latency, cancellationToken);
        }
        return Result.Failure("Invalid latency parameter");
    }
}
