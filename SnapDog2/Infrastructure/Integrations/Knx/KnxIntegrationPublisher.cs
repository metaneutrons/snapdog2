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
namespace SnapDog2.Infrastructure.Integrations.Knx;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// Enterprise-grade KNX integration publisher for real-time state synchronization.
/// </summary>
public partial class KnxIntegrationPublisher : IIntegrationPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KnxIntegrationPublisher> _logger;
    private readonly SnapDogConfiguration _configuration;

    public KnxIntegrationPublisher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<KnxIntegrationPublisher>>();
        _configuration = serviceProvider.GetRequiredService<IOptions<SnapDogConfiguration>>().Value;
    }

    public string Name => "KNX";
    public bool IsEnabled => _configuration.Services.Knx.Enabled;

    public async Task PublishZonePlaylistChangedAsync(int zoneIndex, PlaylistInfo? playlist, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return;
        }

        var zoneConfig = GetZoneKnxConfig(zoneIndex);
        if (zoneConfig?.Enabled != true)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var knxService = scope.ServiceProvider.GetService<IKnxService>();
        if (knxService == null)
        {
            return;
        }

        // Send playlist status to KNX
        if (!string.IsNullOrEmpty(zoneConfig.PlaylistStatus))
        {
            await knxService.WriteGroupValueAsync(zoneConfig.PlaylistStatus, playlist?.Name ?? "");
        }
    }

    public async Task PublishZoneVolumeChangedAsync(int zoneIndex, int volume, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return;
        }

        var zoneConfig = GetZoneKnxConfig(zoneIndex);
        if (zoneConfig?.Enabled != true)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var knxService = scope.ServiceProvider.GetService<IKnxService>();
        if (knxService == null)
        {
            return;
        }

        // Send volume status to KNX
        if (!string.IsNullOrEmpty(zoneConfig.VolumeStatus))
        {
            await knxService.WriteGroupValueAsync(zoneConfig.VolumeStatus, volume);
        }
    }

    public async Task PublishZoneTrackChangedAsync(int zoneIndex, TrackInfo? track, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return;
        }

        var zoneConfig = GetZoneKnxConfig(zoneIndex);
        if (zoneConfig?.Enabled != true)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var knxService = scope.ServiceProvider.GetService<IKnxService>();
        if (knxService == null)
        {
            return;
        }

        // Send track metadata to KNX
        if (track != null)
        {
            if (!string.IsNullOrEmpty(zoneConfig.TrackTitleStatus))
            {
                await knxService.WriteGroupValueAsync(zoneConfig.TrackTitleStatus, track.Title ?? "");
            }

            if (!string.IsNullOrEmpty(zoneConfig.TrackArtistStatus))
            {
                await knxService.WriteGroupValueAsync(zoneConfig.TrackArtistStatus, track.Artist ?? "");
            }

            if (!string.IsNullOrEmpty(zoneConfig.TrackAlbumStatus))
            {
                await knxService.WriteGroupValueAsync(zoneConfig.TrackAlbumStatus, track.Album ?? "");
            }
        }
    }

    public async Task PublishZonePlaybackStateChangedAsync(int zoneIndex, PlaybackState playbackState, CancellationToken cancellationToken = default)
    {
        LogKnxPublish("ZonePlaybackStateChanged", zoneIndex, playbackState);

        if (!IsEnabled)
        {
            return;
        }

        var zoneConfig = GetZoneKnxConfig(zoneIndex);
        if (zoneConfig?.Enabled != true)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var knxService = scope.ServiceProvider.GetService<IKnxService>();
        if (knxService == null)
        {
            return;
        }

        // Send playing status to KNX (1 = playing, 0 = not playing)
        if (!string.IsNullOrEmpty(zoneConfig.TrackPlayingStatus))
        {
            var isPlaying = playbackState == PlaybackState.Playing ? 1 : 0;
            await knxService.WriteGroupValueAsync(zoneConfig.TrackPlayingStatus, isPlaying);
        }
    }

    public async Task PublishClientVolumeChangedAsync(int clientIndex, int volume, CancellationToken cancellationToken = default)
    {
        // Per blueprint: Client-specific network settings not suitable for building automation
        await Task.CompletedTask;
    }

    public async Task PublishClientConnectionChangedAsync(int clientIndex, bool connected, CancellationToken cancellationToken = default)
    {
        // Per blueprint: Client-specific network settings not suitable for building automation
        await Task.CompletedTask;
    }

    public async Task PublishClientNameChangedAsync(int clientIndex, string name, CancellationToken cancellationToken = default)
    {
        // Per blueprint: Client-specific network settings not suitable for building automation
        await Task.CompletedTask;
    }

    public async Task PublishClientLatencyChangedAsync(int clientIndex, int latencyMs, CancellationToken cancellationToken = default)
    {
        // Per blueprint: Network-specific setting not suitable for building automation
        await Task.CompletedTask;
    }

    [LoggerMessage(EventId = 121001, Level = LogLevel.Information, Message = "KNX publish {EventType} for entity {EntityIndex}: {Value}")]
    private partial void LogKnxPublish(string eventType, int entityIndex, object value);

    private ZoneKnxConfig? GetZoneKnxConfig(int zoneIndex)
    {
        var zoneConfigIndex = zoneIndex - 1; // Convert 1-based to 0-based
        return zoneConfigIndex >= 0 && zoneConfigIndex < _configuration.Zones.Count
            ? _configuration.Zones[zoneConfigIndex].Knx
            : null;
    }
}
