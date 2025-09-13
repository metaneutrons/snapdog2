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

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SnapDog2.Api.Hubs;
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
    private readonly IHubContext<SnapDogHub> _hubContext;
    private readonly ILogger<ClientService> _logger;

    public ClientService(
        IClientStateStore clientStateStore,
        ISnapcastService snapcastService,
        IClientManager clientManager,
        IHubContext<SnapDogHub> hubContext,
        ILogger<ClientService> logger)
    {
        _clientStateStore = clientStateStore;
        _snapcastService = snapcastService;
        _clientManager = clientManager;
        _hubContext = hubContext;
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

        var clientResult = await _clientManager.GetClientAsync(clientIndex);
        if (clientResult.IsFailure || clientResult.Value == null)
        {
            return Result.Failure($"Client {clientIndex} not found");
        }

        var result = await clientResult.Value.SetVolumeAsync(volume);
        _logger.LogDebug("ClientManager.SetVolumeAsync result: {IsSuccess}, {ErrorMessage}", result.IsSuccess, result.ErrorMessage);

        if (result.IsSuccess)
        {
            try
            {
                _logger.LogDebug("Sending ClientVolumeChanged SignalR event for client {ClientIndex}, volume {Volume}", clientIndex, volume);
                await _hubContext.Clients.All.SendAsync("ClientVolumeChanged", clientIndex, volume, cancellationToken);
                _logger.LogDebug("ClientVolumeChanged SignalR event sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send ClientVolumeChanged SignalR event for client {ClientIndex}", clientIndex);
            }
        }

        return result;
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

        var clientResult = await _clientManager.GetClientAsync(clientIndex);
        if (clientResult.IsFailure || clientResult.Value == null)
        {
            return Result.Failure($"Client {clientIndex} not found");
        }

        var result = await clientResult.Value.SetMuteAsync(muted);
        _logger.LogDebug("ClientManager.SetMuteAsync result: {IsSuccess}, {ErrorMessage}", result.IsSuccess, result.ErrorMessage);

        if (result.IsSuccess)
        {
            try
            {
                _logger.LogDebug("Sending ClientMuteChanged SignalR event for client {ClientIndex}, muted {Muted}", clientIndex, muted);
                await _hubContext.Clients.All.SendAsync("ClientMuteChanged", clientIndex, muted, cancellationToken);
                _logger.LogDebug("ClientMuteChanged SignalR event sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send ClientMuteChanged SignalR event for client {ClientIndex}", clientIndex);
            }
        }

        return result;
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
    public async Task<Result> SetLatencyAsync(int clientIndex, int latencyMs, CancellationToken cancellationToken = default)
    {
        LogSettingLatency(clientIndex, latencyMs);

        var clientResult = await _clientManager.GetClientAsync(clientIndex);
        if (clientResult.IsFailure || clientResult.Value == null)
        {
            return Result.Failure($"Client {clientIndex} not found");
        }

        var result = await clientResult.Value.SetLatencyAsync(latencyMs);
        _logger.LogDebug("ClientManager.SetLatencyAsync result: {IsSuccess}, {ErrorMessage}", result.IsSuccess, result.ErrorMessage);

        if (result.IsSuccess)
        {
            try
            {
                _logger.LogDebug("Sending ClientLatencyChanged SignalR event for client {ClientIndex}, latency {LatencyMs}ms", clientIndex, latencyMs);
                await _hubContext.Clients.All.SendAsync("ClientLatencyChanged", clientIndex, latencyMs, cancellationToken);
                _logger.LogDebug("ClientLatencyChanged SignalR event sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send ClientLatencyChanged SignalR event for client {ClientIndex}", clientIndex);
            }
        }

        return result;
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

    [LoggerMessage(EventId = 10039, Level = LogLevel.Information, Message = "Getting clients count")]
    private partial void LogGettingClientsCount();

    [LoggerMessage(EventId = 10040, Level = LogLevel.Information, Message = "Getting all clients")]
    private partial void LogGettingAllClients();

    [LoggerMessage(EventId = 10041, Level = LogLevel.Information, Message = "Getting client {ClientIndex}")]
    private partial void LogGettingClient(int ClientIndex);

    [LoggerMessage(EventId = 10042, Level = LogLevel.Information, Message = "Assigning client {ClientIndex} to zone {ZoneIndex}")]
    private partial void LogAssigningClient(int ClientIndex, int ZoneIndex);

    [LoggerMessage(EventId = 10043, Level = LogLevel.Information, Message = "Setting client {ClientIndex} volume to {Volume}")]
    private partial void LogSettingVolume(int ClientIndex, int Volume);

    [LoggerMessage(EventId = 10044, Level = LogLevel.Information, Message = "Increasing client {ClientIndex} volume by {Step}")]
    private partial void LogIncreasingVolume(int ClientIndex, int Step);

    [LoggerMessage(EventId = 10045, Level = LogLevel.Information, Message = "Decreasing client {ClientIndex} volume by {Step}")]
    private partial void LogDecreasingVolume(int ClientIndex, int Step);

    [LoggerMessage(EventId = 10046, Level = LogLevel.Information, Message = "Setting client {ClientIndex} mute to {Muted}")]
    private partial void LogSettingMute(int ClientIndex, bool Muted);

    [LoggerMessage(EventId = 10047, Level = LogLevel.Information, Message = "Toggling mute for client {ClientIndex}")]
    private partial void LogTogglingMute(int ClientIndex);

    [LoggerMessage(EventId = 10048, Level = LogLevel.Information, Message = "Setting client {ClientIndex} latency to {LatencyMs}ms")]
    private partial void LogSettingLatency(int ClientIndex, int LatencyMs);

    [LoggerMessage(EventId = 10049, Level = LogLevel.Information, Message = "Setting client {ClientIndex} name to {Name}")]
    private partial void LogSettingName(int ClientIndex, string Name);
}
