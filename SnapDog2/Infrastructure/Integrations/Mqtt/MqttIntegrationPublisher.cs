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

using Microsoft.Extensions.Options;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// MQTT implementation of integration publisher.
/// </summary>
public class MqttIntegrationPublisher : IIntegrationPublisher
{
    private readonly IMqttService? _mqttService;
    private readonly SnapDogConfiguration _configuration;

    public MqttIntegrationPublisher(IServiceProvider serviceProvider)
    {
        _mqttService = serviceProvider.GetService<IMqttService>();
        _configuration = serviceProvider.GetRequiredService<IOptions<SnapDogConfiguration>>().Value;
    }

    public string Name => "MQTT";

    public bool IsEnabled => _configuration.Services.Mqtt.Enabled;

    public async Task PublishZonePlaylistChangedAsync(int zoneIndex, PlaylistInfo? playlist, CancellationToken cancellationToken = default)
    {
        if (_mqttService == null)
        {
            return;
        }

        var topic = $"snapdog/zones/{zoneIndex}/playlist";
        var payload = playlist?.Index.ToString() ?? "0";
        await _mqttService.PublishAsync(topic, payload, retain: true, cancellationToken);

        var nameTopic = $"snapdog/zones/{zoneIndex}/playlist/name";
        var namePayload = playlist?.Name ?? "";
        await _mqttService.PublishAsync(nameTopic, namePayload, retain: true, cancellationToken);
    }

    public async Task PublishZoneVolumeChangedAsync(int zoneIndex, int volume, CancellationToken cancellationToken = default)
    {
        if (_mqttService == null)
        {
            return;
        }

        var topic = $"snapdog/zones/{zoneIndex}/volume";
        await _mqttService.PublishAsync(topic, volume.ToString(), retain: true, cancellationToken);
    }

    public async Task PublishZoneTrackChangedAsync(int zoneIndex, TrackInfo? track, CancellationToken cancellationToken = default)
    {
        if (_mqttService == null)
        {
            return;
        }

        var topic = $"snapdog/zones/{zoneIndex}/track";
        var payload = track?.Index?.ToString() ?? "0";
        await _mqttService.PublishAsync(topic, payload, retain: true, cancellationToken);

        var titleTopic = $"snapdog/zones/{zoneIndex}/track/title";
        var titlePayload = track?.Title ?? "";
        await _mqttService.PublishAsync(titleTopic, titlePayload, retain: true, cancellationToken);
    }

    public async Task PublishZonePlaybackStateChangedAsync(int zoneIndex, PlaybackState playbackState, CancellationToken cancellationToken = default)
    {
        if (_mqttService == null)
        {
            return;
        }

        var topic = $"snapdog/zones/{zoneIndex}/playback";
        var payload = ((int)playbackState).ToString();
        await _mqttService.PublishAsync(topic, payload, retain: true, cancellationToken);
    }

    public async Task PublishClientVolumeChangedAsync(int clientIndex, int volume, CancellationToken cancellationToken = default)
    {
        if (_mqttService == null)
        {
            return;
        }

        var topic = $"snapdog/clients/{clientIndex}/volume";
        await _mqttService.PublishAsync(topic, volume.ToString(), retain: true, cancellationToken);
    }

    public async Task PublishClientConnectionChangedAsync(int clientIndex, bool connected, CancellationToken cancellationToken = default)
    {
        if (_mqttService == null)
        {
            return;
        }

        var topic = $"snapdog/clients/{clientIndex}/connected";
        var payload = connected ? "1" : "0";
        await _mqttService.PublishAsync(topic, payload, retain: true, cancellationToken);
    }

    public async Task PublishClientNameChangedAsync(int clientIndex, string name, CancellationToken cancellationToken = default)
    {
        if (_mqttService == null)
        {
            return;
        }

        var topic = $"snapdog/clients/{clientIndex}/name";
        await _mqttService.PublishAsync(topic, name, retain: true, cancellationToken);
    }

    public async Task PublishClientLatencyChangedAsync(int clientIndex, int latencyMs, CancellationToken cancellationToken = default)
    {
        if (_mqttService == null)
        {
            return;
        }

        var topic = $"snapdog/clients/{clientIndex}/latency";
        await _mqttService.PublishAsync(topic, latencyMs.ToString(), retain: true, cancellationToken);
    }
}
