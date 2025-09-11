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
public class ClientService : IClientService
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
        _logger.LogInformation("Getting clients count");

        var clientStates = _clientStateStore.GetAllClientStates();
        return Task.FromResult(Result<int>.Success(clientStates.Count));
    }

    /// <summary>
    /// Gets all clients.
    /// </summary>
    public Task<Result<List<ClientState>>> GetAllClientsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all clients");

        var clientStates = _clientStateStore.GetAllClientStates();
        var clients = clientStates.Values.ToList();
        return Task.FromResult(Result<List<ClientState>>.Success(clients));
    }

    /// <summary>
    /// Gets a specific client by index.
    /// </summary>
    public Task<Result<ClientState>> GetClientAsync(int clientIndex, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting client {ClientIndex}", clientIndex);

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
        _logger.LogInformation("Assigning client {ClientIndex} to zone {ZoneIndex}", clientIndex, zoneIndex);

        return await _clientManager.AssignClientToZoneAsync(clientIndex, zoneIndex);
    }

    /// <summary>
    /// Sets the volume for a specific client.
    /// </summary>
    [CommandId("CLIENT_VOLUME")]
    public async Task<Result> SetVolumeAsync(int clientIndex, int volume, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting client {ClientIndex} volume to {Volume}", clientIndex, volume);

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
        _logger.LogInformation("Increasing client {ClientIndex} volume by {Step}", clientIndex, step);

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
        _logger.LogInformation("Decreasing client {ClientIndex} volume by {Step}", clientIndex, step);

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
        _logger.LogInformation("Setting client {ClientIndex} mute to {Muted}", clientIndex, muted);

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
        _logger.LogInformation("Toggling mute for client {ClientIndex}", clientIndex);

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
        _logger.LogInformation("Setting client {ClientIndex} latency to {LatencyMs}ms", clientIndex, latencyMs);

        // TODO: Implement actual client latency control logic
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Sets the name for a specific client.
    /// </summary>
    [CommandId("CLIENT_NAME")]
    public async Task<Result> SetNameAsync(int clientIndex, string name, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting client {ClientIndex} name to {Name}", clientIndex, name);

        var client = _clientStateStore.GetClientState(clientIndex);
        if (client == null)
        {
            return Result.Failure($"Client {clientIndex} not found");
        }

        return await _snapcastService.SetClientNameAsync(client.SnapcastId, name, cancellationToken);
    }
}
