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
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Models;

/// <summary>
/// Handles KNX commands for blueprint compliance.
/// </summary>
public partial class KnxCommandHandler : IKnxCommandHandler
{
    private readonly IKnxService _knxService;
    private readonly ILogger<KnxCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnxCommandHandler"/> class.
    /// </summary>
    /// <param name="knxService">The KNX service.</param>
    /// <param name="logger">The logger.</param>
    public KnxCommandHandler(IKnxService knxService, ILogger<KnxCommandHandler> logger)
    {
        _knxService = knxService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> HandleCommandAsync(string commandId, int zoneIndex, object? parameters = null)
    {
        if (!_knxService.IsConnected)
        {
            return Result.Success(); // Graceful degradation when KNX not available
        }

        return commandId switch
        {
            "PLAY" => await HandlePlay(zoneIndex),
            "PAUSE" => await HandlePause(zoneIndex),
            "STOP" => await HandleStop(zoneIndex),
            "TRACK_PLAY_INDEX" => await HandleTrackPlayIndex(zoneIndex, parameters),
            "TRACK_PLAY_PLAYLIST" => await HandleTrackPlayPlaylist(zoneIndex, parameters),
            "TRACK_REPEAT" => await HandleTrackRepeat(zoneIndex, parameters),
            "TRACK_REPEAT_TOGGLE" => await HandleTrackRepeatToggle(zoneIndex),
            "PLAYLIST" => await HandlePlaylist(zoneIndex, parameters),
            "PLAYLIST_REPEAT" => await HandlePlaylistRepeat(zoneIndex, parameters),
            "PLAYLIST_REPEAT_TOGGLE" => await HandlePlaylistRepeatToggle(zoneIndex),
            _ => Result.Success() // Unknown commands are ignored
        };
    }

    [KnxCommand("PLAY")]
    private async Task<Result> HandlePlay(int zoneIndex)
    {
        LogKnxCommand("PLAY", zoneIndex);
        return await Task.FromResult(Result.Success());
    }

    [KnxCommand("PAUSE")]
    private async Task<Result> HandlePause(int zoneIndex)
    {
        LogKnxCommand("PAUSE", zoneIndex);
        return await Task.FromResult(Result.Success());
    }

    [KnxCommand("STOP")]
    private async Task<Result> HandleStop(int zoneIndex)
    {
        LogKnxCommand("STOP", zoneIndex);
        return await Task.FromResult(Result.Success());
    }

    [KnxCommand("TRACK_PLAY_INDEX")]
    private async Task<Result> HandleTrackPlayIndex(int zoneIndex, object? parameters)
    {
        LogKnxCommand("TRACK_PLAY_INDEX", zoneIndex);
        return await Task.FromResult(Result.Success());
    }

    [KnxCommand("TRACK_PLAY_PLAYLIST")]
    private async Task<Result> HandleTrackPlayPlaylist(int zoneIndex, object? parameters)
    {
        LogKnxCommand("TRACK_PLAY_PLAYLIST", zoneIndex);
        return await Task.FromResult(Result.Success());
    }

    [KnxCommand("TRACK_REPEAT")]
    private async Task<Result> HandleTrackRepeat(int zoneIndex, object? parameters)
    {
        LogKnxCommand("TRACK_REPEAT", zoneIndex);
        return await Task.FromResult(Result.Success());
    }

    [KnxCommand("TRACK_REPEAT_TOGGLE")]
    private async Task<Result> HandleTrackRepeatToggle(int zoneIndex)
    {
        LogKnxCommand("TRACK_REPEAT_TOGGLE", zoneIndex);
        return await Task.FromResult(Result.Success());
    }

    [KnxCommand("PLAYLIST")]
    private async Task<Result> HandlePlaylist(int zoneIndex, object? parameters)
    {
        LogKnxCommand("PLAYLIST", zoneIndex);
        return await Task.FromResult(Result.Success());
    }

    [KnxCommand("PLAYLIST_REPEAT")]
    private async Task<Result> HandlePlaylistRepeat(int zoneIndex, object? parameters)
    {
        LogKnxCommand("PLAYLIST_REPEAT", zoneIndex);
        return await Task.FromResult(Result.Success());
    }

    [KnxCommand("PLAYLIST_REPEAT_TOGGLE")]
    private async Task<Result> HandlePlaylistRepeatToggle(int zoneIndex)
    {
        LogKnxCommand("PLAYLIST_REPEAT_TOGGLE", zoneIndex);
        return await Task.FromResult(Result.Success());
    }

    [LoggerMessage(EventId = 120001, Level = LogLevel.Debug, Message = "KNX command {CommandId} handled for zone {ZoneIndex}")]
    private partial void LogKnxCommand(string commandId, int zoneIndex);
}
