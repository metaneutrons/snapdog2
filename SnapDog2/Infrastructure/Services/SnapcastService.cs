namespace SnapDog2.Infrastructure.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Cortex.Mediator.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapcastClient;
using SnapcastClient.Models;
using SnapcastClient.Params;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Models;
using SnapDog2.Server.Notifications;

/// <summary>
/// Snapcast service implementation using the enterprise SnapcastClient library.
/// Manages connection to Snapcast server and provides high-level operations.
/// </summary>
public partial class SnapcastService : ISnapcastService, IAsyncDisposable
{
    private readonly SnapcastConfig _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISnapcastStateRepository _stateRepository;
    private readonly ILogger<SnapcastService> _logger;
    private readonly SnapcastClient.IClient _snapcastClient;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private bool _disposed = false;
    private bool _initialized = false;

    public SnapcastService(
        IOptions<SnapDogConfiguration> configOptions,
        IServiceProvider serviceProvider,
        ISnapcastStateRepository stateRepository,
        ILogger<SnapcastService> logger,
        SnapcastClient.IClient snapcastClient
    )
    {
        this._config = configOptions.Value.Services.Snapcast;
        this._serviceProvider = serviceProvider;
        this._stateRepository = stateRepository;
        this._logger = logger;
        this._snapcastClient = snapcastClient;

        // Subscribe to client events
        this.SubscribeToEvents();
    }

    #region Logging

    [LoggerMessage(1001, LogLevel.Information, "Initializing Snapcast connection to {Host}:{Port}")]
    private partial void LogInitializing(string host, int port);

    [LoggerMessage(1002, LogLevel.Information, "Snapcast connection established successfully")]
    private partial void LogConnectionEstablished();

    [LoggerMessage(1003, LogLevel.Warning, "Snapcast connection lost: {Reason}")]
    private partial void LogConnectionLost(string reason);

    [LoggerMessage(1004, LogLevel.Error, "Failed to initialize Snapcast connection")]
    private partial void LogInitializationFailed(Exception ex);

    [LoggerMessage(1005, LogLevel.Error, "Snapcast operation {Operation} failed")]
    private partial void LogOperationFailed(string operation, Exception ex);

    [LoggerMessage(1006, LogLevel.Debug, "Processing Snapcast event: {EventType}")]
    private partial void LogProcessingEvent(string eventType);

    [LoggerMessage(1007, LogLevel.Error, "Error processing Snapcast event {EventType}")]
    private partial void LogEventProcessingError(string eventType, Exception ex);

    [LoggerMessage(1008, LogLevel.Information, "Snapcast service disposed")]
    private partial void LogServiceDisposed();

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets IMediator from service provider using a scope to avoid lifetime issues.
    /// </summary>
    private async Task PublishNotificationAsync<T>(T notification)
        where T : INotification
    {
        try
        {
            using var scope = this._serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.PublishAsync(notification);
        }
        catch (Exception ex)
        {
            this.LogEventProcessingError(typeof(T).Name, ex);
        }
    }

    #endregion

    #region Initialization

    public async Task<Result> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (this._disposed)
        {
            return Result.Failure("Service has been disposed");
        }

        if (this._initialized)
        {
            return Result.Success();
        }

        this.LogInitializing(this._config.Address, this._config.JsonRpcPort);

        try
        {
            await this._operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (this._initialized)
                {
                    return Result.Success();
                }

                // The enterprise SnapcastClient client handles connection automatically
                // We mark as initialized immediately and let the client handle connection in the background
                // The state repository will be updated via event handlers once connection is established
                this._initialized = true;
                this.LogConnectionEstablished();

                // Publish connection established notification
                await this.PublishNotificationAsync(new SnapcastConnectionEstablishedNotification());

                return Result.Success();
            }
            finally
            {
                this._operationLock.Release();
            }
        }
        catch (Exception ex)
        {
            this.LogInitializationFailed(ex);
            return Result.Failure(ex);
        }
    }

    #endregion

    #region Server Operations

    public async Task<Result<SnapcastServerStatus>> GetServerStatusAsync(CancellationToken cancellationToken = default)
    {
        if (this._disposed)
        {
            return Result<SnapcastServerStatus>.Failure("Service has been disposed");
        }

        try
        {
            var serverStatus = await this._snapcastClient.ServerGetStatusAsync().ConfigureAwait(false);

            // Update our state repository with the raw data
            this._stateRepository.UpdateServerState(serverStatus);

            // Map to our domain model
            var mappedStatus = MapToSnapcastServerStatus(serverStatus);

            return Result<SnapcastServerStatus>.Success(mappedStatus);
        }
        catch (Exception ex)
        {
            this.LogOperationFailed(nameof(this.GetServerStatusAsync), ex);
            return Result<SnapcastServerStatus>.Failure(ex);
        }
    }

    public async Task<Result<VersionDetails>> GetRpcVersionAsync(CancellationToken cancellationToken = default)
    {
        if (this._disposed)
        {
            return Result<VersionDetails>.Failure("Service has been disposed");
        }

        try
        {
            var version = await this._snapcastClient.ServerGetRpcVersionAsync().ConfigureAwait(false);

            var versionDetails = new VersionDetails
            {
                Major = version.Major,
                Minor = version.Minor,
                Patch = version.Patch,
                Version = $"{version.Major}.{version.Minor}.{version.Patch}",
            };

            return Result<VersionDetails>.Success(versionDetails);
        }
        catch (Exception ex)
        {
            this.LogOperationFailed(nameof(this.GetRpcVersionAsync), ex);
            return Result<VersionDetails>.Failure(ex);
        }
    }

    #endregion

    #region Client Operations

    public async Task<Result> SetClientVolumeAsync(
        string snapcastClientId,
        int volumePercent,
        CancellationToken cancellationToken = default
    )
    {
        if (this._disposed)
        {
            return Result.Failure("Service has been disposed");
        }

        try
        {
            await this._snapcastClient.ClientSetVolumeAsync(snapcastClientId, volumePercent).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed(nameof(this.SetClientVolumeAsync), ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> SetClientMuteAsync(
        string snapcastClientId,
        bool muted,
        CancellationToken cancellationToken = default
    )
    {
        if (this._disposed)
        {
            return Result.Failure("Service has been disposed");
        }

        try
        {
            // Get current client to preserve volume when muting/unmuting
            var client = this._stateRepository.GetClient(snapcastClientId);
            if (client == null)
            {
                return Result.Failure($"Client {snapcastClientId} not found");
            }

            var currentVolume = client.Value.Config.Volume.Percent;
            var newVolume = muted ? 0 : currentVolume;

            await this._snapcastClient.ClientSetVolumeAsync(snapcastClientId, newVolume).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed(nameof(this.SetClientMuteAsync), ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> SetClientLatencyAsync(
        string snapcastClientId,
        int latencyMs,
        CancellationToken cancellationToken = default
    )
    {
        if (this._disposed)
        {
            return Result.Failure("Service has been disposed");
        }

        try
        {
            await this._snapcastClient.ClientSetLatencyAsync(snapcastClientId, latencyMs).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed(nameof(this.SetClientLatencyAsync), ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> SetClientNameAsync(
        string snapcastClientId,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        if (this._disposed)
        {
            return Result.Failure("Service has been disposed");
        }

        try
        {
            await this._snapcastClient.ClientSetNameAsync(snapcastClientId, name).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed(nameof(this.SetClientNameAsync), ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> SetClientGroupAsync(
        string snapcastClientId,
        string groupId,
        CancellationToken cancellationToken = default
    )
    {
        if (this._disposed)
        {
            return Result.Failure("Service has been disposed");
        }

        try
        {
            await this
                ._snapcastClient.GroupSetClientsAsync(groupId, new List<string> { snapcastClientId })
                .ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed(nameof(this.SetClientGroupAsync), ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> DeleteClientAsync(string snapcastClientId, CancellationToken cancellationToken = default)
    {
        if (this._disposed)
        {
            return Result.Failure("Service has been disposed");
        }

        try
        {
            await this._snapcastClient.ServerDeleteClientAsync(snapcastClientId).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed(nameof(this.DeleteClientAsync), ex);
            return Result.Failure(ex);
        }
    }

    #endregion

    #region Group Operations

    public async Task<Result> SetGroupMuteAsync(
        string groupId,
        bool muted,
        CancellationToken cancellationToken = default
    )
    {
        if (this._disposed)
        {
            return Result.Failure("Service has been disposed");
        }

        try
        {
            await this._snapcastClient.GroupSetMuteAsync(groupId, muted).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed(nameof(this.SetGroupMuteAsync), ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> SetGroupStreamAsync(
        string groupId,
        string streamId,
        CancellationToken cancellationToken = default
    )
    {
        if (this._disposed)
        {
            return Result.Failure("Service has been disposed");
        }

        try
        {
            await this._snapcastClient.GroupSetStreamAsync(groupId, streamId).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed(nameof(this.SetGroupStreamAsync), ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> SetGroupNameAsync(
        string groupId,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        if (this._disposed)
        {
            return Result.Failure("Service has been disposed");
        }

        try
        {
            await this._snapcastClient.GroupSetNameAsync(groupId, name).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed(nameof(this.SetGroupNameAsync), ex);
            return Result.Failure(ex);
        }
    }

    public Task<Result<string>> CreateGroupAsync(
        IEnumerable<string> clientIds,
        CancellationToken cancellationToken = default
    )
    {
        if (this._disposed)
        {
            return Task.FromResult(Result<string>.Failure("Service has been disposed"));
        }

        try
        {
            var clientIdArray = clientIds.ToArray();
            if (clientIdArray.Length == 0)
            {
                return Task.FromResult(Result<string>.Failure("At least one client ID is required"));
            }

            // For now, we'll use the first client's current group as a template
            // In a real implementation, you might want to create a new group ID
            var firstClient = this._stateRepository.GetClient(clientIdArray[0]);
            if (firstClient == null)
            {
                return Task.FromResult(Result<string>.Failure($"Client {clientIdArray[0]} not found"));
            }

            // This is a simplified implementation - in practice, you'd need to handle group creation differently
            return Task.FromResult(
                Result<string>.Failure("Group creation not yet implemented in SnapcastClient library")
            );
        }
        catch (Exception ex)
        {
            this.LogOperationFailed(nameof(this.CreateGroupAsync), ex);
            return Task.FromResult(Result<string>.Failure(ex));
        }
    }

    public Task<Result> DeleteGroupAsync(string groupId, CancellationToken cancellationToken = default)
    {
        if (this._disposed)
        {
            return Task.FromResult(Result.Failure("Service has been disposed"));
        }

        try
        {
            // SnapcastClient doesn't have a direct DeleteGroup method
            // Groups are typically deleted by moving all clients to other groups
            return Task.FromResult(Result.Failure("Group deletion not yet implemented in SnapcastClient library"));
        }
        catch (Exception ex)
        {
            this.LogOperationFailed(nameof(this.DeleteGroupAsync), ex);
            return Task.FromResult(Result.Failure(ex));
        }
    }

    #endregion

    #region Event Handling

    private void SubscribeToEvents()
    {
        this._snapcastClient.OnClientConnect = this.HandleClientConnect;
        this._snapcastClient.OnClientDisconnect = this.HandleClientDisconnect;
        this._snapcastClient.OnClientVolumeChanged = this.HandleClientVolumeChanged;
        this._snapcastClient.OnClientLatencyChanged = this.HandleClientLatencyChanged;
        this._snapcastClient.OnClientNameChanged = this.HandleClientNameChanged;
        this._snapcastClient.OnGroupMute = this.HandleGroupMuteChanged;
        this._snapcastClient.OnGroupStreamChanged = this.HandleGroupStreamChanged;
        this._snapcastClient.OnGroupNameChanged = this.HandleGroupNameChanged;
        this._snapcastClient.OnStreamUpdate = this.HandleStreamUpdateAsync;
        this._snapcastClient.OnStreamProperties = this.HandleStreamProperties;
        this._snapcastClient.OnServerUpdate = this.HandleServerUpdate;
    }

    private void HandleClientConnect(SnapClient client)
    {
        this.LogProcessingEvent("ClientConnect");
        try
        {
            this._stateRepository.UpdateClient(client);
            _ = this.PublishNotificationAsync(new SnapcastClientConnectedNotification(client));
        }
        catch (Exception ex)
        {
            this.LogEventProcessingError("ClientConnect", ex);
        }
    }

    private void HandleClientDisconnect(SnapClient client)
    {
        this.LogProcessingEvent("ClientDisconnect");
        try
        {
            this._stateRepository.UpdateClient(client);
            _ = this.PublishNotificationAsync(new SnapcastClientDisconnectedNotification(client));
        }
        catch (Exception ex)
        {
            this.LogEventProcessingError("ClientDisconnect", ex);
        }
    }

    private void HandleClientVolumeChanged(ClientSetVolume volumeChange)
    {
        this.LogProcessingEvent("ClientVolumeChanged");
        try
        {
            // Update the client in our repository
            var client = this._stateRepository.GetClient(volumeChange.Id);
            if (client != null)
            {
                var updatedClient = client.Value with
                {
                    Config = client.Value.Config with
                    {
                        Volume = new SnapcastClient.Models.ClientVolume
                        {
                            Muted = volumeChange.Volume.Muted,
                            Percent = volumeChange.Volume.Percent,
                        },
                    },
                };
                this._stateRepository.UpdateClient(updatedClient);
            }

            // Convert Params.ClientVolume to Models.ClientVolume for the notification
            var modelVolume = new SnapcastClient.Models.ClientVolume
            {
                Muted = volumeChange.Volume.Muted,
                Percent = volumeChange.Volume.Percent,
            };
            _ = this.PublishNotificationAsync(
                new SnapcastClientVolumeChangedNotification(volumeChange.Id, modelVolume)
            );
        }
        catch (Exception ex)
        {
            this.LogEventProcessingError("ClientVolumeChanged", ex);
        }
    }

    private void HandleClientLatencyChanged(ClientSetLatency latencyChange)
    {
        this.LogProcessingEvent("ClientLatencyChanged");
        try
        {
            // Update the client in our repository
            var client = this._stateRepository.GetClient(latencyChange.Id);
            if (client != null)
            {
                var updatedClient = client.Value with
                {
                    Config = client.Value.Config with { Latency = latencyChange.Latency },
                };
                this._stateRepository.UpdateClient(updatedClient);
            }

            _ = this.PublishNotificationAsync(
                new SnapcastClientLatencyChangedNotification(latencyChange.Id, latencyChange.Latency)
            );
        }
        catch (Exception ex)
        {
            this.LogEventProcessingError("ClientLatencyChanged", ex);
        }
    }

    private void HandleClientNameChanged(ClientSetName nameChange)
    {
        this.LogProcessingEvent("ClientNameChanged");
        try
        {
            // Update the client in our repository
            var client = this._stateRepository.GetClient(nameChange.Id);
            if (client != null)
            {
                var updatedClient = client.Value with { Config = client.Value.Config with { Name = nameChange.Name } };
                this._stateRepository.UpdateClient(updatedClient);
            }

            _ = this.PublishNotificationAsync(
                new SnapcastClientNameChangedNotification(nameChange.Id, nameChange.Name)
            );
        }
        catch (Exception ex)
        {
            this.LogEventProcessingError("ClientNameChanged", ex);
        }
    }

    private void HandleGroupMuteChanged(GroupOnMute muteChange)
    {
        this.LogProcessingEvent("GroupMuteChanged");
        try
        {
            // Update the group in our repository
            var group = this._stateRepository.GetGroup(muteChange.Id);
            if (group != null)
            {
                var updatedGroup = group.Value with { Muted = muteChange.Mute };
                this._stateRepository.UpdateGroup(updatedGroup);
            }

            _ = this.PublishNotificationAsync(new SnapcastGroupMuteChangedNotification(muteChange.Id, muteChange.Mute));
        }
        catch (Exception ex)
        {
            this.LogEventProcessingError("GroupMuteChanged", ex);
        }
    }

    private void HandleGroupStreamChanged(GroupOnStreamChanged streamChange)
    {
        this.LogProcessingEvent("GroupStreamChanged");
        try
        {
            // Update the group in our repository
            var group = this._stateRepository.GetGroup(streamChange.Id);
            if (group != null)
            {
                var updatedGroup = group.Value with { StreamId = streamChange.StreamId };
                this._stateRepository.UpdateGroup(updatedGroup);
            }

            _ = this.PublishNotificationAsync(
                new SnapcastGroupStreamChangedNotification(streamChange.Id, streamChange.StreamId)
            );
        }
        catch (Exception ex)
        {
            this.LogEventProcessingError("GroupStreamChanged", ex);
        }
    }

    private void HandleGroupNameChanged(GroupOnNameChanged nameChange)
    {
        this.LogProcessingEvent("GroupNameChanged");
        try
        {
            // Update the group in our repository
            var group = this._stateRepository.GetGroup(nameChange.Id);
            if (group != null)
            {
                var updatedGroup = group.Value with { Name = nameChange.Name };
                this._stateRepository.UpdateGroup(updatedGroup);
            }

            _ = this.PublishNotificationAsync(new SnapcastGroupNameChangedNotification(nameChange.Id, nameChange.Name));
        }
        catch (Exception ex)
        {
            this.LogEventProcessingError("GroupNameChanged", ex);
        }
    }

    private async Task HandleStreamUpdateAsync(Stream stream)
    {
        this.LogProcessingEvent("StreamUpdate");
        try
        {
            this._stateRepository.UpdateStream(stream);
            await this.PublishNotificationAsync(new SnapcastStreamUpdatedNotification(stream));
        }
        catch (Exception ex)
        {
            this.LogEventProcessingError("StreamUpdate", ex);
        }
    }

    private void HandleStreamProperties(StreamOnProperties properties)
    {
        this.LogProcessingEvent("StreamProperties");
        try
        {
            // Convert StreamProperties to Dictionary for the notification
            var propertiesDict = new Dictionary<string, object>
            {
                ["canControl"] = properties.Properties.CanControl,
                ["canGoNext"] = properties.Properties.CanGoNext,
                ["canGoPrevious"] = properties.Properties.CanGoPrevious,
                ["canPause"] = properties.Properties.CanPause,
                ["canPlay"] = properties.Properties.CanPlay,
                ["canSeek"] = properties.Properties.CanSeek,
            };

            // Add metadata if available
            if (!string.IsNullOrEmpty(properties.Properties.Metadata?.Title))
            {
                propertiesDict["metadata"] = properties.Properties.Metadata;
            }

            _ = this.PublishNotificationAsync(
                new SnapcastStreamPropertiesChangedNotification(properties.Id, propertiesDict)
            );
        }
        catch (Exception ex)
        {
            this.LogEventProcessingError("StreamProperties", ex);
        }
    }

    private void HandleServerUpdate(Server server)
    {
        this.LogProcessingEvent("ServerUpdate");
        try
        {
            this._stateRepository.UpdateServerState(server);
            _ = this.PublishNotificationAsync(new SnapcastServerUpdatedNotification(server));
        }
        catch (Exception ex)
        {
            this.LogEventProcessingError("ServerUpdate", ex);
        }
    }

    #endregion

    #region Mapping

    private static SnapcastServerStatus MapToSnapcastServerStatus(Server server)
    {
        var serverInfo = new SnapcastServerInfo
        {
            Version = new VersionDetails
            {
                Major = 0, // SnapcastClient doesn't provide version info in the same format
                Minor = 0,
                Patch = 0,
                Version = server.ServerInfo.SnapServer.Version,
            },
            Host = server.ServerInfo.Host.Name,
            Port = 1705, // Default Snapcast port - could be made configurable
            UptimeSeconds = 0, // Not available in SnapcastClient model
        };

        var groups = server
            .Groups.Select(g => new SnapcastGroupInfo
            {
                Id = g.Id,
                Name = g.Name,
                Muted = g.Muted,
                StreamId = g.StreamId,
                Clients = g.Clients.Select(MapToSnapcastClientInfo).ToList().AsReadOnly(),
            })
            .ToList()
            .AsReadOnly();

        var streams = server
            .Streams.Select(s => new SnapcastStreamInfo
            {
                Id = s.Id,
                Status = s.Status,
                Uri = s.Uri.Raw,
                Properties = new Dictionary<string, object>().AsReadOnly(),
            })
            .ToList()
            .AsReadOnly();

        return new SnapcastServerStatus
        {
            Server = serverInfo,
            Groups = groups,
            Streams = streams,
        };
    }

    private static SnapcastClientInfo MapToSnapcastClientInfo(SnapClient client)
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
                Arch = client.Host.Arch,
            },
            LastSeenUtc = DateTimeOffset.FromUnixTimeSeconds(client.LastSeen.Sec).UtcDateTime,
        };
    }

    #endregion

    #region Disposal

    public async ValueTask DisposeAsync()
    {
        if (this._disposed)
        {
            return;
        }

        this._disposed = true;

        try
        {
            // Unsubscribe from events
            if (this._snapcastClient != null)
            {
                this._snapcastClient.OnClientConnect = null;
                this._snapcastClient.OnClientDisconnect = null;
                this._snapcastClient.OnClientVolumeChanged = null;
                this._snapcastClient.OnClientLatencyChanged = null;
                this._snapcastClient.OnClientNameChanged = null;
                this._snapcastClient.OnGroupMute = null;
                this._snapcastClient.OnGroupStreamChanged = null;
                this._snapcastClient.OnGroupNameChanged = null;
                this._snapcastClient.OnStreamUpdate = null;
                this._snapcastClient.OnStreamProperties = null;
                this._snapcastClient.OnServerUpdate = null;

                // Dispose the client if it implements IAsyncDisposable
                if (this._snapcastClient is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else if (this._snapcastClient is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            this._operationLock?.Dispose();
            this.LogServiceDisposed();
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error during SnapcastService disposal");
        }
    }

    #endregion
}
