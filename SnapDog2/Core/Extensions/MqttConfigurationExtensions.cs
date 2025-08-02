namespace SnapDog2.Core.Extensions;

using SnapDog2.Core.Configuration;
using SnapDog2.Core.Models;

/// <summary>
/// Extension methods for MQTT configuration to build topic structures.
/// </summary>
public static class MqttConfigurationExtensions
{
    /// <summary>
    /// Builds the complete MQTT topic configuration for a zone.
    /// </summary>
    /// <param name="zoneConfig">Zone configuration.</param>
    /// <returns>Complete zone MQTT topics configuration.</returns>
    public static ZoneMqttTopics BuildMqttTopics(this ZoneConfig zoneConfig)
    {
        ArgumentNullException.ThrowIfNull(zoneConfig);
        ArgumentNullException.ThrowIfNull(zoneConfig.Mqtt);

        var baseTopic = zoneConfig.Mqtt.BaseTopic?.TrimEnd('/') ?? string.Empty;

        return new ZoneMqttTopics
        {
            BaseTopic = baseTopic,
            Control = new ZoneControlTopics
            {
                ControlSet = $"{baseTopic}/{zoneConfig.Mqtt.ControlSetTopic}",
                TrackSet = $"{baseTopic}/{zoneConfig.Mqtt.TrackSetTopic}",
                TrackRepeatSet = $"{baseTopic}/{zoneConfig.Mqtt.TrackRepeatSetTopic}",
                PlaylistSet = $"{baseTopic}/{zoneConfig.Mqtt.PlaylistSetTopic}",
                PlaylistRepeatSet = $"{baseTopic}/{zoneConfig.Mqtt.PlaylistRepeatSetTopic}",
                PlaylistShuffleSet = $"{baseTopic}/{zoneConfig.Mqtt.PlaylistShuffleSetTopic}",
                VolumeSet = $"{baseTopic}/{zoneConfig.Mqtt.VolumeSetTopic}",
                MuteSet = $"{baseTopic}/{zoneConfig.Mqtt.MuteSetTopic}",
            },
            Status = new ZoneStatusTopics
            {
                Control = $"{baseTopic}/{zoneConfig.Mqtt.ControlTopic}",
                Track = $"{baseTopic}/{zoneConfig.Mqtt.TrackTopic}",
                TrackInfo = $"{baseTopic}/{zoneConfig.Mqtt.TrackInfoTopic}",
                TrackRepeat = $"{baseTopic}/{zoneConfig.Mqtt.TrackRepeatTopic}",
                Playlist = $"{baseTopic}/{zoneConfig.Mqtt.PlaylistTopic}",
                PlaylistInfo = $"{baseTopic}/{zoneConfig.Mqtt.PlaylistInfoTopic}",
                PlaylistRepeat = $"{baseTopic}/{zoneConfig.Mqtt.PlaylistRepeatTopic}",
                PlaylistShuffle = $"{baseTopic}/{zoneConfig.Mqtt.PlaylistShuffleTopic}",
                Volume = $"{baseTopic}/{zoneConfig.Mqtt.VolumeTopic}",
                Mute = $"{baseTopic}/{zoneConfig.Mqtt.MuteTopic}",
                State = $"{baseTopic}/{zoneConfig.Mqtt.StateTopic}",
            },
        };
    }

    /// <summary>
    /// Builds the complete MQTT topic configuration for a client.
    /// </summary>
    /// <param name="clientConfig">Client configuration.</param>
    /// <returns>Complete client MQTT topics configuration.</returns>
    public static ClientMqttTopics BuildMqttTopics(this ClientConfig clientConfig)
    {
        ArgumentNullException.ThrowIfNull(clientConfig);
        ArgumentNullException.ThrowIfNull(clientConfig.Mqtt);

        var baseTopic = clientConfig.Mqtt.BaseTopic?.TrimEnd('/') ?? string.Empty;

        return new ClientMqttTopics
        {
            BaseTopic = baseTopic,
            Control = new ClientControlTopics
            {
                VolumeSet = $"{baseTopic}/{clientConfig.Mqtt.VolumeSetTopic}",
                MuteSet = $"{baseTopic}/{clientConfig.Mqtt.MuteSetTopic}",
                LatencySet = $"{baseTopic}/{clientConfig.Mqtt.LatencySetTopic}",
                ZoneSet = $"{baseTopic}/{clientConfig.Mqtt.ZoneSetTopic}",
            },
            Status = new ClientStatusTopics
            {
                Connected = $"{baseTopic}/{clientConfig.Mqtt.ConnectedTopic}",
                Volume = $"{baseTopic}/{clientConfig.Mqtt.VolumeTopic}",
                Mute = $"{baseTopic}/{clientConfig.Mqtt.MuteTopic}",
                Latency = $"{baseTopic}/{clientConfig.Mqtt.LatencyTopic}",
                Zone = $"{baseTopic}/{clientConfig.Mqtt.ZoneTopic}",
                State = $"{baseTopic}/{clientConfig.Mqtt.StateTopic}",
            },
        };
    }

    /// <summary>
    /// Gets all control topics for a zone (for subscription).
    /// </summary>
    /// <param name="zoneTopics">Zone MQTT topics configuration.</param>
    /// <returns>List of all control topics to subscribe to.</returns>
    public static IEnumerable<string> GetAllControlTopics(this ZoneMqttTopics zoneTopics)
    {
        yield return zoneTopics.Control.ControlSet;
        yield return zoneTopics.Control.TrackSet;
        yield return zoneTopics.Control.TrackRepeatSet;
        yield return zoneTopics.Control.PlaylistSet;
        yield return zoneTopics.Control.PlaylistRepeatSet;
        yield return zoneTopics.Control.PlaylistShuffleSet;
        yield return zoneTopics.Control.VolumeSet;
        yield return zoneTopics.Control.MuteSet;
    }

    /// <summary>
    /// Gets all control topics for a client (for subscription).
    /// </summary>
    /// <param name="clientTopics">Client MQTT topics configuration.</param>
    /// <returns>List of all control topics to subscribe to.</returns>
    public static IEnumerable<string> GetAllControlTopics(this ClientMqttTopics clientTopics)
    {
        yield return clientTopics.Control.VolumeSet;
        yield return clientTopics.Control.MuteSet;
        yield return clientTopics.Control.LatencySet;
        yield return clientTopics.Control.ZoneSet;
    }

    /// <summary>
    /// Determines the zone ID from a topic path.
    /// </summary>
    /// <param name="topic">MQTT topic.</param>
    /// <param name="zoneTopics">List of zone topic configurations.</param>
    /// <returns>Zone ID if found, null otherwise.</returns>
    public static int? GetZoneIdFromTopic(string topic, IEnumerable<ZoneMqttTopics> zoneTopics)
    {
        foreach (var zoneConfig in zoneTopics)
        {
            if (topic.StartsWith(zoneConfig.BaseTopic, StringComparison.OrdinalIgnoreCase))
            {
                // Extract zone ID from base topic (assumes format like "snapdog/zones/1" or "snapdog/zones/living-room")
                var baseParts = zoneConfig.BaseTopic.Split('/');
                if (baseParts.Length >= 3 && int.TryParse(baseParts[^1], out var zoneId))
                {
                    return zoneId;
                }
                // If not numeric, we need to find the zone ID from configuration
                // This would require additional lookup logic
            }
        }
        return null;
    }

    /// <summary>
    /// Determines the client ID from a topic path.
    /// </summary>
    /// <param name="topic">MQTT topic.</param>
    /// <param name="clientTopics">List of client topic configurations.</param>
    /// <returns>Client ID if found, null otherwise.</returns>
    public static string? GetClientIdFromTopic(string topic, IEnumerable<ClientMqttTopics> clientTopics)
    {
        foreach (var clientConfig in clientTopics)
        {
            if (topic.StartsWith(clientConfig.BaseTopic, StringComparison.OrdinalIgnoreCase))
            {
                // Extract client ID from base topic (assumes format like "snapdog/clients/living-room")
                var baseParts = clientConfig.BaseTopic.Split('/');
                if (baseParts.Length >= 3)
                {
                    return baseParts[^1];
                }
            }
        }
        return null;
    }
}
