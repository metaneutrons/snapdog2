namespace SnapDog2.Infrastructure.Domain;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Models;

/// <summary>
/// Production implementation of IClientManager with Snapcast integration.
/// Uses SnapcastStateRepository.GetClientByIndex for clean separation of concerns.
/// </summary>
public partial class ClientManager : IClientManager
{
    private readonly ILogger<ClientManager> _logger;
    private readonly ISnapcastStateRepository _snapcastStateRepository;
    private readonly ISnapcastService _snapcastService;
    private readonly List<ClientConfig> _clientConfigs;
    private readonly SnapDogConfiguration _configuration;

    [LoggerMessage(7001, LogLevel.Debug, "Getting client {ClientIndex}")]
    private partial void LogGettingClient(int clientIndex);

    [LoggerMessage(7002, LogLevel.Warning, "Client {ClientIndex} not found")]
    private partial void LogClientNotFound(int clientIndex);

    [LoggerMessage(7003, LogLevel.Debug, "Getting all clients")]
    private partial void LogGettingAllClients();

    [LoggerMessage(7004, LogLevel.Debug, "Getting clients for zone {ZoneIndex}")]
    private partial void LogGettingClientsByZone(int zoneIndex);

    [LoggerMessage(7005, LogLevel.Information, "Assigning client {ClientIndex} to zone {ZoneIndex}")]
    private partial void LogAssigningClientToZone(int clientIndex, int zoneIndex);

    [LoggerMessage(7006, LogLevel.Information, "Initialized ClientManager with {ClientCount} configured clients")]
    private partial void LogInitialized(int clientCount);

    public ClientManager(
        ILogger<ClientManager> logger,
        ISnapcastStateRepository snapcastStateRepository,
        ISnapcastService snapcastService,
        SnapDogConfiguration configuration
    )
    {
        this._logger = logger;
        this._snapcastStateRepository = snapcastStateRepository;
        this._snapcastService = snapcastService;
        this._clientConfigs = configuration.Clients;
        this._configuration = configuration;

        this.LogInitialized(this._clientConfigs.Count);
    }

    public async Task<Result<IClient>> GetClientAsync(int clientIndex)
    {
        this.LogGettingClient(clientIndex);

        await Task.Delay(1); // Maintain async signature

        // Validate client ID range
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
            this._snapcastStateRepository
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
            Id = client.Id,
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
            if (clientIndex < 1 || clientIndex > _clientConfigs.Count)
            {
                return Result.Failure($"Invalid client index: {clientIndex}");
            }

            // Get the client from Snapcast
            var snapcastClient = _snapcastStateRepository.GetClientByIndex(clientIndex);
            if (snapcastClient == null)
            {
                return Result.Failure($"Client {clientIndex} not found in Snapcast");
            }

            var snapcastClientId = snapcastClient.Value.Id;
            if (string.IsNullOrEmpty(snapcastClientId))
            {
                return Result.Failure($"Client {clientIndex} has no valid ID");
            }

            this._logger.LogDebug("Found client {ClientIndex} with Snapcast ID: {SnapcastClientId}", clientIndex, snapcastClientId);

            // Get target zone's stream ID
            var zoneConfigs = _configuration.Zones;
            if (zoneIndex < 1 || zoneIndex > zoneConfigs.Count)
            {
                return Result.Failure($"Invalid zone index: {zoneIndex}");
            }

            var targetZoneConfig = zoneConfigs[zoneIndex - 1];
            var targetStreamId = ExtractStreamIdFromSink(targetZoneConfig.Sink);
            
            this._logger.LogDebug("Target zone {ZoneIndex} maps to stream: {StreamId}", zoneIndex, targetStreamId);

            // Find or create group for target zone
            var targetGroupId = await FindOrCreateGroupForStreamAsync(targetStreamId);
            if (targetGroupId == null)
            {
                return Result.Failure($"Failed to find or create group for zone {zoneIndex}");
            }

            this._logger.LogDebug("Using group {GroupId} for zone {ZoneIndex}", targetGroupId, zoneIndex);

            // Move client to target group
            var result = await _snapcastService.SetClientGroupAsync(snapcastClientId, targetGroupId);
            if (result.IsFailure)
            {
                this._logger.LogWarning("Failed to move client {ClientId} to group {GroupId}: {Error}", 
                    snapcastClientId, targetGroupId, result.ErrorMessage);
                return Result.Failure($"Failed to move client to zone {zoneIndex}: {result.ErrorMessage}");
            }

            // Refresh server state to update our local repository
            this._logger.LogDebug("Refreshing server state after zone assignment");
            var serverStatusResult = await _snapcastService.GetServerStatusAsync();
            if (serverStatusResult.IsSuccess)
            {
                // The GetServerStatusAsync should trigger state repository updates via event handlers
                this._logger.LogDebug("Server state refreshed successfully");
            }
            else
            {
                this._logger.LogWarning("Failed to refresh server state after zone assignment: {Error}", 
                    serverStatusResult.ErrorMessage);
            }

            this._logger.LogInformation("Successfully assigned client {ClientIndex} ({ClientId}) to zone {ZoneIndex} (group {GroupId})", 
                clientIndex, snapcastClientId, zoneIndex, targetGroupId);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error assigning client {ClientIndex} to zone {ZoneIndex}", clientIndex, zoneIndex);
            return Result.Failure($"Error assigning client {clientIndex} to zone {zoneIndex}: {ex.Message}");
        }
    }

    private async Task<string?> FindOrCreateGroupForStreamAsync(string streamId)
    {
        try
        {
            // Find existing group with this stream
            var allGroups = _snapcastStateRepository.GetAllGroups();
            var existingGroup = allGroups.FirstOrDefault(g => g.StreamId == streamId);

            if (existingGroup.Id != null)
            {
                this._logger.LogDebug("Found existing group {GroupId} for stream {StreamId}", existingGroup.Id, streamId);
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
                this._logger.LogDebug("Assigning group {GroupId} to stream {StreamId}", availableGroup.Id, streamId);
                
                // Set the group to use our target stream
                var result = await _snapcastService.SetGroupStreamAsync(availableGroup.Id, streamId);
                if (result.IsSuccess)
                {
                    this._logger.LogDebug("Successfully assigned group {GroupId} to stream {StreamId}", availableGroup.Id, streamId);
                    return availableGroup.Id;
                }
                else
                {
                    this._logger.LogWarning("Failed to assign group {GroupId} to stream {StreamId}: {Error}", 
                        availableGroup.Id, streamId, result.ErrorMessage);
                }
            }

            this._logger.LogWarning("No available groups found for stream {StreamId}", streamId);
            return null;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error finding or creating group for stream {StreamId}", streamId);
            return null;
        }
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

    /// <summary>
    /// Internal implementation of IClient that wraps a Snapcast client with SnapDog2 configuration.
    /// </summary>
    private class SnapDogClient : IClient
    {
        internal readonly SnapcastClient.Models.SnapClient _snapcastClient;
        private readonly ClientConfig _config;
        private readonly ISnapcastService _snapcastService;
        private readonly ISnapcastStateRepository _snapcastStateRepository;

        public SnapDogClient(
            int id,
            SnapcastClient.Models.SnapClient snapcastClient,
            ClientConfig config,
            ISnapcastService snapcastService,
            ISnapcastStateRepository snapcastStateRepository
        )
        {
            this.Id = id;
            this._snapcastClient = snapcastClient;
            this._config = config;
            this._snapcastService = snapcastService;
            this._snapcastStateRepository = snapcastStateRepository;
        }

        public int Id { get; }
        public string Name => this._config.Name;

        // Internal properties for ClientState mapping
        internal bool Connected => this._snapcastClient.Connected;
        internal int Volume => this._snapcastClient.Config.Volume.Percent;
        internal bool Muted => this._snapcastClient.Config.Volume.Muted;
        internal int LatencyMs => this._snapcastClient.Config.Latency;
        internal int ZoneIndex => GetActualZoneFromSnapcast();

        private int GetActualZoneFromSnapcast()
        {
            try
            {
                // Find the group this client belongs to
                var allGroups = _snapcastStateRepository.GetAllGroups();
                var clientGroup = allGroups.FirstOrDefault(g => 
                    g.Clients.Any(c => c.Id == _snapcastClient.Id));

                if (clientGroup.Id == null || string.IsNullOrEmpty(clientGroup.StreamId))
                {
                    // No group or no stream assigned, return default zone
                    return _config.DefaultZone;
                }

                // Map stream ID to zone index
                // Zone1 -> 1, Zone2 -> 2, etc.
                if (clientGroup.StreamId.StartsWith("Zone") && 
                    int.TryParse(clientGroup.StreamId.Substring(4), out int zoneIndex))
                {
                    return zoneIndex;
                }

                // If we can't parse the stream ID, return default zone
                return _config.DefaultZone;
            }
            catch
            {
                // On any error, return default zone
                return _config.DefaultZone;
            }
        }
        internal string MacAddress => this._snapcastClient.Host.Mac;
        internal string IpAddress => this._snapcastClient.Host.Ip;
        internal DateTime LastSeenUtc =>
            DateTimeOffset.FromUnixTimeSeconds(this._snapcastClient.LastSeen.Sec).UtcDateTime;

        public async Task<Result> SetVolumeAsync(int volume)
        {
            return await this._snapcastService.SetClientVolumeAsync(this._snapcastClient.Id, volume);
        }

        public async Task<Result> SetMuteAsync(bool mute)
        {
            return await this._snapcastService.SetClientMuteAsync(this._snapcastClient.Id, mute);
        }

        public async Task<Result> SetLatencyAsync(int latencyMs)
        {
            return await this._snapcastService.SetClientLatencyAsync(this._snapcastClient.Id, latencyMs);
        }
    }
}
