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

using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// KNX implementation of integration publisher.
/// </summary>
public class KnxIntegrationPublisher : IIntegrationPublisher
{
    private readonly IKnxService? _knxService;
    private readonly SnapDogConfiguration _configuration;

    public KnxIntegrationPublisher(IServiceProvider serviceProvider, SnapDogConfiguration configuration)
    {
        _knxService = serviceProvider.GetService<IKnxService>();
        _configuration = configuration;
    }

    public string Name => "KNX";

    public bool IsEnabled => _knxService?.IsConnected == true;

    public async Task PublishZonePlaylistChangedAsync(int zoneIndex, PlaylistInfo? playlist, CancellationToken cancellationToken = default)
    {
        if (_knxService == null)
        {
            return;
        }

        var playlistIndex = playlist?.Index ?? 0;
        var knxValue = playlistIndex > 255 ? 0 : playlistIndex; // KNX DPT 5.010 limitation

        var groupAddress = GetZonePlaylistStatusGA(zoneIndex);
        if (groupAddress != null)
        {
            await _knxService.WriteGroupValueAsync(groupAddress, knxValue, cancellationToken);
        }
    }

    public async Task PublishZoneVolumeChangedAsync(int zoneIndex, int volume, CancellationToken cancellationToken = default)
    {
        if (_knxService == null)
        {
            return;
        }

        var knxValue = Math.Clamp(volume, 0, 100);
        var groupAddress = GetZoneVolumeStatusGA(zoneIndex);
        if (groupAddress != null)
        {
            await _knxService.WriteGroupValueAsync(groupAddress, knxValue, cancellationToken);
        }
    }

    public async Task PublishZoneTrackChangedAsync(int zoneIndex, TrackInfo? track, CancellationToken cancellationToken = default)
    {
        if (_knxService == null)
        {
            return;
        }

        var trackIndex = track?.Index ?? 0;
        var knxValue = trackIndex > 255 ? 0 : trackIndex; // KNX DPT 5.010 limitation

        var groupAddress = GetZoneTrackStatusGA(zoneIndex);
        if (groupAddress != null)
        {
            await _knxService.WriteGroupValueAsync(groupAddress, knxValue, cancellationToken);
        }
    }

    public async Task PublishZonePlaybackStateChangedAsync(int zoneIndex, PlaybackState playbackState, CancellationToken cancellationToken = default)
    {
        if (_knxService == null)
        {
            return;
        }

        var knxValue = (int)playbackState;
        var groupAddress = GetZonePlaybackStatusGA(zoneIndex);
        if (groupAddress != null)
        {
            await _knxService.WriteGroupValueAsync(groupAddress, knxValue, cancellationToken);
        }
    }

    private string? GetZonePlaylistStatusGA(int zoneIndex)
    {
        return zoneIndex <= _configuration.Zones.Count
            ? _configuration.Zones[zoneIndex - 1].Knx?.PlaylistStatus
            : null;
    }

    private string? GetZoneVolumeStatusGA(int zoneIndex)
    {
        return zoneIndex <= _configuration.Zones.Count
            ? _configuration.Zones[zoneIndex - 1].Knx?.VolumeStatus
            : null;
    }

    private string? GetZoneTrackStatusGA(int zoneIndex)
    {
        return zoneIndex <= _configuration.Zones.Count
            ? _configuration.Zones[zoneIndex - 1].Knx?.TrackStatus
            : null;
    }

    private string? GetZonePlaybackStatusGA(int zoneIndex)
    {
        return zoneIndex <= _configuration.Zones.Count
            ? _configuration.Zones[zoneIndex - 1].Knx?.TrackPlayingStatus
            : null;
    }
}
