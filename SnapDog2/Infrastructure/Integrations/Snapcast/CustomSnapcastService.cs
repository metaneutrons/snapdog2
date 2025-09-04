using System.Text.Json;
using Cortex.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Infrastructure.Integrations.Snapcast.JsonRpc;
using SnapDog2.Infrastructure.Integrations.Snapcast.JsonRpc.Models;
using SnapDog2.Server.Snapcast.Notifications;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Models;
using Models = SnapDog2.Infrastructure.Integrations.Snapcast.Models;

namespace SnapDog2.Infrastructure.Integrations.Snapcast;

public partial class CustomSnapcastService : ISnapcastService, IDisposable
{
    private readonly SnapcastJsonRpcClient _jsonRpcClient;
    private readonly IClientStateStore _clientStateStore;
    private readonly ISnapcastStateRepository _stateRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CustomSnapcastService> _logger;
    private readonly Timer _healthCheckTimer;

    public bool IsConnected => _jsonRpcClient != null; // Simplified for now

    public CustomSnapcastService(
        IClientStateStore clientStateStore,
        ISnapcastStateRepository stateRepository,
        IServiceProvider serviceProvider,
        ILogger<CustomSnapcastService> logger,
        IOptions<SnapDogConfiguration> configuration)
    {
        _clientStateStore = clientStateStore;
        _stateRepository = stateRepository;
        _serviceProvider = serviceProvider;
        _logger = logger;

        var webSocketUrl = configuration.Value.Services.Snapcast.WebSocketUrl;
        var jsonRpcLogger = serviceProvider.GetRequiredService<ILogger<SnapcastJsonRpcClient>>();
        _jsonRpcClient = new SnapcastJsonRpcClient(webSocketUrl, jsonRpcLogger);
        _jsonRpcClient.NotificationReceived += HandleNotification;

        _healthCheckTimer = new Timer(PerformHealthCheck, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public async Task<Result> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _jsonRpcClient.ConnectAsync();

            // Initialize state repository with current server state
            await RefreshServerState();

            LogServiceInitialized();
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogInitializationFailed(ex);
            return Result.Failure($"Failed to initialize: {ex.Message}");
        }
    }

    // Interface methods using string snapcastClientId (for backward compatibility)
    public async Task<Result> SetClientVolumeAsync(string snapcastClientId, int volumePercent, CancellationToken cancellationToken = default)
    {
        return await SetClientVolumeAsync(snapcastClientId, volumePercent, false);
    }

    public async Task<Result> SetClientMuteAsync(string snapcastClientId, bool muted, CancellationToken cancellationToken = default)
    {
        // Get current server status to preserve the client's current volume
        var serverStatus = await GetServerStatusAsync();
        if (serverStatus.IsFailure)
        {
            return serverStatus;
        }

        var snapcastClient = serverStatus.Value?.Groups
            ?.SelectMany(g => g.Clients)
            ?.FirstOrDefault(c => c.Id == snapcastClientId);

        if (snapcastClient == null)
        {
            return Result.Failure($"Snapcast client {snapcastClientId} not found in server status");
        }

        // Preserve current volume when muting/unmuting
        return await SetClientVolumeAsync(snapcastClientId, snapcastClient.Volume, muted);
    }

    public async Task<Result> SetClientLatencyAsync(string snapcastClientId, int latencyMs, CancellationToken cancellationToken = default)
    {
        return await SetClientLatencyAsync(snapcastClientId, latencyMs);
    }

    public async Task<Result> SetClientNameAsync(string snapcastClientId, string name, CancellationToken cancellationToken = default)
    {
        return await SetClientNameAsync(snapcastClientId, name);
    }

    public async Task<Result> SetClientGroupAsync(string snapcastClientId, string groupId, CancellationToken cancellationToken = default)
    {
        return await SetGroupClientsAsync(groupId, new[] { snapcastClientId });
    }

    public async Task<Result> SetGroupMuteAsync(string groupId, bool muted, CancellationToken cancellationToken = default)
    {
        return await SetGroupMuteAsync(groupId, muted);
    }

    public async Task<Result> SetGroupStreamAsync(string groupId, string streamId, CancellationToken cancellationToken = default)
    {
        return await SetGroupStreamAsync(groupId, streamId);
    }

    public async Task<Result> SetGroupNameAsync(string groupId, string name, CancellationToken cancellationToken = default)
    {
        return await SetGroupNameAsync(groupId, name);
    }

    public Task<Result> DeleteGroupAsync(string groupId, CancellationToken cancellationToken = default)
    {
        // This would require deleting a group - not implemented in basic JSON-RPC
        return Task.FromResult(Result.Failure("DeleteGroup not supported in JSON-RPC protocol"));
    }

    public async Task<Result> DeleteClientAsync(string snapcastClientId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _jsonRpcClient.SendRequestAsync<ServerGetStatusResponse>("Server.DeleteClient", new { id = snapcastClientId });
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogDeleteClientFailed(ex, snapcastClientId);
            return Result.Failure($"Failed to delete client: {ex.Message}");
        }
    }

    public async Task<Result> SetGroupClientsAsync(string groupId, IEnumerable<string> clientIds, CancellationToken cancellationToken = default)
    {
        return await SetGroupClientsAsync(groupId, clientIds.ToList());
    }

    public async Task<Result<VersionDetails>> GetRpcVersionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _jsonRpcClient.SendRequestAsync<ServerGetRpcVersionResponse>("Server.GetRPCVersion");
            var version = new VersionDetails
            {
                Version = $"{response.Major}.{response.Minor}.{response.Patch}",
                Major = response.Major,
                Minor = response.Minor,
                Patch = response.Patch
            };
            return Result<VersionDetails>.Success(version);
        }
        catch (Exception ex)
        {
            LogGetRpcVersionFailed(ex);
            return Result<VersionDetails>.Failure($"Failed to get version: {ex.Message}");
        }
    }

    public async Task<Result> SetClientVolumeAsync(string snapcastClientId, int volumePercent, bool muted)
    {
        try
        {
            var request = new ClientSetVolumeRequest(
                snapcastClientId,
                new VolumeInfo(muted, volumePercent));

            await _jsonRpcClient.SendRequestAsync<ClientSetVolumeResponse>("Client.SetVolume", request);

            LogSetClientVolume(snapcastClientId, volumePercent, muted);

            return Result.Success();
        }
        catch (Exception ex)
        {
            LogSetClientVolumeFailed(ex, snapcastClientId);
            return Result.Failure($"Failed to set volume: {ex.Message}");
        }
    }

    // Public interface methods that use 1-based client indices
    public async Task<Result> SetClientVolumeAsync(int clientIndex, int volumePercent, CancellationToken cancellationToken = default)
    {
        var (client, snapcastClientId) = await GetSnapcastClientIdByIndexAsync(clientIndex);
        if (client == null)
        {
            return Result.Failure($"Client {clientIndex} not found or not configured");
        }

        return await SetClientVolumeAsync(snapcastClientId, volumePercent, false);
    }

    public async Task<Result> SetClientMuteAsync(int clientIndex, bool muted, CancellationToken cancellationToken = default)
    {
        var (client, snapcastClientId) = await GetSnapcastClientIdByIndexAsync(clientIndex);
        if (client == null)
        {
            return Result.Failure($"Client {clientIndex} not found or not configured");
        }

        // Get current server status to preserve the client's current volume
        var serverStatus = await GetServerStatusAsync();
        if (serverStatus.IsFailure)
        {
            return serverStatus;
        }

        var snapcastClient = serverStatus.Value?.Groups
            ?.SelectMany(g => g.Clients)
            ?.FirstOrDefault(c => c.Id == snapcastClientId);

        if (snapcastClient == null)
        {
            return Result.Failure($"Snapcast client {snapcastClientId} not found in server status");
        }

        // Preserve current volume when muting/unmuting
        return await SetClientVolumeAsync(snapcastClientId, snapcastClient.Volume, muted);
    }

    public async Task<Result> SetClientLatencyAsync(string snapcastClientId, int latencyMs)
    {
        try
        {
            var request = new ClientSetLatencyRequest(snapcastClientId, latencyMs);
            await _jsonRpcClient.SendRequestAsync<ClientSetLatencyResponse>("Client.SetLatency", request);

            LogSetClientLatency(snapcastClientId, latencyMs);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogSetClientLatencyFailed(ex, snapcastClientId);
            return Result.Failure($"Failed to set latency: {ex.Message}");
        }
    }

    public async Task<Result> SetClientNameAsync(string snapcastClientId, string name)
    {
        try
        {
            var request = new ClientSetNameRequest(snapcastClientId, name);
            await _jsonRpcClient.SendRequestAsync<ClientSetNameResponse>("Client.SetName", request);

            LogSetClientName(snapcastClientId, name);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogSetClientNameFailed(ex, snapcastClientId);
            return Result.Failure($"Failed to set name: {ex.Message}");
        }
    }

    public async Task<Result> SetGroupMuteAsync(string groupId, bool muted)
    {
        try
        {
            var request = new GroupSetMuteRequest(groupId, muted);
            await _jsonRpcClient.SendRequestAsync<GroupSetMuteResponse>("Group.SetMute", request);

            LogSetGroupMute(groupId, muted);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogSetGroupMuteFailed(ex, groupId);
            return Result.Failure($"Failed to set mute: {ex.Message}");
        }
    }

    public async Task<Result> SetGroupStreamAsync(string groupId, string streamId)
    {
        try
        {
            var request = new GroupSetStreamRequest(groupId, streamId);
            await _jsonRpcClient.SendRequestAsync<GroupSetStreamResponse>("Group.SetStream", request);

            LogSetGroupStream(groupId, streamId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogSetGroupStreamFailed(ex, groupId);
            return Result.Failure($"Failed to set stream: {ex.Message}");
        }
    }

    public async Task<Result> SetGroupNameAsync(string groupId, string name)
    {
        try
        {
            var request = new GroupSetNameRequest(groupId, name);
            await _jsonRpcClient.SendRequestAsync<GroupSetNameResponse>("Group.SetName", request);

            LogSetGroupName(groupId, name);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogSetGroupNameFailed(ex, groupId);
            return Result.Failure($"Failed to set name: {ex.Message}");
        }
    }

    public async Task<Result> SetGroupClientsAsync(string groupId, List<string> clientIds)
    {
        try
        {
            var request = new GroupSetClientsRequest(groupId, clientIds.ToArray());
            await _jsonRpcClient.SendRequestAsync<ServerGetStatusResponse>("Group.SetClients", request);

            LogSetGroupClients(groupId, string.Join(",", clientIds));
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogSetGroupClientsFailed(ex, groupId);
            return Result.Failure($"Failed to set clients: {ex.Message}");
        }
    }

    public async Task<Result<SnapcastServerStatus>> GetServerStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _jsonRpcClient.SendRequestAsync<ServerGetStatusResponse>("Server.GetStatus");

            // Update state repository with fresh data
            var server = ConvertToServer(response.Server);
            _stateRepository.UpdateServerState(server);

            // Convert to existing SnapcastServerStatus model
            var serverStatus = ConvertToSnapcastServerStatus(response.Server);
            return Result<SnapcastServerStatus>.Success(serverStatus);
        }
        catch (Exception ex)
        {
            LogGetServerStatusFailed(ex);
            return Result<SnapcastServerStatus>.Failure($"Failed to get status: {ex.Message}");
        }
    }

    public async Task<Result<RpcVersion>> GetRpcVersionAsync()
    {
        try
        {
            var response = await _jsonRpcClient.SendRequestAsync<ServerGetRpcVersionResponse>("Server.GetRPCVersion");
            var version = new RpcVersion(response.Major, response.Minor, response.Patch);
            return Result<RpcVersion>.Success(version);
        }
        catch (Exception ex)
        {
            LogGetRpcVersionFailed(ex);
            return Result<RpcVersion>.Failure($"Failed to get version: {ex.Message}");
        }
    }

    private async void HandleNotification(string method, JsonElement parameters)
    {
        try
        {
            switch (method)
            {
                case "Client.OnVolumeChanged":
                    var volumeNotification = JsonSerializer.Deserialize<ClientOnVolumeChangedNotification>(parameters.GetRawText());
                    if (volumeNotification != null)
                    {
                        await HandleClientVolumeChanged(volumeNotification);
                    }

                    break;

                case "Client.OnLatencyChanged":
                    var latencyNotification = JsonSerializer.Deserialize<ClientOnLatencyChangedNotification>(parameters.GetRawText());
                    if (latencyNotification != null)
                    {
                        await HandleClientLatencyChanged(latencyNotification);
                    }

                    break;

                case "Client.OnNameChanged":
                    var nameNotification = JsonSerializer.Deserialize<ClientOnNameChangedNotification>(parameters.GetRawText());
                    if (nameNotification != null)
                    {
                        await HandleClientNameChanged(nameNotification);
                    }

                    break;

                case "Client.OnConnect":
                    var connectNotification = JsonSerializer.Deserialize<ClientOnConnectNotification>(parameters.GetRawText());
                    if (connectNotification != null)
                    {
                        await HandleClientConnect(connectNotification);
                    }

                    break;

                case "Client.OnDisconnect":
                    var disconnectNotification = JsonSerializer.Deserialize<ClientOnDisconnectNotification>(parameters.GetRawText());
                    if (disconnectNotification != null)
                    {
                        await HandleClientDisconnect(disconnectNotification);
                    }

                    break;

                case "Group.OnMute":
                    var groupMuteNotification = JsonSerializer.Deserialize<GroupOnMuteNotification>(parameters.GetRawText());
                    if (groupMuteNotification != null)
                    {
                        await HandleGroupMuteChanged(groupMuteNotification);
                    }

                    break;

                case "Group.OnStreamChanged":
                    var streamNotification = JsonSerializer.Deserialize<GroupOnStreamChangedNotification>(parameters.GetRawText());
                    if (streamNotification != null)
                    {
                        await HandleGroupStreamChanged(streamNotification);
                    }

                    break;

                case "Group.OnNameChanged":
                    var groupNameNotification = JsonSerializer.Deserialize<GroupOnNameChangedNotification>(parameters.GetRawText());
                    if (groupNameNotification != null)
                    {
                        await HandleGroupNameChanged(groupNameNotification);
                    }

                    break;

                case "Server.OnUpdate":
                    var serverNotification = JsonSerializer.Deserialize<ServerOnUpdateNotification>(parameters.GetRawText());
                    if (serverNotification != null)
                    {
                        await HandleServerUpdate(serverNotification);
                    }

                    break;

                default:
                    LogUnhandledNotification(method);
                    break;
            }
        }
        catch (Exception ex)
        {
            LogNotificationHandlingError(ex, method);
        }
    }

    private async Task HandleClientVolumeChanged(ClientOnVolumeChangedNotification notification)
    {
        LogClientVolumeChanged(notification.Id, notification.Volume.Percent, notification.Volume.Muted);

        // Only process notifications for explicitly configured clients
        var (client, clientIndex) = await GetClientBySnapcastIdAsync(notification.Id);
        if (client == null)
        {
            LogIgnoringVolumeChange(notification.Id);
            return;
        }

        // Update storage
        var currentState = _clientStateStore.GetClientState(clientIndex);
        if (currentState != null)
        {
            var updatedState = currentState with
            {
                Volume = notification.Volume.Percent,
                Mute = notification.Volume.Muted
            };
            _clientStateStore.SetClientState(clientIndex, updatedState);
        }

        // Publish SnapDog notifications using 1-based client index
        var volumeNotification = new SnapcastClientVolumeChangedNotification(
            clientIndex.ToString(),
            new Models.ClientVolume { Muted = notification.Volume.Muted, Percent = notification.Volume.Percent });

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.PublishAsync(volumeNotification);
    }

    private async Task HandleClientLatencyChanged(ClientOnLatencyChangedNotification notification)
    {
        var (client, clientIndex) = await GetClientBySnapcastIdAsync(notification.Id);
        if (client != null)
        {
            var latencyNotification = new SnapcastClientLatencyChangedNotification(notification.Id, notification.Latency);
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.PublishAsync(latencyNotification);
        }
    }

    private async Task HandleClientNameChanged(ClientOnNameChangedNotification notification)
    {
        var (client, clientIndex) = await GetClientBySnapcastIdAsync(notification.Id);
        if (client != null)
        {
            var nameNotification = new SnapcastClientNameChangedNotification(notification.Id, notification.Name);
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.PublishAsync(nameNotification);
        }
    }

    private async Task HandleClientConnect(ClientOnConnectNotification notification)
    {
        // Convert JsonRpc ClientInfo to our SnapClient model
        var snapClient = ConvertToSnapClient(notification.Client);
        var connectNotification = new SnapcastClientConnectedNotification(snapClient);
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.PublishAsync(connectNotification);
    }

    private async Task HandleClientDisconnect(ClientOnDisconnectNotification notification)
    {
        // Convert JsonRpc ClientInfo to our SnapClient model
        var snapClient = ConvertToSnapClient(notification.Client);
        var disconnectNotification = new SnapcastClientDisconnectedNotification(snapClient);
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.PublishAsync(disconnectNotification);
    }

    private async Task HandleGroupMuteChanged(GroupOnMuteNotification notification)
    {
        var muteNotification = new SnapcastGroupMuteChangedNotification(notification.Id, notification.Mute);
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.PublishAsync(muteNotification);
    }

    private async Task HandleGroupStreamChanged(GroupOnStreamChangedNotification notification)
    {
        var streamNotification = new SnapcastGroupStreamChangedNotification(notification.Id, notification.StreamId);
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.PublishAsync(streamNotification);
    }

    private async Task HandleGroupNameChanged(GroupOnNameChangedNotification notification)
    {
        var nameNotification = new SnapcastGroupNameChangedNotification(notification.Id, notification.Name);
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.PublishAsync(nameNotification);
    }

    private async Task HandleServerUpdate(ServerOnUpdateNotification notification)
    {
        // Update state repository with current server state
        var server = ConvertToServer(notification.Server);
        _stateRepository.UpdateServerState(server);

        var updateNotification = new SnapcastConnectionEstablishedNotification();
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.PublishAsync(updateNotification);
    }

    private async Task RefreshServerState()
    {
        try
        {
            var response = await _jsonRpcClient.SendRequestAsync<ServerGetStatusResponse>("Server.GetStatus");
            var server = ConvertToServer(response.Server);
            _stateRepository.UpdateServerState(server);
            LogServerStateRefreshed(response.Server.Groups.Length);
        }
        catch (Exception ex)
        {
            LogRefreshServerStateFailed(ex);
        }
    }

    private Models.Server ConvertToServer(JsonRpc.Models.ServerInfo serverInfo)
    {
        return new Models.Server
        {
            Groups = serverInfo.Groups.Select(ConvertToGroup).ToArray(),
            Streams = serverInfo.Streams.Select(ConvertToStream).ToArray()
        };
    }

    private Models.Group ConvertToGroup(JsonRpc.Models.GroupInfo groupInfo)
    {
        return new Models.Group
        {
            Id = groupInfo.Id,
            Name = groupInfo.Name,
            Muted = groupInfo.Muted,
            StreamId = groupInfo.StreamId,
            Clients = groupInfo.Clients.Select(ConvertToSnapClient).ToArray()
        };
    }

    private Models.SnapClient ConvertToSnapClient(JsonRpc.Models.ClientInfo clientInfo)
    {
        return new Models.SnapClient
        {
            Id = clientInfo.Id,
            Host = new Models.HostInfo
            {
                Arch = clientInfo.Host.Arch,
                Ip = clientInfo.Host.Ip,
                Mac = clientInfo.Host.Mac,
                Name = clientInfo.Host.Name,
                Os = clientInfo.Host.Os
            },
            Config = new Models.ClientConfig
            {
                Instance = clientInfo.Config.Instance,
                Latency = clientInfo.Config.Latency,
                Name = clientInfo.Config.Name,
                Volume = new Models.ClientVolume
                {
                    Muted = clientInfo.Config.Volume.Muted,
                    Percent = clientInfo.Config.Volume.Percent
                }
            },
            Connected = clientInfo.Connected,
            LastSeen = new Models.LastSeenInfo
            {
                Sec = clientInfo.LastSeen.Sec,
                Usec = clientInfo.LastSeen.Usec
            },
            Snapclient = new Models.SnapclientInfo
            {
                Name = clientInfo.Snapclient.Name,
                ProtocolVersion = clientInfo.Snapclient.ProtocolVersion,
                Version = clientInfo.Snapclient.Version
            }
        };
    }

    private Models.Stream ConvertToStream(JsonRpc.Models.StreamInfo streamInfo)
    {
        return new Models.Stream
        {
            Id = streamInfo.Id,
            Status = streamInfo.Status,
            Uri = new Models.StreamUri
            {
                Fragment = streamInfo.Uri.Fragment,
                Host = streamInfo.Uri.Host,
                Path = streamInfo.Uri.Path,
                Query = streamInfo.Uri.Query,
                Raw = streamInfo.Uri.Raw,
                Scheme = streamInfo.Uri.Scheme
            }
        };
    }

    private async Task<(Domain.Abstractions.IClient? client, int clientIndex)> GetClientBySnapcastIdAsync(string snapcastId)
    {
        using var scope = _serviceProvider.CreateScope();
        var clientManager = scope.ServiceProvider.GetRequiredService<IClientManager>();
        return await clientManager.GetClientBySnapcastIdAsync(snapcastId);
    }

    private async Task<(Domain.Abstractions.IClient? client, string snapcastClientId)> GetSnapcastClientIdByIndexAsync(int clientIndex)
    {
        using var scope = _serviceProvider.CreateScope();
        var clientManager = scope.ServiceProvider.GetRequiredService<IClientManager>();
        var client = await clientManager.GetClientAsync(clientIndex);

        if (client.IsFailure || client.Value == null)
        {
            return (null, string.Empty);
        }

        // Get the MAC address from client configuration
        var configuration = scope.ServiceProvider.GetRequiredService<IOptions<SnapDogConfiguration>>();
        var clientConfigs = configuration.Value.Clients;

        if (clientIndex < 1 || clientIndex > clientConfigs.Count)
        {
            return (null, string.Empty);
        }

        var clientConfig = clientConfigs[clientIndex - 1]; // Convert to 0-based index
        var macAddress = clientConfig.Mac;

        if (string.IsNullOrEmpty(macAddress))
        {
            return (null, string.Empty);
        }

        // Find the Snapcast client with this MAC address
        var allSnapcastClients = _stateRepository.GetAllClients();
        var snapcastClient = allSnapcastClients.FirstOrDefault(c =>
            string.Equals(c.Host.Mac, macAddress, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(snapcastClient.Id))
        {
            return (null, string.Empty);
        }

        return (client.Value, snapcastClient.Id);
    }

    private SnapcastServerStatus ConvertToSnapcastServerStatus(JsonRpc.Models.ServerInfo serverInfo)
    {
        return new SnapcastServerStatus
        {
            Server = new SnapcastServerInfo
            {
                Version = new VersionDetails
                {
                    Version = serverInfo.Server.Snapserver.Version,
                    Major = serverInfo.Server.Snapserver.ProtocolVersion,
                    Minor = 0,
                    Patch = 0
                },
                Host = serverInfo.Server.Host.Name,
                Port = 1705, // Default Snapcast port
                UptimeSeconds = 0 // Not available in JSON-RPC response
            },
            Groups = serverInfo.Groups.Select(ConvertToSnapcastGroupInfo).ToList().AsReadOnly(),
            Streams = serverInfo.Streams.Select(ConvertToSnapcastStreamInfo).ToList().AsReadOnly()
        };
    }

    private SnapcastGroupInfo ConvertToSnapcastGroupInfo(GroupInfo group)
    {
        return new SnapcastGroupInfo
        {
            Id = group.Id,
            Name = group.Name,
            Muted = group.Muted,
            StreamId = group.StreamId,
            Clients = group.Clients.Select(ConvertToSnapcastClientInfo).ToList().AsReadOnly()
        };
    }

    private SnapcastClientInfo ConvertToSnapcastClientInfo(ClientInfo client)
    {
        return new SnapcastClientInfo
        {
            Id = client.Id,
            Name = client.Config.Name,
            Connected = client.Connected,
            Volume = client.Config.Volume.Percent,
            Muted = client.Config.Volume.Muted,
            LatencyMs = client.Config.Latency,
            Host = new SnapcastClientHost
            {
                Ip = client.Host.Ip,
                Name = client.Host.Name,
                Mac = client.Host.Mac,
                Os = client.Host.Os,
                Arch = client.Host.Arch
            },
            LastSeenUtc = DateTimeOffset.FromUnixTimeSeconds(client.LastSeen.Sec).DateTime
        };
    }

    private SnapcastStreamInfo ConvertToSnapcastStreamInfo(StreamInfo stream)
    {
        return new SnapcastStreamInfo
        {
            Id = stream.Id,
            Status = stream.Status,
            Uri = stream.Uri.Raw,
            Properties = stream.Uri.Query.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value).AsReadOnly()
        };
    }

    private async void PerformHealthCheck(object? state)
    {
        try
        {
            await GetRpcVersionAsync();
        }
        catch (Exception ex)
        {
            LogHealthCheckFailed(ex);
            try
            {
                await _jsonRpcClient.ConnectAsync();
            }
            catch (Exception reconnectEx)
            {
                LogReconnectFailed(reconnectEx);
            }
        }
    }

    public void Dispose()
    {
        _healthCheckTimer?.Dispose();
        _jsonRpcClient?.Dispose();
    }

    // LoggerMessage patterns
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Custom Snapcast service initialized successfully")]
    private partial void LogServiceInitialized();

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "Failed to initialize custom Snapcast service")]
    private partial void LogInitializationFailed(Exception ex);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error, Message = "Failed to delete client {ClientId}")]
    private partial void LogDeleteClientFailed(Exception ex, string clientId);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Error, Message = "Failed to get RPC version")]
    private partial void LogGetRpcVersionFailed(Exception ex);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Debug, Message = "Set client {ClientId} volume to {Volume}% (muted: {Muted})")]
    private partial void LogSetClientVolume(string clientId, int volume, bool muted);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Error, Message = "Failed to set client volume for {ClientId}")]
    private partial void LogSetClientVolumeFailed(Exception ex, string clientId);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Debug, Message = "Set client {ClientId} latency to {Latency}ms")]
    private partial void LogSetClientLatency(string clientId, int latency);

    [LoggerMessage(EventId = 1008, Level = LogLevel.Error, Message = "Failed to set client latency for {ClientId}")]
    private partial void LogSetClientLatencyFailed(Exception ex, string clientId);

    [LoggerMessage(EventId = 1009, Level = LogLevel.Debug, Message = "Set client {ClientId} name to {Name}")]
    private partial void LogSetClientName(string clientId, string name);

    [LoggerMessage(EventId = 1010, Level = LogLevel.Error, Message = "Failed to set client name for {ClientId}")]
    private partial void LogSetClientNameFailed(Exception ex, string clientId);

    [LoggerMessage(EventId = 1011, Level = LogLevel.Debug, Message = "Set group {GroupId} mute to {Muted}")]
    private partial void LogSetGroupMute(string groupId, bool muted);

    [LoggerMessage(EventId = 1012, Level = LogLevel.Error, Message = "Failed to set group mute for {GroupId}")]
    private partial void LogSetGroupMuteFailed(Exception ex, string groupId);

    [LoggerMessage(EventId = 1013, Level = LogLevel.Debug, Message = "Set group {GroupId} stream to {StreamId}")]
    private partial void LogSetGroupStream(string groupId, string streamId);

    [LoggerMessage(EventId = 1014, Level = LogLevel.Error, Message = "Failed to set group stream for {GroupId}")]
    private partial void LogSetGroupStreamFailed(Exception ex, string groupId);

    [LoggerMessage(EventId = 1015, Level = LogLevel.Debug, Message = "Set group {GroupId} name to {Name}")]
    private partial void LogSetGroupName(string groupId, string name);

    [LoggerMessage(EventId = 1016, Level = LogLevel.Error, Message = "Failed to set group name for {GroupId}")]
    private partial void LogSetGroupNameFailed(Exception ex, string groupId);

    [LoggerMessage(EventId = 1017, Level = LogLevel.Debug, Message = "Set group {GroupId} clients to {Clients}")]
    private partial void LogSetGroupClients(string groupId, string clients);

    [LoggerMessage(EventId = 1018, Level = LogLevel.Error, Message = "Failed to set group clients for {GroupId}")]
    private partial void LogSetGroupClientsFailed(Exception ex, string groupId);

    [LoggerMessage(EventId = 1019, Level = LogLevel.Error, Message = "Failed to get server status")]
    private partial void LogGetServerStatusFailed(Exception ex);

    [LoggerMessage(EventId = 1020, Level = LogLevel.Debug, Message = "Unhandled notification: {Method}")]
    private partial void LogUnhandledNotification(string method);

    [LoggerMessage(EventId = 1021, Level = LogLevel.Error, Message = "Error handling notification {Method}")]
    private partial void LogNotificationHandlingError(Exception ex, string method);

    [LoggerMessage(EventId = 1022, Level = LogLevel.Debug, Message = "🔊 Client volume changed: {ClientId} -> {Volume}% (muted: {Muted})")]
    private partial void LogClientVolumeChanged(string clientId, int volume, bool muted);

    [LoggerMessage(EventId = 1023, Level = LogLevel.Debug, Message = "Ignoring volume change for unconfigured client: {ClientId}")]
    private partial void LogIgnoringVolumeChange(string clientId);

    [LoggerMessage(EventId = 1024, Level = LogLevel.Debug, Message = "Server state refreshed with {GroupCount} groups")]
    private partial void LogServerStateRefreshed(int groupCount);

    [LoggerMessage(EventId = 1025, Level = LogLevel.Warning, Message = "Failed to refresh server state")]
    private partial void LogRefreshServerStateFailed(Exception ex);

    [LoggerMessage(EventId = 1026, Level = LogLevel.Warning, Message = "Health check failed, attempting reconnection")]
    private partial void LogHealthCheckFailed(Exception ex);

    [LoggerMessage(EventId = 1027, Level = LogLevel.Error, Message = "Failed to reconnect during health check")]
    private partial void LogReconnectFailed(Exception ex);
}
