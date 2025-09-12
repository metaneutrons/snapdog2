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
namespace SnapDog2.Domain.Services;

using Microsoft.Extensions.Logging;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Models;

/// <summary>
/// Implementation of IClientService for client operations.
/// </summary>
public partial class ClientService : IClientService
{
    private readonly IClientStateStore _clientStateStore;
    private readonly ISnapcastService _snapcastService;
    private readonly IClientManager _clientManager;
    private readonly ILogger<ClientService> _logger;

    public ClientService(
        IClientStateStore clientStateStore,
        ISnapcastService snapcastService,
        IClientManager clientManager,
        ILogger<ClientService> logger)
    {
        _clientStateStore = clientStateStore;
        _snapcastService = snapcastService;
        _clientManager = clientManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets the total count of clients.
    /// </summary>
    public Task<Result<int>> GetClientsCountAsync(CancellationToken cancellationToken = default)
    {
        LogGettingClientsCount();

        var clientStates = _clientStateStore.GetAllClientStates();
        return Task.FromResult(Result<int>.Success(clientStates.Count));
    }

    /// <summary>
    /// Gets all clients.
    /// </summary>
    public Task<Result<List<ClientState>>> GetAllClientsAsync(CancellationToken cancellationToken = default)
    {
        LogGettingAllClients();

        var clientStates = _clientStateStore.GetAllClientStates();
        var clients = clientStates.Values.ToList();
        return Task.FromResult(Result<List<ClientState>>.Success(clients));
    }

    /// <summary>
    /// Gets a specific client by index.
    /// </summary>
    public Task<Result<ClientState>> GetClientAsync(int clientIndex, CancellationToken cancellationToken = default)
    {
        LogGettingClient(clientIndex);

        var client = _clientStateStore.GetClientState(clientIndex);
        if (client == null)
        {
            return Task.FromResult(Result<ClientState>.Failure($"Client {clientIndex} not found"));
        }

        return Task.FromResult(Result<ClientState>.Success(client));
    }

    /// <summary>
    /// Assigns a client to a specific zone.
    /// </summary>
    [CommandId("CLIENT_ZONE")]
    public async Task<Result> AssignToZoneAsync(int clientIndex, int zoneIndex, CancellationToken cancellationToken = default)
    {
        LogAssigningClient(clientIndex, zoneIndex);

        return await _clientManager.AssignClientToZoneAsync(clientIndex, zoneIndex);
    }

    /// <summary>
    /// Sets the volume for a specific client.
    /// </summary>
    [CommandId("CLIENT_VOLUME")]
    public async Task<Result> SetVolumeAsync(int clientIndex, int volume, CancellationToken cancellationToken = default)
    {
        LogSettingVolume(clientIndex, volume);

        var client = _clientStateStore.GetClientState(clientIndex);
        if (client == null)
        {
            return Result.Failure($"Client {clientIndex} not found");
        }

        return await _snapcastService.SetClientVolumeAsync(client.SnapcastId, volume, cancellationToken);
    }

    /// <summary>
    /// Increases client volume by specified step.
    /// </summary>
    [CommandId("CLIENT_VOLUME_UP")]
    public async Task<Result> VolumeUpAsync(int clientIndex, int step, CancellationToken cancellationToken = default)
    {
        LogIncreasingVolume(clientIndex, step);

        var client = _clientStateStore.GetClientState(clientIndex);
        if (client == null)
        {
            return Result.Failure($"Client {clientIndex} not found");
        }

        var newVolume = Math.Min(100, client.Volume + step);
        return await SetVolumeAsync(clientIndex, newVolume, cancellationToken);
    }

    /// <summary>
    /// Decreases client volume by specified step.
    /// </summary>
    [CommandId("CLIENT_VOLUME_DOWN")]
    public async Task<Result> VolumeDownAsync(int clientIndex, int step, CancellationToken cancellationToken = default)
    {
        LogDecreasingVolume(clientIndex, step);

        var client = _clientStateStore.GetClientState(clientIndex);
        if (client == null)
        {
            return Result.Failure($"Client {clientIndex} not found");
        }

        var newVolume = Math.Max(0, client.Volume - step);
        return await SetVolumeAsync(clientIndex, newVolume, cancellationToken);
    }

    /// <summary>
    /// Sets the mute state for a specific client.
    /// </summary>
    [CommandId("CLIENT_MUTE")]
    public async Task<Result> SetMuteAsync(int clientIndex, bool muted, CancellationToken cancellationToken = default)
    {
        LogSettingMute(clientIndex, muted);

        var client = _clientStateStore.GetClientState(clientIndex);
        if (client == null)
        {
            return Result.Failure($"Client {clientIndex} not found");
        }

        return await _snapcastService.SetClientMuteAsync(client.SnapcastId, muted, cancellationToken);
    }

    /// <summary>
    /// Toggles the mute state for a specific client.
    /// </summary>
    [CommandId("CLIENT_MUTE_TOGGLE")]
    public async Task<Result> ToggleMuteAsync(int clientIndex, CancellationToken cancellationToken = default)
    {
        LogTogglingMute(clientIndex);

        var client = _clientStateStore.GetClientState(clientIndex);
        if (client == null)
        {
            return Result.Failure($"Client {clientIndex} not found");
        }

        return await SetMuteAsync(clientIndex, !client.Mute, cancellationToken);
    }

    /// <summary>
    /// Sets the latency compensation for a specific client.
    /// </summary>
    [CommandId("CLIENT_LATENCY")]
    public Task<Result> SetLatencyAsync(int clientIndex, int latencyMs, CancellationToken cancellationToken = default)
    {
        LogSettingLatency(clientIndex, latencyMs);

        // TODO: Implement actual client latency control logic
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Sets the name for a specific client.
    /// </summary>
    [CommandId("CLIENT_NAME")]
    public async Task<Result> SetNameAsync(int clientIndex, string name, CancellationToken cancellationToken = default)
    {
        LogSettingName(clientIndex, name);

        var client = _clientStateStore.GetClientState(clientIndex);
        if (client == null)
        {
            return Result.Failure($"Client {clientIndex} not found");
        }

        return await _snapcastService.SetClientNameAsync(client.SnapcastId, name, cancellationToken);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Getting clients count")]
    private partial void LogGettingClientsCount();

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Getting all clients")]
    private partial void LogGettingAllClients();

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Getting client {ClientIndex}")]
    private partial void LogGettingClient(int ClientIndex);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Assigning client {ClientIndex} to zone {ZoneIndex}")]
    private partial void LogAssigningClient(int ClientIndex, int ZoneIndex);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Setting client {ClientIndex} volume to {Volume}")]
    private partial void LogSettingVolume(int ClientIndex, int Volume);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Increasing client {ClientIndex} volume by {Step}")]
    private partial void LogIncreasingVolume(int ClientIndex, int Step);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Decreasing client {ClientIndex} volume by {Step}")]
    private partial void LogDecreasingVolume(int ClientIndex, int Step);

    [LoggerMessage(EventId = 8, Level = LogLevel.Information, Message = "Setting client {ClientIndex} mute to {Muted}")]
    private partial void LogSettingMute(int ClientIndex, bool Muted);

    [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Toggling mute for client {ClientIndex}")]
    private partial void LogTogglingMute(int ClientIndex);

    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "Setting client {ClientIndex} latency to {LatencyMs}ms")]
    private partial void LogSettingLatency(int ClientIndex, int LatencyMs);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Setting client {ClientIndex} name to {Name}")]
    private partial void LogSettingName(int ClientIndex, string Name);
}
