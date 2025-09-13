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

using Microsoft.Extensions.Options;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Configuration;

/// <summary>
/// KNX command receiver for handling incoming KNX commands.
/// </summary>
public partial class KnxCommandReceiver : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IZoneManager _zoneManager;
    private readonly IClientManager _clientManager;
    private readonly ILogger<KnxCommandReceiver> _logger;
    private readonly SnapDogConfiguration _configuration;

    public KnxCommandReceiver(
        IServiceProvider serviceProvider,
        IZoneManager zoneManager,
        IClientManager clientManager,
        ILogger<KnxCommandReceiver> logger,
        IOptions<SnapDogConfiguration> configuration)
    {
        _serviceProvider = serviceProvider;
        _zoneManager = zoneManager;
        _clientManager = clientManager;
        _logger = logger;
        _configuration = configuration.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.Services.Knx.Enabled)
        {
            LogKnxCommandReceiverDisabled();
            return Task.CompletedTask;
        }

        // TODO: Subscribe to KNX bus for incoming commands
        // This would require KNX service to support command reception
        LogKnxCommandReceiverStarted();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // TODO: Unsubscribe from KNX bus
        return Task.CompletedTask;
    }

    // Zone Playback Commands
    [CommandId("PLAY")]
    public async Task<bool> HandlePlayCommand(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (!zoneResult.IsSuccess || zoneResult.Value == null)
        {
            return false;
        }

        var result = await zoneResult.Value.PlayAsync();
        LogKnxCommandHandled("PLAY", zoneIndex, result.IsSuccess);
        return result.IsSuccess;
    }

    [CommandId("PAUSE")]
    public async Task<bool> HandlePauseCommand(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (!zoneResult.IsSuccess || zoneResult.Value == null)
        {
            return false;
        }

        var result = await zoneResult.Value.PauseAsync();
        LogKnxCommandHandled("PAUSE", zoneIndex, result.IsSuccess);
        return result.IsSuccess;
    }

    [CommandId("STOP")]
    public async Task<bool> HandleStopCommand(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (!zoneResult.IsSuccess || zoneResult.Value == null)
        {
            return false;
        }

        var result = await zoneResult.Value.StopAsync();
        LogKnxCommandHandled("STOP", zoneIndex, result.IsSuccess);
        return result.IsSuccess;
    }

    // Track Commands
    [CommandId("TRACK_NEXT")]
    public async Task<bool> HandleTrackNextCommand(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (!zoneResult.IsSuccess || zoneResult.Value == null)
        {
            return false;
        }

        var result = await zoneResult.Value.NextTrackAsync();
        LogKnxCommandHandled("TRACK_NEXT", zoneIndex, result.IsSuccess);
        return result.IsSuccess;
    }

    [CommandId("TRACK_PREVIOUS")]
    public async Task<bool> HandleTrackPreviousCommand(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (!zoneResult.IsSuccess || zoneResult.Value == null)
        {
            return false;
        }

        var result = await zoneResult.Value.PreviousTrackAsync();
        LogKnxCommandHandled("TRACK_PREVIOUS", zoneIndex, result.IsSuccess);
        return result.IsSuccess;
    }

    [CommandId("TRACK")]
    public async Task<bool> HandleTrackCommand(int zoneIndex, int trackIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (!zoneResult.IsSuccess || zoneResult.Value == null)
        {
            return false;
        }

        var result = await zoneResult.Value.PlayTrackAsync(trackIndex);
        LogKnxCommandHandled("TRACK", zoneIndex, result.IsSuccess);
        return result.IsSuccess;
    }

    [CommandId("TRACK_REPEAT")]
    public async Task<bool> HandleTrackRepeatCommand(int zoneIndex, bool repeat)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (!zoneResult.IsSuccess || zoneResult.Value == null)
        {
            return false;
        }

        var result = await zoneResult.Value.SetTrackRepeatAsync(repeat);
        LogKnxCommandHandled("TRACK_REPEAT", zoneIndex, result.IsSuccess);
        return result.IsSuccess;
    }

    [CommandId("TRACK_REPEAT_TOGGLE")]
    public async Task<bool> HandleTrackRepeatToggleCommand(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (!zoneResult.IsSuccess || zoneResult.Value == null)
        {
            return false;
        }

        var result = await zoneResult.Value.ToggleTrackRepeatAsync();
        LogKnxCommandHandled("TRACK_REPEAT_TOGGLE", zoneIndex, result.IsSuccess);
        return result.IsSuccess;
    }

    // Playlist Commands
    [CommandId("PLAYLIST")]
    public async Task<bool> HandlePlaylistCommand(int zoneIndex, int playlistIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (!zoneResult.IsSuccess || zoneResult.Value == null)
        {
            return false;
        }

        var result = await zoneResult.Value.SetPlaylistAsync(playlistIndex);
        LogKnxCommandHandled("PLAYLIST", zoneIndex, result.IsSuccess);
        return result.IsSuccess;
    }

    [CommandId("PLAYLIST_REPEAT")]
    public async Task<bool> HandlePlaylistRepeatCommand(int zoneIndex, bool repeat)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (!zoneResult.IsSuccess || zoneResult.Value == null)
        {
            return false;
        }

        var result = await zoneResult.Value.SetPlaylistRepeatAsync(repeat);
        LogKnxCommandHandled("PLAYLIST_REPEAT", zoneIndex, result.IsSuccess);
        return result.IsSuccess;
    }

    [CommandId("PLAYLIST_REPEAT_TOGGLE")]
    public async Task<bool> HandlePlaylistRepeatToggleCommand(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (!zoneResult.IsSuccess || zoneResult.Value == null)
        {
            return false;
        }

        var result = await zoneResult.Value.TogglePlaylistRepeatAsync();
        LogKnxCommandHandled("PLAYLIST_REPEAT_TOGGLE", zoneIndex, result.IsSuccess);
        return result.IsSuccess;
    }

    // Client Commands
    [CommandId("CLIENT_VOLUME")]
    public async Task<bool> HandleClientVolumeCommand(int clientIndex, int volume)
    {
        var clientResult = await _clientManager.GetClientAsync(clientIndex);
        if (!clientResult.IsSuccess || clientResult.Value == null)
        {
            return false;
        }

        var result = await clientResult.Value.SetVolumeAsync(volume);
        LogKnxCommandHandled("CLIENT_VOLUME", clientIndex, result.IsSuccess);
        return result.IsSuccess;
    }

    [CommandId("CLIENT_MUTE")]
    public async Task<bool> HandleClientMuteCommand(int clientIndex, bool mute)
    {
        var clientResult = await _clientManager.GetClientAsync(clientIndex);
        if (!clientResult.IsSuccess || clientResult.Value == null)
        {
            return false;
        }

        var result = await clientResult.Value.SetMuteAsync(mute);
        LogKnxCommandHandled("CLIENT_MUTE", clientIndex, result.IsSuccess);
        return result.IsSuccess;
    }

    [CommandId("CLIENT_ZONE")]
    public async Task<bool> HandleClientZoneCommand(int clientIndex, int zoneIndex)
    {
        var clientResult = await _clientManager.GetClientAsync(clientIndex);
        if (!clientResult.IsSuccess || clientResult.Value == null)
        {
            return false;
        }

        var result = await clientResult.Value.AssignToZoneAsync(zoneIndex);
        LogKnxCommandHandled("CLIENT_ZONE", clientIndex, result.IsSuccess);
        return result.IsSuccess;
    }

    [LoggerMessage(EventId = 15137, Level = LogLevel.Information, Message = "KNX command receiver started")]
    private partial void LogKnxCommandReceiverStarted();

    [LoggerMessage(EventId = 15138, Level = LogLevel.Warning, Message = "KNX not enabled, command receiver disabled")]
    private partial void LogKnxCommandReceiverDisabled();

    [LoggerMessage(EventId = 15139, Level = LogLevel.Debug, Message = "KNX command {CommandId} handled for entity {EntityIndex}: {Success}")]
    private partial void LogKnxCommandHandled(string CommandId, int EntityIndex, bool Success);
}
