namespace SnapDog2.Infrastructure.Domain;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

    [LoggerMessage(7001, LogLevel.Debug, "Getting client {ClientId}")]
    private partial void LogGettingClient(int clientId);

    [LoggerMessage(7002, LogLevel.Warning, "Client {ClientId} not found")]
    private partial void LogClientNotFound(int clientId);

    [LoggerMessage(7003, LogLevel.Debug, "Getting all clients")]
    private partial void LogGettingAllClients();

    [LoggerMessage(7004, LogLevel.Debug, "Getting clients for zone {ZoneId}")]
    private partial void LogGettingClientsByZone(int zoneId);

    [LoggerMessage(7005, LogLevel.Information, "Assigning client {ClientId} to zone {ZoneId}")]
    private partial void LogAssigningClientToZone(int clientId, int zoneId);

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

        this.LogInitialized(this._clientConfigs.Count);
    }

    public async Task<Result<IClient>> GetClientAsync(int clientId)
    {
        this.LogGettingClient(clientId);

        await Task.Delay(1); // Maintain async signature

        // Validate client ID range
        if (clientId < 1 || clientId > this._clientConfigs.Count)
        {
            this.LogClientNotFound(clientId);
            return Result<IClient>.Failure(
                $"Client {clientId} is out of range. Valid range: 1-{this._clientConfigs.Count}"
            );
        }

        // Get client from Snapcast using the repository's lookup logic
        var snapcastClient = this._snapcastStateRepository.GetClientByIndex(clientId);
        if (snapcastClient == null)
        {
            this.LogClientNotFound(clientId);
            return Result<IClient>.Failure($"Client {clientId} not found in Snapcast");
        }

        // Convert Snapcast client to domain client
        var client = new SnapDogClient(
            clientId,
            snapcastClient.Value,
            this._clientConfigs[clientId - 1],
            this._snapcastService
        );
        return Result<IClient>.Success(client);
    }

    public async Task<Result<ClientState>> GetClientStateAsync(int clientId)
    {
        this.LogGettingClient(clientId);

        var clientResult = await this.GetClientAsync(clientId);
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
            ZoneId = snapDogClient.ZoneId,
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

        for (int clientId = 1; clientId <= this._clientConfigs.Count; clientId++)
        {
            var stateResult = await this.GetClientStateAsync(clientId);
            if (stateResult.IsSuccess)
            {
                clientStates.Add(stateResult.Value!);
            }
            // Continue even if some clients are not found/connected
        }

        return Result<List<ClientState>>.Success(clientStates);
    }

    public async Task<Result<List<ClientState>>> GetClientsByZoneAsync(int zoneId)
    {
        this.LogGettingClientsByZone(zoneId);

        var allClientsResult = await this.GetAllClientsAsync();
        if (!allClientsResult.IsSuccess)
        {
            return Result<List<ClientState>>.Failure(allClientsResult.ErrorMessage!);
        }

        var zoneClients = allClientsResult.Value!.Where(c => c.ZoneId == zoneId).ToList();

        return Result<List<ClientState>>.Success(zoneClients);
    }

    public async Task<Result> AssignClientToZoneAsync(int clientId, int zoneId)
    {
        this.LogAssigningClientToZone(clientId, zoneId);

        // TODO: Implement zone assignment through Snapcast groups
        // This would require mapping zones to Snapcast groups and moving clients between groups
        await Task.Delay(1); // Maintain async signature

        return Result.Failure("Client zone assignment not yet implemented");
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
        internal int ZoneId => this._config.DefaultZone; // TODO: Get actual zone from Snapcast groups
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
