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
            this._snapcastService
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

            // Get target zone's stream ID
            var zoneConfigs = _configuration.Zones;
            if (zoneIndex < 1 || zoneIndex > zoneConfigs.Count)
            {
                return Result.Failure($"Invalid zone index: {zoneIndex}");
            }

            var targetZoneConfig = zoneConfigs[zoneIndex - 1];
            var targetStreamId = ExtractStreamIdFromSink(targetZoneConfig.Sink);

            // Find or create group for target zone
            var targetGroupId = await FindOrCreateGroupForStreamAsync(targetStreamId);
            if (targetGroupId == null)
            {
                return Result.Failure($"Failed to find or create group for zone {zoneIndex}");
            }

            // Move client to target group
            var result = await _snapcastService.SetClientGroupAsync(snapcastClientId, targetGroupId);
            if (result.IsFailure)
            {
                return Result.Failure($"Failed to move client to zone {zoneIndex}: {result.ErrorMessage}");
            }

            this.LogAssigningClientToZone(clientIndex, zoneIndex);
            return Result.Success();
        }
        catch (Exception ex)
        {
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
                return existingGroup.Id;
            }

            // No existing group for this stream, need to create one
            // For now, we'll use an existing group and change its stream
            // This is a limitation of the current Snapcast library
            var anyGroup = allGroups.FirstOrDefault();
            if (anyGroup.Id != null)
            {
                // Set the group to use our target stream
                var result = await _snapcastService.SetGroupStreamAsync(anyGroup.Id, streamId);
                if (result.IsSuccess)
                {
                    return anyGroup.Id;
                }
            }

            return null;
        }
        catch (Exception)
        {
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

        public SnapDogClient(
            int id,
            SnapcastClient.Models.SnapClient snapcastClient,
            ClientConfig config,
            ISnapcastService snapcastService
        )
        {
            this.Id = id;
            this._snapcastClient = snapcastClient;
            this._config = config;
            this._snapcastService = snapcastService;
        }

        public int Id { get; }
        public string Name => this._config.Name;

        // Internal properties for ClientState mapping
        internal bool Connected => this._snapcastClient.Connected;
        internal int Volume => this._snapcastClient.Config.Volume.Percent;
        internal bool Muted => this._snapcastClient.Config.Volume.Muted;
        internal int LatencyMs => this._snapcastClient.Config.Latency;
        internal int ZoneIndex => this._config.DefaultZone; // TODO: Get actual zone from Snapcast groups
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
