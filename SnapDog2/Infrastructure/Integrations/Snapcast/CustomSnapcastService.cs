using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Infrastructure.Integrations.Snapcast.JsonRpc;
using SnapDog2.Infrastructure.Integrations.Snapcast.JsonRpc.Models;
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

        _healthCheckTimer = new Timer(PerformHealthCheck, null, 30000, 30000); // 30 seconds in milliseconds
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

        // Notification publishing removed - using direct SignalR calls instead
    }

    private async Task HandleClientLatencyChanged(ClientOnLatencyChangedNotification notification)
    {
        var (client, clientIndex) = await GetClientBySnapcastIdAsync(notification.Id);
        if (client != null)
        {
            // Notification publishing removed - using direct SignalR calls instead
        }
    }

    private async Task HandleClientNameChanged(ClientOnNameChangedNotification notification)
    {
        var (client, clientIndex) = await GetClientBySnapcastIdAsync(notification.Id);
        if (client != null)
        {
            // Notification publishing removed - using direct SignalR calls instead
        }
    }

    private Task HandleClientConnect(ClientOnConnectNotification notification)
    {
        // Convert JsonRpc ClientInfo to our SnapClient model
        var snapClient = ConvertToSnapClient(notification.Client);
        // Notification publishing removed - using direct SignalR calls instead
        return Task.CompletedTask;
    }

    private Task HandleClientDisconnect(ClientOnDisconnectNotification notification)
    {
        // Convert JsonRpc ClientInfo to our SnapClient model
        var snapClient = ConvertToSnapClient(notification.Client);
        // Notification publishing removed - using direct SignalR calls instead
        return Task.CompletedTask;
    }

    private Task HandleGroupMuteChanged(GroupOnMuteNotification notification)
    {
        // Notification publishing removed - using direct SignalR calls instead
        // TODO: Update internal state and notify clients via SignalR
        return Task.CompletedTask;
    }

    private Task HandleGroupStreamChanged(GroupOnStreamChangedNotification notification)
    {
        // Notification publishing removed - using direct SignalR calls instead
        // TODO: Update internal state and notify clients via SignalR
        return Task.CompletedTask;
    }

    private Task HandleGroupNameChanged(GroupOnNameChangedNotification notification)
    {
        // Notification publishing removed - using direct SignalR calls instead
        // TODO: Update internal state and notify clients via SignalR
        return Task.CompletedTask;
    }

    private Task HandleServerUpdate(ServerOnUpdateNotification notification)
    {
        // Update state repository with current server state
        var server = ConvertToServer(notification.Server);
        _stateRepository.UpdateServerState(server);

        // Notification publishing removed - using direct SignalR calls instead
        return Task.CompletedTask;
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

    private async Task<(IClient? client, string snapcastClientId)> GetSnapcastClientIdByIndexAsync(int clientIndex)
    {
        using var scope = _serviceProvider.CreateScope();
        var clientManager = scope.ServiceProvider.GetRequiredService<IClientManager>();

        // Get client state to access SnapcastId
        var stateResult = await clientManager.GetClientStateAsync(clientIndex);
        if (!stateResult.IsSuccess || stateResult.Value == null)
        {
            return (null, string.Empty);
        }

        // Get the IClient instance
        var clientResult = await clientManager.GetClientAsync(clientIndex);
        if (!clientResult.IsSuccess || clientResult.Value == null)
        {
            return (null, string.Empty);
        }

        return (clientResult.Value, stateResult.Value.SnapcastId);
    }

    private SnapcastServerStatus ConvertToSnapcastServerStatus(JsonRpc.Models.ServerInfo serverInfo)
    {
        return new SnapcastServerStatus
        {
            Server = new SnapcastServerInfo
            {
                Version = new VersionDetails
                {
                    Version = "0.0.0", // Default version
                    Major = 0,
                    Minor = 0,
                    Patch = 0
                },
                Host = "localhost", // Default - would need actual host info
                Port = 1705, // Default Snapcast port
                UptimeSeconds = 0 // Default - would need actual uptime
            },
            Groups = serverInfo.Groups.Select(g => new SnapcastGroupInfo
            {
                Id = g.Id,
                Name = g.Name,
                Muted = g.Muted,
                StreamId = g.StreamId,
                Clients = g.Clients.Select(c => new SnapcastClientInfo
                {
                    Id = c.Id,
                    Name = c.Config.Name,
                    Connected = c.Connected,
                    Volume = c.Config.Volume.Percent,
                    Muted = c.Config.Volume.Muted,
                    LatencyMs = c.Config.Latency,
                    Host = new SnapcastClientHost
                    {
                        Ip = c.Host.Ip,
                        Name = c.Host.Name,
                        Mac = c.Host.Mac,
                        Os = c.Host.Os,
                        Arch = c.Host.Arch
                    },
                    LastSeenUtc = c.LastSeen != null ? DateTimeOffset.FromUnixTimeSeconds(c.LastSeen.Sec).UtcDateTime : DateTime.UtcNow
                }).ToList().AsReadOnly()
            }).ToList().AsReadOnly(),
            Streams = serverInfo.Streams.Select(s => new SnapcastStreamInfo
            {
                Id = s.Id,
                Status = s.Status,
                Uri = s.Uri.Raw,
                Properties = new Dictionary<string, object>().AsReadOnly() // FIXME: StreamInfo doesn't have properties in JsonRpc model
            }).ToList().AsReadOnly()
        };
    }

    private void PerformHealthCheck(object? state)
    {
        // Simple health check - could be expanded later
        if (!IsConnected)
        {
            _logger.LogWarning("Snapcast service health check failed - not connected");
        }
    }

    public void Dispose()
    {
        _jsonRpcClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    // LoggerMessage methods
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Snapcast service initialized successfully")]
    private partial void LogServiceInitialized();

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to initialize Snapcast service")]
    private partial void LogInitializationFailed(Exception ex);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to delete client {ClientId}")]
    private partial void LogDeleteClientFailed(Exception ex, string ClientId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Failed to get RPC version")]
    private partial void LogGetRpcVersionFailed(Exception ex);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Set client {ClientId} volume to {Volume}%, muted: {Muted}")]
    private partial void LogSetClientVolume(string ClientId, int Volume, bool Muted);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Failed to set client {ClientId} volume")]
    private partial void LogSetClientVolumeFailed(Exception ex, string ClientId);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Set client {ClientId} latency to {Latency}ms")]
    private partial void LogSetClientLatency(string ClientId, int Latency);

    [LoggerMessage(EventId = 8, Level = LogLevel.Error, Message = "Failed to set client {ClientId} latency")]
    private partial void LogSetClientLatencyFailed(Exception ex, string ClientId);

    [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Set client {ClientId} name to {Name}")]
    private partial void LogSetClientName(string ClientId, string Name);

    [LoggerMessage(EventId = 10, Level = LogLevel.Error, Message = "Failed to set client {ClientId} name")]
    private partial void LogSetClientNameFailed(Exception ex, string ClientId);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Set group {GroupId} mute to {Muted}")]
    private partial void LogSetGroupMute(string GroupId, bool Muted);

    [LoggerMessage(EventId = 12, Level = LogLevel.Error, Message = "Failed to set group {GroupId} mute")]
    private partial void LogSetGroupMuteFailed(Exception ex, string GroupId);

    [LoggerMessage(EventId = 13, Level = LogLevel.Information, Message = "Set group {GroupId} stream to {StreamId}")]
    private partial void LogSetGroupStream(string GroupId, string StreamId);

    [LoggerMessage(EventId = 14, Level = LogLevel.Error, Message = "Failed to set group {GroupId} stream")]
    private partial void LogSetGroupStreamFailed(Exception ex, string GroupId);

    [LoggerMessage(EventId = 15, Level = LogLevel.Information, Message = "Set group {GroupId} name to {Name}")]
    private partial void LogSetGroupName(string GroupId, string Name);

    [LoggerMessage(EventId = 16, Level = LogLevel.Error, Message = "Failed to set group {GroupId} name")]
    private partial void LogSetGroupNameFailed(Exception ex, string GroupId);

    [LoggerMessage(EventId = 17, Level = LogLevel.Information, Message = "Set group {GroupId} clients to {ClientIds}")]
    private partial void LogSetGroupClients(string GroupId, string ClientIds);

    [LoggerMessage(EventId = 18, Level = LogLevel.Error, Message = "Failed to set group {GroupId} clients")]
    private partial void LogSetGroupClientsFailed(Exception ex, string GroupId);

    [LoggerMessage(EventId = 19, Level = LogLevel.Error, Message = "Failed to get server status")]
    private partial void LogGetServerStatusFailed(Exception ex);

    [LoggerMessage(EventId = 20, Level = LogLevel.Warning, Message = "Unhandled notification method: {Method}")]
    private partial void LogUnhandledNotification(string Method);

    [LoggerMessage(EventId = 21, Level = LogLevel.Error, Message = "Error handling notification {Method}")]
    private partial void LogNotificationHandlingError(Exception ex, string Method);

    [LoggerMessage(EventId = 22, Level = LogLevel.Debug, Message = "Client {ClientId} volume changed to {Volume}%, muted: {Muted}")]
    private partial void LogClientVolumeChanged(string ClientId, int Volume, bool Muted);

    [LoggerMessage(EventId = 23, Level = LogLevel.Debug, Message = "Ignoring volume change for client {ClientId} (not found in state store)")]
    private partial void LogIgnoringVolumeChange(string ClientId);

    [LoggerMessage(EventId = 24, Level = LogLevel.Information, Message = "Server state refreshed with {GroupCount} groups")]
    private partial void LogServerStateRefreshed(int GroupCount);

    [LoggerMessage(EventId = 25, Level = LogLevel.Error, Message = "Failed to refresh server state")]
    private partial void LogRefreshServerStateFailed(Exception ex);
}
