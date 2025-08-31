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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Clients.Notifications;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Models;

/// <summary>
/// Production implementation of IClientManager with Snapcast integration.
/// Uses SnapcastStateRepository.GetClientByIndex for clean separation of concerns.
/// Maintains internal ClientState for each configured client.
/// </summary>
public partial class ClientManager : IClientManager
{
    private readonly ILogger<ClientManager> _logger;
    private readonly ISnapcastStateRepository _snapcastStateRepository;
    private readonly ISnapcastService _snapcastService;
    private readonly IMediator _mediator;
    private readonly IClientStateStore _clientStateStore;
    private readonly List<ClientConfig> _clientConfigs;
    private readonly SnapDogConfiguration _configuration;
    private readonly Dictionary<int, ClientState> _clientStates = new();
    private readonly Dictionary<int, SemaphoreSlim> _clientStateLocks = new();

    [LoggerMessage(
        EventId = 6300,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Getting client {ClientIndex}"
    )]
    private partial void LogGettingClient(int clientIndex);

    [LoggerMessage(
        EventId = 6301,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Client {ClientIndex} not found"
    )]
    private partial void LogClientNotFound(int clientIndex);

    [LoggerMessage(
        EventId = 6302,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Getting all clients"
    )]
    private partial void LogGettingAllClients();

    [LoggerMessage(
        EventId = 6303,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Getting clients for zone {ZoneIndex}"
    )]
    private partial void LogGettingClientsByZone(int zoneIndex);

    [LoggerMessage(
        EventId = 6304,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Assigning client {ClientIndex} to zone {ZoneIndex}"
    )]
    private partial void LogAssigningClientToZone(int clientIndex, int zoneIndex);

    [LoggerMessage(
        EventId = 6305,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Initialized ClientManager with {ClientCount} configured clients"
    )]
    private partial void LogInitialized(int clientCount);

    [LoggerMessage(
        EventId = 6306,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Getting client by Snapcast ID {SnapcastClientId}"
    )]
    private partial void LogGettingClientBySnapcastId(string snapcastClientId);

    [LoggerMessage(
        EventId = 6307,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Snapcast client {SnapcastClientId} not found"
    )]
    private partial void LogSnapcastClientNotFound(string snapcastClientId);

    [LoggerMessage(
        EventId = 6308,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "MAC address not found for Snapcast client {SnapcastClientId}"
    )]
    private partial void LogMacAddressNotFound(string snapcastClientId);

    [LoggerMessage(
        EventId = 6309,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Client config not found for MAC address {MacAddress}"
    )]
    private partial void LogClientConfigNotFoundByMac(string macAddress);

    [LoggerMessage(
        EventId = 6310,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Found client {SnapcastClientId} with MAC {MacAddress} mapped to client index {ClientIndex}"
    )]
    private partial void LogClientFoundByMac(string snapcastClientId, string macAddress, int clientIndex);

    [LoggerMessage(
        EventId = 6311,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Error getting client by Snapcast ID {SnapcastClientId}"
    )]
    private partial void LogGetClientBySnapcastIdError(string snapcastClientId, Exception ex);

    [LoggerMessage(
        EventId = 6312,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Found client {ClientIndex} with Snapcast ID: {SnapcastClientId}"
    )]
    private partial void LogFoundClientWithSnapcastId(int ClientIndex, string SnapcastClientId);

    [LoggerMessage(
        EventId = 6313,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Target zone {ZoneIndex} maps to stream: {StreamId}"
    )]
    private partial void LogTargetZoneMapsToStream(int ZoneIndex, string StreamId);

    [LoggerMessage(
        EventId = 6314,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Using group {GroupId} for zone {ZoneIndex}"
    )]
    private partial void LogUsingGroupForZone(string GroupId, int ZoneIndex);

    [LoggerMessage(
        EventId = 6315,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to move client {ClientIndex} to group {GroupId}: {Error}"
    )]
    private partial void LogFailedToMoveClientToGroup(string ClientIndex, string GroupId, string? Error);

    [LoggerMessage(
        EventId = 6316,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Refreshing server state after zone assignment"
    )]
    private partial void LogRefreshingServerStateAfterZoneAssignment();

    [LoggerMessage(
        EventId = 6317,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Server state refreshed successfully"
    )]
    private partial void LogServerStateRefreshedSuccessfully();

    [LoggerMessage(
        EventId = 6318,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to refresh server state after zone assignment: {Error}"
    )]
    private partial void LogFailedToRefreshServerState(string? Error);

    [LoggerMessage(
        EventId = 6319,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Successfully assigned client {ClientIndex} - ({ClientId}) to zone {ZoneIndex} (group {GroupId})"
    )]
    private partial void LogSuccessfullyAssignedClientToZone(
        int ClientIndex,
        string ClientId,
        int ZoneIndex,
        string GroupId
    );

    [LoggerMessage(
        EventId = 6320,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Error assigning client {ClientIndex} to zone {ZoneIndex}"
    )]
    private partial void LogErrorAssigningClientToZone(Exception ex, int ClientIndex, int ZoneIndex);

    [LoggerMessage(
        EventId = 6321,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Found existing group {GroupId} for stream {StreamId}"
    )]
    private partial void LogFoundExistingGroupForStream(string GroupId, string StreamId);

    [LoggerMessage(
        EventId = 6322,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Assigning group {GroupId} to stream {StreamId}"
    )]
    private partial void LogAssigningGroupToStream(string GroupId, string StreamId);

    [LoggerMessage(
        EventId = 6323,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Successfully assigned group {GroupId} to stream {StreamId}"
    )]
    private partial void LogSuccessfullyAssignedGroupToStream(string GroupId, string StreamId);

    [LoggerMessage(
        EventId = 6324,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to assign group {GroupId} to stream {StreamId}: {Error}"
    )]
    private partial void LogFailedToAssignGroupToStream(string GroupId, string StreamId, string? Error);

    [LoggerMessage(
        EventId = 6325,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "No available groups found for stream {StreamId}"
    )]
    private partial void LogNoAvailableGroupsFoundForStream(string StreamId);

    [LoggerMessage(
        EventId = 6326,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Error finding or creating group for stream {StreamId}"
    )]
    private partial void LogErrorFindingOrCreatingGroupForStream(Exception ex, string StreamId);

    public ClientManager(
        ILogger<ClientManager> logger,
        ISnapcastStateRepository snapcastStateRepository,
        ISnapcastService snapcastService,
        IMediator mediator,
        IClientStateStore clientStateStore,
        SnapDogConfiguration configuration
    )
    {
        this._logger = logger;
        this._snapcastStateRepository = snapcastStateRepository;
        this._snapcastService = snapcastService;
        this._mediator = mediator;
        this._clientStateStore = clientStateStore;
        this._clientConfigs = configuration.Clients;
        this._configuration = configuration;

        // Initialize state locks and default states for each configured client
        for (int i = 0; i < this._clientConfigs.Count; i++)
        {
            var clientIndex = i + 1; // 1-based indexing
            var clientConfig = this._clientConfigs[i];

            this._clientStateLocks[clientIndex] = new SemaphoreSlim(1, 1);

            // Initialize default client state
            var defaultState = new ClientState
            {
                Id = clientIndex,
                Name = clientConfig.Name,
                Mac = clientConfig.Mac ?? "00:00:00:00:00:00",
                SnapcastId = string.Empty, // Will be updated when Snapcast client is discovered
                Volume = 50, // Default volume
                Mute = false,
                Connected = false,
                ZoneIndex = clientConfig.DefaultZone,
                LatencyMs = 0,
                ConfiguredSnapcastName = string.Empty,
                LastSeenUtc = DateTime.UtcNow,
                HostIpAddress = string.Empty,
                HostName = string.Empty,
                HostOs = string.Empty,
                HostArch = string.Empty,
                TimestampUtc = DateTime.UtcNow
            };

            this._clientStates[clientIndex] = defaultState;
            this._clientStateStore.InitializeClientState(clientIndex, defaultState);
        }

        this.LogInitialized(this._clientConfigs.Count);
    }

    public async Task<Result<IClient>> GetClientAsync(int clientIndex)
    {
        this.LogGettingClient(clientIndex);

        await Task.Delay(1); // Maintain async signature

        // Validate client Index range
        if (clientIndex < 1 || clientIndex > this._clientConfigs.Count)
        {
            this.LogClientNotFound(clientIndex);
            return Result<IClient>.Failure(
                $"Client {clientIndex} is out of range. Valid range: 1-{this._clientConfigs.Count}"
            );
        }

        // Get client from Snapcast using the repository's lookup logic
        var snapcastClient = this._snapcastStateRepository.GetClientByIndex(clientIndex);
        if (snapcastClient == null)
        {
            this.LogClientNotFound(clientIndex);
            return Result<IClient>.Failure($"Client {clientIndex} not found in Snapcast");
        }

        // Convert Snapcast client to domain client
        var client = new SnapDogClient(
            clientIndex,
            snapcastClient.Value,
            this._clientConfigs[clientIndex - 1],
            this._snapcastService,
            this._snapcastStateRepository,
            this._mediator,
            this
        );
        return Result<IClient>.Success(client);
    }

    public async Task<Result<ClientState>> GetClientStateAsync(int clientIndex)
    {
        this.LogGettingClient(clientIndex);

        var clientResult = await this.GetClientAsync(clientIndex);
        if (!clientResult.IsSuccess)
        {
            return Result<ClientState>.Failure(clientResult.ErrorMessage!);
        }

        var client = clientResult.Value!;
        var snapDogClient = (SnapDogClient)client;
        var state = new ClientState
        {
            Id = clientIndex,
            SnapcastId = snapDogClient._snapcastClient.Id,
            Name = client.Name,
            Mac = snapDogClient.MacAddress,
            Connected = snapDogClient.Connected,
            Volume = snapDogClient.Volume,
            Mute = snapDogClient.Muted,
            LatencyMs = snapDogClient.LatencyMs,
            ZoneIndex = snapDogClient.ZoneIndex,
            ConfiguredSnapcastName = snapDogClient._snapcastClient.Config.Name,
            LastSeenUtc = snapDogClient.LastSeenUtc,
            HostIpAddress = snapDogClient.IpAddress,
            HostName = snapDogClient._snapcastClient.Host.Name,
            HostOs = snapDogClient._snapcastClient.Host.Os,
            HostArch = snapDogClient._snapcastClient.Host.Arch,
            SnapClientVersion = null, // Not available in SnapcastClient model
            SnapClientProtocolVersion = null, // Not available in SnapcastClient model
        };

        return Result<ClientState>.Success(state);
    }

    public async Task<Result<List<ClientState>>> GetAllClientsAsync()
    {
        this.LogGettingAllClients();

        var clientStates = new List<ClientState>();

        for (int clientIndex = 1; clientIndex <= this._clientConfigs.Count; clientIndex++)
        {
            var stateResult = await this.GetClientStateAsync(clientIndex);
            if (stateResult.IsSuccess)
            {
                clientStates.Add(stateResult.Value!);
            }
            // Continue even if some clients are not found/connected
        }

        return Result<List<ClientState>>.Success(clientStates);
    }

    public async Task<Result<List<ClientState>>> GetClientsByZoneAsync(int zoneIndex)
    {
        this.LogGettingClientsByZone(zoneIndex);

        var allClientsResult = await this.GetAllClientsAsync();
        if (!allClientsResult.IsSuccess)
        {
            return Result<List<ClientState>>.Failure(allClientsResult.ErrorMessage!);
        }

        var zoneClients = allClientsResult.Value!.Where(c => c.ZoneIndex == zoneIndex).ToList();

        return Result<List<ClientState>>.Success(zoneClients);
    }

    public async Task<Result> AssignClientToZoneAsync(int clientIndex, int zoneIndex)
    {
        this.LogAssigningClientToZone(clientIndex, zoneIndex);

        try
        {
            // Validate inputs
            if (clientIndex < 1 || clientIndex > this._clientConfigs.Count)
            {
                return Result.Failure($"Invalid client index: {clientIndex}");
            }

            // Get the client from Snapcast
            var snapcastClient = this._snapcastStateRepository.GetClientByIndex(clientIndex);
            if (snapcastClient == null)
            {
                return Result.Failure($"Client {clientIndex} not found in Snapcast");
            }

            var snapcastClientId = snapcastClient.Value.Id;
            if (string.IsNullOrEmpty(snapcastClientId))
            {
                return Result.Failure($"Client {clientIndex} has no valid ID");
            }

            this.LogFoundClientWithSnapcastId(clientIndex, snapcastClientId);

            // Get target zone's stream ID
            var zoneConfigs = this._configuration.Zones;
            if (zoneIndex < 1 || zoneIndex > zoneConfigs.Count)
            {
                return Result.Failure($"Invalid zone index: {zoneIndex}");
            }

            var targetZoneConfig = zoneConfigs[zoneIndex - 1];
            var targetStreamId = ExtractStreamIdFromSink(targetZoneConfig.Sink);

            this.LogTargetZoneMapsToStream(zoneIndex, targetStreamId);

            // Find or create group for target zone
            var targetGroupId = await this.FindOrCreateGroupForStreamAsync(targetStreamId);
            if (targetGroupId == null)
            {
                return Result.Failure($"Failed to find or create group for zone {zoneIndex}");
            }

            this.LogUsingGroupForZone(targetGroupId, zoneIndex);

            // Move client to target group
            var result = await this._snapcastService.SetClientGroupAsync(snapcastClientId, targetGroupId);
            if (result.IsFailure)
            {
                this.LogFailedToMoveClientToGroup(snapcastClientId, targetGroupId, result.ErrorMessage);
                return Result.Failure($"Failed to move client to zone {zoneIndex}: {result.ErrorMessage}");
            }

            // Refresh server state to update our local repository
            this.LogRefreshingServerStateAfterZoneAssignment();
            var serverStatusResult = await this._snapcastService.GetServerStatusAsync();
            if (serverStatusResult.IsSuccess)
            {
                // The GetServerStatusAsync should trigger state repository updates via event handlers
                this.LogServerStateRefreshedSuccessfully();
            }
            else
            {
                this.LogFailedToRefreshServerState(serverStatusResult.ErrorMessage);
            }

            this.LogSuccessfullyAssignedClientToZone(clientIndex, snapcastClientId, zoneIndex, targetGroupId);

            // Update internal client state
            if (this._clientStateLocks.TryGetValue(clientIndex, out var stateLock))
            {
                await stateLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    this._clientStates[clientIndex] = this._clientStates[clientIndex] with { ZoneIndex = zoneIndex };
                    this.PublishClientStateChangedAsync(clientIndex);
                }
                finally
                {
                    stateLock.Release();
                }
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogErrorAssigningClientToZone(ex, clientIndex, zoneIndex);
            return Result.Failure($"Error assigning client {clientIndex} to zone {zoneIndex}: {ex.Message}");
        }
    }

    public Task<IClient?> GetClientBySnapcastIdAsync(string snapcastClientId)
    {
        this.LogGettingClientBySnapcastId(snapcastClientId);

        try
        {
            // Get all Snapcast clients (synchronous call)
            var allSnapcastClients = this._snapcastStateRepository.GetAllClients();

            // Find the Snapcast client by ID
            var snapcastClient = allSnapcastClients.FirstOrDefault(c => c.Id == snapcastClientId);
            if (string.IsNullOrEmpty(snapcastClient.Id)) // Check if default struct was returned
            {
                this.LogSnapcastClientNotFound(snapcastClientId);
                return Task.FromResult<IClient?>(null);
            }

            // Get the MAC address from the Snapcast client
            var macAddress = snapcastClient.Host.Mac;
            if (string.IsNullOrEmpty(macAddress))
            {
                this.LogMacAddressNotFound(snapcastClientId);
                return Task.FromResult<IClient?>(null);
            }

            // Find the corresponding client index by MAC address in our configuration
            var clientIndex =
                this._clientConfigs.FindIndex(config =>
                    string.Equals(config.Mac, macAddress, StringComparison.OrdinalIgnoreCase)
                ) + 1; // 1-based index

            if (clientIndex == 0) // Not found (-1 + 1 = 0)
            {
                this.LogClientConfigNotFoundByMac(macAddress);
                return Task.FromResult<IClient?>(null);
            }

            // Create and return the IClient wrapper
            var client = new SnapDogClient(
                clientIndex,
                snapcastClient,
                this._clientConfigs[clientIndex - 1],
                this._snapcastService,
                this._snapcastStateRepository,
                this._mediator,
                this
            );

            this.LogClientFoundByMac(snapcastClientId, macAddress, clientIndex);
            return Task.FromResult<IClient?>(client);
        }
        catch (Exception ex)
        {
            this.LogGetClientBySnapcastIdError(snapcastClientId, ex);
            return Task.FromResult<IClient?>(null);
        }
    }

    private async Task<string?> FindOrCreateGroupForStreamAsync(string streamId)
    {
        try
        {
            // Find existing group with this stream
            var allGroups = this._snapcastStateRepository.GetAllGroups();
            var existingGroup = allGroups.FirstOrDefault(g => g.StreamId == streamId);

            if (existingGroup.Id != null)
            {
                this.LogFoundExistingGroupForStream(existingGroup.Id, streamId);
                return existingGroup.Id;
            }

            // No existing group for this stream, find a group to assign to this stream
            // Prefer groups with no stream assigned (stream == null)
            var availableGroup = allGroups.FirstOrDefault(g => string.IsNullOrEmpty(g.StreamId));

            // If no unassigned group, use any group (we'll reassign it)
            if (availableGroup.Id == null)
            {
                availableGroup = allGroups.FirstOrDefault();
            }

            if (availableGroup.Id != null)
            {
                this.LogAssigningGroupToStream(availableGroup.Id, streamId);

                // Set the group to use our target stream
                var result = await this._snapcastService.SetGroupStreamAsync(availableGroup.Id, streamId);
                if (result.IsSuccess)
                {
                    this.LogSuccessfullyAssignedGroupToStream(availableGroup.Id, streamId);
                    return availableGroup.Id;
                }
                else
                {
                    this.LogFailedToAssignGroupToStream(availableGroup.Id, streamId, result.ErrorMessage);
                }
            }

            this.LogNoAvailableGroupsFoundForStream(streamId);
            return null;
        }
        catch (Exception ex)
        {
            this.LogErrorFindingOrCreatingGroupForStream(ex, streamId);
            return null;
        }
    }

    public async Task<Result<ClientState>> GetClientAsync(
        int clientIndex,
        CancellationToken cancellationToken = default
    )
    {
        var clientResult = await this.GetClientStateAsync(clientIndex);
        return clientResult;
    }

    public async Task<Result<List<ClientState>>> GetAllClientsAsync(CancellationToken cancellationToken = default)
    {
        return await this.GetAllClientsAsync();
    }

    public async Task<Result<List<ClientState>>> GetClientsByZoneAsync(
        int zoneIndex,
        CancellationToken cancellationToken = default
    )
    {
        return await this.GetClientsByZoneAsync(zoneIndex);
    }

    private static string ExtractStreamIdFromSink(string sink)
    {
        // Convert "/snapsinks/zone1" -> "Zone1", "/snapsinks/zone2" -> "Zone2"
        var fileName = Path.GetFileName(sink);
        if (fileName.StartsWith("zone", StringComparison.OrdinalIgnoreCase))
        {
            var zoneNumber = fileName.Substring(4);
            return $"Zone{zoneNumber}";
        }
        return fileName;
    }

    #region Client State Management Methods

    /// <summary>
    /// Updates client volume and publishes state change.
    /// </summary>
    public async Task<Result> SetClientVolumeAsync(int clientIndex, int volume)
    {
        if (!this._clientStateLocks.TryGetValue(clientIndex, out var stateLock))
        {
            return Result.Failure($"Client {clientIndex} not found");
        }

        await stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Get Snapcast client
            var snapcastClient = this._snapcastStateRepository.GetClientByIndex(clientIndex);
            if (snapcastClient == null)
            {
                return Result.Failure($"Client {clientIndex} not found in Snapcast");
            }

            // Call Snapcast service
            var result = await this._snapcastService.SetClientVolumeAsync(snapcastClient.Value.Id, volume);
            if (result.IsFailure)
            {
                return result;
            }

            // Update internal state
            var clampedVolume = Math.Clamp(volume, 0, 100);
            this._clientStates[clientIndex] = this._clientStates[clientIndex] with { Volume = clampedVolume };
            this.PublishClientStateChangedAsync(clientIndex);

            return Result.Success();
        }
        finally
        {
            stateLock.Release();
        }
    }

    /// <summary>
    /// Updates client mute state and publishes state change.
    /// </summary>
    public async Task<Result> SetClientMuteAsync(int clientIndex, bool mute)
    {
        if (!this._clientStateLocks.TryGetValue(clientIndex, out var stateLock))
        {
            return Result.Failure($"Client {clientIndex} not found");
        }

        await stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Get Snapcast client
            var snapcastClient = this._snapcastStateRepository.GetClientByIndex(clientIndex);
            if (snapcastClient == null)
            {
                return Result.Failure($"Client {clientIndex} not found in Snapcast");
            }

            // Call Snapcast service
            var result = await this._snapcastService.SetClientMuteAsync(snapcastClient.Value.Id, mute);
            if (result.IsFailure)
            {
                return result;
            }

            // Update internal state
            this._clientStates[clientIndex] = this._clientStates[clientIndex] with { Mute = mute };
            this.PublishClientStateChangedAsync(clientIndex);

            return Result.Success();
        }
        finally
        {
            stateLock.Release();
        }
    }

    /// <summary>
    /// Updates internal client state from Snapcast server data.
    /// </summary>
    private async Task UpdateClientStateFromSnapcastAsync(int clientIndex)
    {
        await Task.Delay(1); // Maintain async signature

        var snapcastClient = this._snapcastStateRepository.GetClientByIndex(clientIndex);
        if (snapcastClient == null)
        {
            return;
        }

        var client = snapcastClient.Value;
        var currentState = this._clientStates[clientIndex];

        // Update state from Snapcast data
        this._clientStates[clientIndex] = currentState with
        {
            SnapcastId = client.Id,
            Volume = client.Config.Volume.Percent,
            Mute = client.Config.Volume.Muted,
            Connected = client.Connected,
            LatencyMs = client.Config.Latency,
            ConfiguredSnapcastName = client.Config.Name,
            LastSeenUtc = DateTimeOffset.FromUnixTimeSeconds(client.LastSeen.Sec).UtcDateTime,
            HostIpAddress = client.Host.Ip,
            HostName = client.Host.Name,
            HostOs = client.Host.Os,
            HostArch = client.Host.Arch,
            // ZoneIndex is determined by group assignment - will be updated by zone assignment logic
            TimestampUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Publishes client state changes to the state store and notification system.
    /// Similar to ZoneManager.PublishZoneStateChangedAsync().
    /// </summary>
    private void PublishClientStateChangedAsync(int clientIndex)
    {
        if (!this._clientStates.TryGetValue(clientIndex, out var currentState))
        {
            return; // No state to publish
        }

        // Persist state to store for future requests
        this._clientStateStore.SetClientState(clientIndex, currentState);

        // Publish notification via mediator using fire-and-forget to prevent blocking
        // Use Task.Run to avoid blocking the calling thread
        _ = Task.Run(async () =>
        {
            try
            {
                var notification = new ClientStateChangedNotification
                {
                    ClientIndex = clientIndex,
                    ClientState = currentState
                };
                await this._mediator.PublishAsync(notification);
            }
            catch (ObjectDisposedException)
            {
                // Service provider has been disposed (likely during shutdown) - ignore this event
                // This is expected behavior during application shutdown
            }
            catch (Exception ex)
            {
                // Log other unexpected exceptions but don't rethrow to avoid breaking the event flow
                System.Diagnostics.Debug.WriteLine($"Error publishing client state for client {clientIndex}: {ex.Message}");
            }
        }, CancellationToken.None);
    }

    #endregion

    /// <summary>
    /// Internal implementation of IClient that wraps a Snapcast client with SnapDog2 configuration.
    /// </summary>
    private class SnapDogClient(
        int id,
        SnapcastClient.Models.SnapClient snapcastClient,
        ClientConfig config,
        ISnapcastService snapcastService,
        ISnapcastStateRepository snapcastStateRepository,
        IMediator mediator,
        IClientManager clientManager
    ) : IClient
    {
        internal readonly SnapcastClient.Models.SnapClient _snapcastClient = snapcastClient;
        private readonly ClientConfig _config = config;
        private readonly ISnapcastService _snapcastService = snapcastService;
        private readonly ISnapcastStateRepository _snapcastStateRepository = snapcastStateRepository;
        private readonly IMediator _mediator = mediator;
        private readonly IClientManager _clientManager = clientManager;

        public int Id { get; } = id;
        public string Name => this._config.Name;

        // Internal properties for ClientState mapping
        internal bool Connected => this._snapcastClient.Connected;
        internal int Volume => this._snapcastClient.Config.Volume.Percent;
        internal bool Muted => this._snapcastClient.Config.Volume.Muted;
        internal int LatencyMs => this._snapcastClient.Config.Latency;
        internal int ZoneIndex => this._config.DefaultZone;

        private int GetActualZoneFromSnapcast()
        {
            try
            {
                // Find the group this client belongs to
                var allGroups = this._snapcastStateRepository.GetAllGroups();
                var clientGroup = allGroups.FirstOrDefault(g => g.Clients.Any(c => c.Id == this._snapcastClient.Id));

                if (clientGroup.Id == null || string.IsNullOrEmpty(clientGroup.StreamId))
                {
                    // No group or no stream assigned, return default zone
                    return this._config.DefaultZone;
                }

                // Map stream ID to zone index
                // Zone1 -> 1, Zone2 -> 2, etc.
                if (
                    clientGroup.StreamId.StartsWith("Zone")
                    && int.TryParse(clientGroup.StreamId.AsSpan(4), out int zoneIndex)
                )
                {
                    return zoneIndex;
                }

                // If we can't parse the stream ID, return default zone
                return this._config.DefaultZone;
            }
            catch
            {
                // On any error, return default zone
                return this._config.DefaultZone;
            }
        }

        internal string MacAddress => this._snapcastClient.Host.Mac;
        internal string IpAddress => this._snapcastClient.Host.Ip;
        internal DateTime LastSeenUtc =>
            DateTimeOffset.FromUnixTimeSeconds(this._snapcastClient.LastSeen.Sec).UtcDateTime;

        #region Command Operations

        public async Task<Result> SetVolumeAsync(int volume)
        {
            var result = await this._snapcastService.SetClientVolumeAsync(this._snapcastClient.Id, volume);

            if (result.IsSuccess)
            {
                await this.PublishVolumeStatusAsync(volume);
            }

            return result;
        }

        public async Task<Result> SetMuteAsync(bool mute)
        {
            var result = await this._snapcastService.SetClientMuteAsync(this._snapcastClient.Id, mute);

            if (result.IsSuccess)
            {
                await this.PublishMuteStatusAsync(mute);
            }

            return result;
        }

        public async Task<Result> SetLatencyAsync(int latencyMs)
        {
            var result = await this._snapcastService.SetClientLatencyAsync(this._snapcastClient.Id, latencyMs);

            if (result.IsSuccess)
            {
                await this.PublishLatencyStatusAsync(latencyMs);
            }

            return result;
        }

        public async Task<Result> SetNameAsync(string name)
        {
            var result = await this._snapcastService.SetClientNameAsync(this._snapcastClient.Id, name);

            if (result.IsSuccess)
            {
                // Note: Name changes don't have a status notification in blueprint
                // They use ClientNameChangedNotification instead
            }

            return result;
        }

        public async Task<Result> VolumeUpAsync(int step = 5)
        {
            // Get current volume and calculate new volume
            var currentVolume = this.Volume;
            var newVolume = Math.Min(100, currentVolume + step);

            var result = await this._snapcastService.SetClientVolumeAsync(this._snapcastClient.Id, newVolume);

            if (result.IsSuccess)
            {
                await this.PublishVolumeStatusAsync(newVolume);
            }

            return result;
        }

        public async Task<Result> VolumeDownAsync(int step = 5)
        {
            // Get current volume and calculate new volume
            var currentVolume = this.Volume;
            var newVolume = Math.Max(0, currentVolume - step);

            var result = await this._snapcastService.SetClientVolumeAsync(this._snapcastClient.Id, newVolume);

            if (result.IsSuccess)
            {
                await this.PublishVolumeStatusAsync(newVolume);
            }

            return result;
        }

        public async Task<Result> ToggleMuteAsync()
        {
            // Get current mute state and toggle it
            var currentMuted = this.Muted;
            var newMuted = !currentMuted;

            var result = await this._snapcastService.SetClientMuteAsync(this._snapcastClient.Id, newMuted);

            if (result.IsSuccess)
            {
                await this.PublishMuteStatusAsync(newMuted);
            }

            return result;
        }

        public async Task<Result> AssignToZoneAsync(int zoneIndex)
        {
            // Use the ClientManager's zone assignment logic
            var result = await this._clientManager.AssignClientToZoneAsync(this.Id, zoneIndex);

            if (result.IsSuccess)
            {
                await this.PublishZoneStatusAsync(zoneIndex);
            }

            return result;
        }

        #endregion

        #region Status Publishing

        public async Task PublishVolumeStatusAsync(int volume)
        {
            try
            {
                var notification = new ClientVolumeStatusNotification(this.Id, volume);
                await this._mediator.PublishAsync(notification);
            }
            catch (ObjectDisposedException)
            {
                // Service provider has been disposed (likely during shutdown) - ignore this event
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error publishing volume status for client {this.Id}: {ex.Message}"
                );
            }
        }

        public async Task PublishMuteStatusAsync(bool muted)
        {
            try
            {
                var notification = new ClientMuteStatusNotification(this.Id, muted);
                await this._mediator.PublishAsync(notification);
            }
            catch (ObjectDisposedException)
            {
                // Service provider has been disposed (likely during shutdown) - ignore this event
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error publishing mute status for client {this.Id}: {ex.Message}");
            }
        }

        public async Task PublishLatencyStatusAsync(int latencyMs)
        {
            try
            {
                var notification = new ClientLatencyStatusNotification(this.Id, latencyMs);
                await this._mediator.PublishAsync(notification);
            }
            catch (ObjectDisposedException)
            {
                // Service provider has been disposed (likely during shutdown) - ignore this event
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error publishing latency status for client {this.Id}: {ex.Message}"
                );
            }
        }

        public async Task PublishZoneStatusAsync(int? zoneIndex)
        {
            try
            {
                var notification = new ClientZoneStatusNotification(this.Id, zoneIndex);
                await this._mediator.PublishAsync(notification);
            }
            catch (ObjectDisposedException)
            {
                // Service provider has been disposed (likely during shutdown) - ignore this event
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error publishing zone status for client {this.Id}: {ex.Message}");
            }
        }

        public async Task PublishConnectionStatusAsync(bool isConnected)
        {
            try
            {
                var notification = new ClientConnectionStatusNotification(this.Id, isConnected);
                await this._mediator.PublishAsync(notification);
            }
            catch (ObjectDisposedException)
            {
                // Service provider has been disposed (likely during shutdown) - ignore this event
                // This is expected behavior during application shutdown
            }
            catch (Exception ex)
            {
                // Log other unexpected exceptions but don't rethrow to avoid breaking the event flow
                System.Diagnostics.Debug.WriteLine(
                    $"Error publishing connection status for client {this.Id}: {ex.Message}"
                );
            }
        }

        public async Task PublishStateAsync(ClientState state)
        {
            try
            {
                var notification = new ClientStateNotification(this.Id, state);
                await this._mediator.PublishAsync(notification);
            }
            catch (ObjectDisposedException)
            {
                // Service provider has been disposed (likely during shutdown) - ignore this event
                // This is expected behavior during application shutdown
            }
            catch (Exception ex)
            {
                // Log other unexpected exceptions but don't rethrow to avoid breaking the event flow
                System.Diagnostics.Debug.WriteLine($"Error publishing state for client {this.Id}: {ex.Message}");
            }
        }

        #endregion
    }
}
