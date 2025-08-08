namespace SnapDog2.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using SnapDog2.Core.Enums;
using SnapDog2.Server.Features.Clients.Commands.Config;
using SnapDog2.Server.Features.Clients.Commands.Volume;
using SnapDog2.Server.Features.Zones.Commands.Playback;
using SnapDog2.Server.Features.Zones.Commands.Playlist;
using SnapDog2.Server.Features.Zones.Commands.Track;
using SnapDog2.Server.Features.Zones.Commands.Volume;

/// <summary>
/// Improved MQTT command mapping that follows DRY principles and implements all blueprint commands.
/// This demonstrates a more elegant approach to command mapping.
/// </summary>
public partial class ImprovedMqttCommandMapper
{
    private readonly ILogger<ImprovedMqttCommandMapper> _logger;

    public ImprovedMqttCommandMapper(ILogger<ImprovedMqttCommandMapper> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Maps MQTT topics to Cortex.Mediator commands based on the topic structure.
    /// Follows the blueprint specification from Section 14.
    /// </summary>
    public object? MapTopicToCommand(string topic, string payload)
    {
        try
        {
            // Parse the topic structure: snapdog/{zone|client}/{id}/{command}
            var topicParts = topic.Split('/');
            if (topicParts.Length < 4 || !topicParts[0].Equals("snapdog", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var entityType = topicParts[1].ToLowerInvariant();
            var entityId = topicParts[2];
            var command = topicParts[3].ToLowerInvariant();

            return entityType switch
            {
                "zone" when int.TryParse(entityId, out var zoneId) => this.MapZoneCommand(zoneId, command, payload),
                "client" when int.TryParse(entityId, out var clientId) => this.MapClientCommand(
                    clientId,
                    command,
                    payload
                ),
                _ => null,
            };
        }
        catch (Exception ex)
        {
            this.LogMappingError(ex, topic);
            return null;
        }
    }

    /// <summary>
    /// Maps zone-specific MQTT commands to Mediator commands following the blueprint specification.
    /// </summary>
    private object? MapZoneCommand(int zoneId, string command, string payload)
    {
        return command switch
        {
            // Playback Control Commands (Section 14.3.1)
            "play" => CreatePlayCommand(zoneId, payload),
            "pause" => new PauseCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },
            "stop" => new StopCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },

            // Volume Control Commands
            "volume" when TryParseVolume(payload, out var volume) => new SetZoneVolumeCommand
            {
                ZoneId = zoneId,
                Volume = volume,
                Source = CommandSource.Mqtt,
            },
            "volume" when payload.Equals("+", StringComparison.OrdinalIgnoreCase) => new VolumeUpCommand
            {
                ZoneId = zoneId,
                Source = CommandSource.Mqtt,
            },
            "volume" when payload.Equals("-", StringComparison.OrdinalIgnoreCase) => new VolumeDownCommand
            {
                ZoneId = zoneId,
                Source = CommandSource.Mqtt,
            },
            "volume" when TryParseVolumeStep(payload, out var step, out var direction) => direction > 0
                ? new VolumeUpCommand
                {
                    ZoneId = zoneId,
                    Step = step,
                    Source = CommandSource.Mqtt,
                }
                : new VolumeDownCommand
                {
                    ZoneId = zoneId,
                    Step = step,
                    Source = CommandSource.Mqtt,
                },

            // Mute Control Commands
            "mute" when TryParseBool(payload, out var mute) => new SetZoneMuteCommand
            {
                ZoneId = zoneId,
                Enabled = mute,
                Source = CommandSource.Mqtt,
            },
            "mute" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) => new ToggleZoneMuteCommand
            {
                ZoneId = zoneId,
                Source = CommandSource.Mqtt,
            },

            // Track Management Commands
            "track" when int.TryParse(payload, out var trackIndex) && trackIndex > 0 => new SetTrackCommand
            {
                ZoneId = zoneId,
                TrackIndex = trackIndex,
                Source = CommandSource.Mqtt,
            },
            "track" when payload.Equals("+", StringComparison.OrdinalIgnoreCase) => new NextTrackCommand
            {
                ZoneId = zoneId,
                Source = CommandSource.Mqtt,
            },
            "track" when payload.Equals("-", StringComparison.OrdinalIgnoreCase) => new PreviousTrackCommand
            {
                ZoneId = zoneId,
                Source = CommandSource.Mqtt,
            },
            "next" => new NextTrackCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },
            "previous" => new PreviousTrackCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },

            // Track Repeat Commands
            "track_repeat" when TryParseBool(payload, out var repeat) => new SetTrackRepeatCommand
            {
                ZoneId = zoneId,
                Enabled = repeat,
                Source = CommandSource.Mqtt,
            },
            "track_repeat" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) =>
                new ToggleTrackRepeatCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },

            // Playlist Management Commands
            "playlist" when int.TryParse(payload, out var playlistIndex) && playlistIndex > 0 => new SetPlaylistCommand
            {
                ZoneId = zoneId,
                PlaylistIndex = playlistIndex,
                Source = CommandSource.Mqtt,
            },
            "playlist" when payload.Equals("+", StringComparison.OrdinalIgnoreCase) => new NextPlaylistCommand
            {
                ZoneId = zoneId,
                Source = CommandSource.Mqtt,
            },
            "playlist" when payload.Equals("-", StringComparison.OrdinalIgnoreCase) => new PreviousPlaylistCommand
            {
                ZoneId = zoneId,
                Source = CommandSource.Mqtt,
            },

            // Playlist Mode Commands
            "playlist_shuffle" when TryParseBool(payload, out var shuffle) => new SetPlaylistShuffleCommand
            {
                ZoneId = zoneId,
                Enabled = shuffle,
                Source = CommandSource.Mqtt,
            },
            "playlist_shuffle" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) =>
                new TogglePlaylistShuffleCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },
            "playlist_repeat" when TryParseBool(payload, out var playlistRepeat) => new SetPlaylistRepeatCommand
            {
                ZoneId = zoneId,
                Enabled = playlistRepeat,
                Source = CommandSource.Mqtt,
            },
            "playlist_repeat" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) =>
                new TogglePlaylistRepeatCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },

            _ => null,
        };
    }

    /// <summary>
    /// Maps client-specific MQTT commands to Mediator commands following the blueprint specification.
    /// </summary>
    private object? MapClientCommand(int clientId, string command, string payload)
    {
        return command switch
        {
            // Volume Control Commands
            "volume" when TryParseVolume(payload, out var volume) => new SetClientVolumeCommand
            {
                ClientId = clientId,
                Volume = volume,
                Source = CommandSource.Mqtt,
            },

            // Mute Control Commands
            "mute" when TryParseBool(payload, out var mute) => new SetClientMuteCommand
            {
                ClientId = clientId,
                Enabled = mute,
                Source = CommandSource.Mqtt,
            },
            "mute" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) => new ToggleClientMuteCommand
            {
                ClientId = clientId,
                Source = CommandSource.Mqtt,
            },

            // Configuration Commands
            "latency" when int.TryParse(payload, out var latency) && latency >= 0 => new SetClientLatencyCommand
            {
                ClientId = clientId,
                LatencyMs = latency,
                Source = CommandSource.Mqtt,
            },
            "zone" when int.TryParse(payload, out var zoneId) && zoneId > 0 => new AssignClientToZoneCommand
            {
                ClientId = clientId,
                ZoneId = zoneId,
                Source = CommandSource.Mqtt,
            },

            _ => null,
        };
    }

    /// <summary>
    /// Creates a PlayCommand with optional parameters based on payload.
    /// </summary>
    private static PlayCommand CreatePlayCommand(int zoneId, string payload)
    {
        // Handle different play command formats:
        // "play" - simple play
        // "play url <url>" - play specific URL
        // "play track <index>" - play specific track

        if (string.IsNullOrWhiteSpace(payload) || payload.Equals("play", StringComparison.OrdinalIgnoreCase))
        {
            return new PlayCommand { ZoneId = zoneId, Source = CommandSource.Mqtt };
        }

        var parts = payload.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return parts[0].ToLowerInvariant() switch
            {
                "url" => new PlayCommand
                {
                    ZoneId = zoneId,
                    MediaUrl = parts[1],
                    Source = CommandSource.Mqtt,
                },
                "track" when int.TryParse(parts[1], out var trackIndex) && trackIndex > 0 => new PlayCommand
                {
                    ZoneId = zoneId,
                    TrackIndex = trackIndex,
                    Source = CommandSource.Mqtt,
                },
                _ => new PlayCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },
            };
        }

        return new PlayCommand { ZoneId = zoneId, Source = CommandSource.Mqtt };
    }

    // Helper methods for parsing different payload formats

    private static bool TryParseVolume(string payload, out int volume)
    {
        volume = 0;
        return int.TryParse(payload, out volume) && volume >= 0 && volume <= 100;
    }

    private static bool TryParseBool(string payload, out bool value)
    {
        value = false;
        return payload.ToLowerInvariant() switch
        {
            "true" or "1" or "on" or "yes" => (value = true) == true,
            "false" or "0" or "off" or "no" => (value = false) == false,
            _ => false,
        };
    }

    private static bool TryParseVolumeStep(string payload, out int step, out int direction)
    {
        step = 5; // Default step
        direction = 0;

        if (payload.StartsWith('+'))
        {
            direction = 1;
            var stepStr = payload[1..];
            return string.IsNullOrEmpty(stepStr) || (int.TryParse(stepStr, out step) && step > 0 && step <= 50);
        }

        if (payload.StartsWith('-'))
        {
            direction = -1;
            var stepStr = payload[1..];
            return string.IsNullOrEmpty(stepStr) || (int.TryParse(stepStr, out step) && step > 0 && step <= 50);
        }

        return false;
    }

    // Logger message definitions
    [LoggerMessage(4001, LogLevel.Error, "Failed to map MQTT topic {Topic} to command")]
    private partial void LogMappingError(Exception ex, string topic);
}
