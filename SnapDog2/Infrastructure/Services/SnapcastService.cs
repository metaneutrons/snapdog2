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
        IOptions<ServicesConfig> configOptions,
        IServiceProvider serviceProvider,
        ISnapcastStateRepository stateRepository,
        ILogger<SnapcastService> logger,
        SnapcastClient.IClient snapcastClient
    )
    {
        _config = configOptions.Value.Snapcast;
        _serviceProvider = serviceProvider;
        _stateRepository = stateRepository;
        _logger = logger;
        _snapcastClient = snapcastClient;

        // Subscribe to client events
        SubscribeToEvents();
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
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.PublishAsync(notification);
        }
        catch (Exception ex)
        {
            LogEventProcessingError(typeof(T).Name, ex);
        }
    }

    #endregion

    #region Initialization

    public async Task<Result> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return Result.Failure("Service has been disposed");

        if (_initialized)
            return Result.Success();

        LogInitializing(_config.Address, _config.Port);

        try
        {
            await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_initialized)
                    return Result.Success();

                // The enterprise SnapcastClient client handles connection automatically
                // We just need to get the initial server status to populate our state repository
                var statusResult = await GetServerStatusAsync(cancellationToken).ConfigureAwait(false);
                if (statusResult.IsFailure)
                {
                    LogInitializationFailed(new InvalidOperationException(statusResult.ErrorMessage));
                    return Result.Failure($"Failed to get initial server status: {statusResult.ErrorMessage}");
                }

                _initialized = true;
                LogConnectionEstablished();

                // Publish connection established notification
                await PublishNotificationAsync(new SnapcastConnectionEstablishedNotification());

                return Result.Success();
            }
            finally
            {
                _operationLock.Release();
            }
        }
        catch (Exception ex)
        {
            LogInitializationFailed(ex);
            return Result.Failure(ex);
        }
    }

    #endregion

    #region Server Operations

    public async Task<Result<SnapcastServerStatus>> GetServerStatusAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return Result<SnapcastServerStatus>.Failure("Service has been disposed");

        try
        {
            var serverStatus = await _snapcastClient.ServerGetStatusAsync().ConfigureAwait(false);

            // Update our state repository with the raw data
            _stateRepository.UpdateServerState(serverStatus);

            // Map to our domain model
            var mappedStatus = MapToSnapcastServerStatus(serverStatus);

            return Result<SnapcastServerStatus>.Success(mappedStatus);
        }
        catch (Exception ex)
        {
            LogOperationFailed(nameof(GetServerStatusAsync), ex);
            return Result<SnapcastServerStatus>.Failure(ex);
        }
    }

    public async Task<Result<VersionDetails>> GetRpcVersionAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return Result<VersionDetails>.Failure("Service has been disposed");

        try
        {
            var version = await _snapcastClient.ServerGetRpcVersionAsync().ConfigureAwait(false);

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
            LogOperationFailed(nameof(GetRpcVersionAsync), ex);
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
        if (_disposed)
            return Result.Failure("Service has been disposed");

        try
        {
            await _snapcastClient.ClientSetVolumeAsync(snapcastClientId, volumePercent).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogOperationFailed(nameof(SetClientVolumeAsync), ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> SetClientMuteAsync(
        string snapcastClientId,
        bool muted,
        CancellationToken cancellationToken = default
    )
    {
        if (_disposed)
            return Result.Failure("Service has been disposed");

        try
        {
            // Get current client to preserve volume when muting/unmuting
            var client = _stateRepository.GetClient(snapcastClientId);
            if (client == null)
                return Result.Failure($"Client {snapcastClientId} not found");

            var currentVolume = client.Value.Config.Volume.Percent;
            var newVolume = muted ? 0 : currentVolume;

            await _snapcastClient.ClientSetVolumeAsync(snapcastClientId, newVolume).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogOperationFailed(nameof(SetClientMuteAsync), ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> SetClientLatencyAsync(
        string snapcastClientId,
        int latencyMs,
        CancellationToken cancellationToken = default
    )
    {
        if (_disposed)
            return Result.Failure("Service has been disposed");

        try
        {
            await _snapcastClient.ClientSetLatencyAsync(snapcastClientId, latencyMs).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogOperationFailed(nameof(SetClientLatencyAsync), ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> SetClientNameAsync(
        string snapcastClientId,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        if (_disposed)
            return Result.Failure("Service has been disposed");

        try
        {
            await _snapcastClient.ClientSetNameAsync(snapcastClientId, name).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogOperationFailed(nameof(SetClientNameAsync), ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> SetClientGroupAsync(
        string snapcastClientId,
        string groupId,
        CancellationToken cancellationToken = default
    )
    {
        if (_disposed)
            return Result.Failure("Service has been disposed");

        try
        {
            await _snapcastClient
                .GroupSetClientsAsync(groupId, new List<string> { snapcastClientId })
                .ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogOperationFailed(nameof(SetClientGroupAsync), ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> DeleteClientAsync(string snapcastClientId, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return Result.Failure("Service has been disposed");

        try
        {
            await _snapcastClient.ServerDeleteClientAsync(snapcastClientId).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogOperationFailed(nameof(DeleteClientAsync), ex);
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
        if (_disposed)
            return Result.Failure("Service has been disposed");

        try
        {
            await _snapcastClient.GroupSetMuteAsync(groupId, muted).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogOperationFailed(nameof(SetGroupMuteAsync), ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> SetGroupStreamAsync(
        string groupId,
        string streamId,
        CancellationToken cancellationToken = default
    )
    {
        if (_disposed)
            return Result.Failure("Service has been disposed");

        try
        {
            await _snapcastClient.GroupSetStreamAsync(groupId, streamId).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogOperationFailed(nameof(SetGroupStreamAsync), ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> SetGroupNameAsync(
        string groupId,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        if (_disposed)
            return Result.Failure("Service has been disposed");

        try
        {
            await _snapcastClient.GroupSetNameAsync(groupId, name).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogOperationFailed(nameof(SetGroupNameAsync), ex);
            return Result.Failure(ex);
        }
    }

    public Task<Result<string>> CreateGroupAsync(
        IEnumerable<string> clientIds,
        CancellationToken cancellationToken = default
    )
    {
        if (_disposed)
            return Task.FromResult(Result<string>.Failure("Service has been disposed"));

        try
        {
            var clientIdArray = clientIds.ToArray();
            if (clientIdArray.Length == 0)
                return Task.FromResult(Result<string>.Failure("At least one client ID is required"));

            // For now, we'll use the first client's current group as a template
            // In a real implementation, you might want to create a new group ID
            var firstClient = _stateRepository.GetClient(clientIdArray[0]);
            if (firstClient == null)
                return Task.FromResult(Result<string>.Failure($"Client {clientIdArray[0]} not found"));

            // This is a simplified implementation - in practice, you'd need to handle group creation differently
            return Task.FromResult(
                Result<string>.Failure("Group creation not yet implemented in SnapcastClient library")
            );
        }
        catch (Exception ex)
        {
            LogOperationFailed(nameof(CreateGroupAsync), ex);
            return Task.FromResult(Result<string>.Failure(ex));
        }
    }

    public Task<Result> DeleteGroupAsync(string groupId, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return Task.FromResult(Result.Failure("Service has been disposed"));

        try
        {
            // SnapcastClient doesn't have a direct DeleteGroup method
            // Groups are typically deleted by moving all clients to other groups
            return Task.FromResult(Result.Failure("Group deletion not yet implemented in SnapcastClient library"));
        }
        catch (Exception ex)
        {
            LogOperationFailed(nameof(DeleteGroupAsync), ex);
            return Task.FromResult(Result.Failure(ex));
        }
    }

    #endregion

    #region Event Handling

    private void SubscribeToEvents()
    {
        _snapcastClient.OnClientConnect = HandleClientConnect;
        _snapcastClient.OnClientDisconnect = HandleClientDisconnect;
        _snapcastClient.OnClientVolumeChanged = HandleClientVolumeChanged;
        _snapcastClient.OnClientLatencyChanged = HandleClientLatencyChanged;
        _snapcastClient.OnClientNameChanged = HandleClientNameChanged;
        _snapcastClient.OnGroupMute = HandleGroupMuteChanged;
        _snapcastClient.OnGroupStreamChanged = HandleGroupStreamChanged;
        _snapcastClient.OnGroupNameChanged = HandleGroupNameChanged;
        _snapcastClient.OnStreamUpdate = HandleStreamUpdateAsync;
        _snapcastClient.OnStreamProperties = HandleStreamProperties;
        _snapcastClient.OnServerUpdate = HandleServerUpdate;
    }

    private void HandleClientConnect(SnapClient client)
    {
        LogProcessingEvent("ClientConnect");
        try
        {
            _stateRepository.UpdateClient(client);
            _ = PublishNotificationAsync(new SnapcastClientConnectedNotification(client));
        }
        catch (Exception ex)
        {
            LogEventProcessingError("ClientConnect", ex);
        }
    }

    private void HandleClientDisconnect(SnapClient client)
    {
        LogProcessingEvent("ClientDisconnect");
        try
        {
            _stateRepository.UpdateClient(client);
            _ = PublishNotificationAsync(new SnapcastClientDisconnectedNotification(client));
        }
        catch (Exception ex)
        {
            LogEventProcessingError("ClientDisconnect", ex);
        }
    }

    private void HandleClientVolumeChanged(ClientSetVolume volumeChange)
    {
        LogProcessingEvent("ClientVolumeChanged");
        try
        {
            // Update the client in our repository
            var client = _stateRepository.GetClient(volumeChange.Id);
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
                _stateRepository.UpdateClient(updatedClient);
            }

            // Convert Params.ClientVolume to Models.ClientVolume for the notification
            var modelVolume = new SnapcastClient.Models.ClientVolume
            {
                Muted = volumeChange.Volume.Muted,
                Percent = volumeChange.Volume.Percent,
            };
            _ = PublishNotificationAsync(new SnapcastClientVolumeChangedNotification(volumeChange.Id, modelVolume));
        }
        catch (Exception ex)
        {
            LogEventProcessingError("ClientVolumeChanged", ex);
        }
    }

    private void HandleClientLatencyChanged(ClientSetLatency latencyChange)
    {
        LogProcessingEvent("ClientLatencyChanged");
        try
        {
            // Update the client in our repository
            var client = _stateRepository.GetClient(latencyChange.Id);
            if (client != null)
            {
                var updatedClient = client.Value with
                {
                    Config = client.Value.Config with { Latency = latencyChange.Latency },
                };
                _stateRepository.UpdateClient(updatedClient);
            }

            _ = PublishNotificationAsync(
                new SnapcastClientLatencyChangedNotification(latencyChange.Id, latencyChange.Latency)
            );
        }
        catch (Exception ex)
        {
            LogEventProcessingError("ClientLatencyChanged", ex);
        }
    }

    private void HandleClientNameChanged(ClientSetName nameChange)
    {
        LogProcessingEvent("ClientNameChanged");
        try
        {
            // Update the client in our repository
            var client = _stateRepository.GetClient(nameChange.Id);
            if (client != null)
            {
                var updatedClient = client.Value with { Config = client.Value.Config with { Name = nameChange.Name } };
                _stateRepository.UpdateClient(updatedClient);
            }

            _ = PublishNotificationAsync(new SnapcastClientNameChangedNotification(nameChange.Id, nameChange.Name));
        }
        catch (Exception ex)
        {
            LogEventProcessingError("ClientNameChanged", ex);
        }
    }

    private void HandleGroupMuteChanged(GroupOnMute muteChange)
    {
        LogProcessingEvent("GroupMuteChanged");
        try
        {
            // Update the group in our repository
            var group = _stateRepository.GetGroup(muteChange.Id);
            if (group != null)
            {
                var updatedGroup = group.Value with { Muted = muteChange.Mute };
                _stateRepository.UpdateGroup(updatedGroup);
            }

            _ = PublishNotificationAsync(new SnapcastGroupMuteChangedNotification(muteChange.Id, muteChange.Mute));
        }
        catch (Exception ex)
        {
            LogEventProcessingError("GroupMuteChanged", ex);
        }
    }

    private void HandleGroupStreamChanged(GroupOnStreamChanged streamChange)
    {
        LogProcessingEvent("GroupStreamChanged");
        try
        {
            // Update the group in our repository
            var group = _stateRepository.GetGroup(streamChange.Id);
            if (group != null)
            {
                var updatedGroup = group.Value with { StreamId = streamChange.StreamId };
                _stateRepository.UpdateGroup(updatedGroup);
            }

            _ = PublishNotificationAsync(
                new SnapcastGroupStreamChangedNotification(streamChange.Id, streamChange.StreamId)
            );
        }
        catch (Exception ex)
        {
            LogEventProcessingError("GroupStreamChanged", ex);
        }
    }

    private void HandleGroupNameChanged(GroupOnNameChanged nameChange)
    {
        LogProcessingEvent("GroupNameChanged");
        try
        {
            // Update the group in our repository
            var group = _stateRepository.GetGroup(nameChange.Id);
            if (group != null)
            {
                var updatedGroup = group.Value with { Name = nameChange.Name };
                _stateRepository.UpdateGroup(updatedGroup);
            }

            _ = PublishNotificationAsync(new SnapcastGroupNameChangedNotification(nameChange.Id, nameChange.Name));
        }
        catch (Exception ex)
        {
            LogEventProcessingError("GroupNameChanged", ex);
        }
    }

    private async Task HandleStreamUpdateAsync(Stream stream)
    {
        LogProcessingEvent("StreamUpdate");
        try
        {
            _stateRepository.UpdateStream(stream);
            await PublishNotificationAsync(new SnapcastStreamUpdatedNotification(stream));
        }
        catch (Exception ex)
        {
            LogEventProcessingError("StreamUpdate", ex);
        }
    }

    private void HandleStreamProperties(StreamOnProperties properties)
    {
        LogProcessingEvent("StreamProperties");
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

            _ = PublishNotificationAsync(
                new SnapcastStreamPropertiesChangedNotification(properties.Id, propertiesDict)
            );
        }
        catch (Exception ex)
        {
            LogEventProcessingError("StreamProperties", ex);
        }
    }

    private void HandleServerUpdate(Server server)
    {
        LogProcessingEvent("ServerUpdate");
        try
        {
            _stateRepository.UpdateServerState(server);
            _ = PublishNotificationAsync(new SnapcastServerUpdatedNotification(server));
        }
        catch (Exception ex)
        {
            LogEventProcessingError("ServerUpdate", ex);
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
                Properties =
                    s.Properties?.Metadata != null
                        ? new Dictionary<string, object>
                        {
                            ["canControl"] = s.Properties?.CanControl ?? false,
                            ["canGoNext"] = s.Properties?.CanGoNext ?? false,
                            ["canGoPrevious"] = s.Properties?.CanGoPrevious ?? false,
                            ["canPause"] = s.Properties?.CanPause ?? false,
                            ["canPlay"] = s.Properties?.CanPlay ?? false,
                            ["canSeek"] = s.Properties?.CanSeek ?? false,
                            ["metadata"] = s.Properties?.Metadata,
                        }.AsReadOnly()
                        : new Dictionary<string, object>().AsReadOnly(),
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
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            // Unsubscribe from events
            if (_snapcastClient != null)
            {
                _snapcastClient.OnClientConnect = null;
                _snapcastClient.OnClientDisconnect = null;
                _snapcastClient.OnClientVolumeChanged = null;
                _snapcastClient.OnClientLatencyChanged = null;
                _snapcastClient.OnClientNameChanged = null;
                _snapcastClient.OnGroupMute = null;
                _snapcastClient.OnGroupStreamChanged = null;
                _snapcastClient.OnGroupNameChanged = null;
                _snapcastClient.OnStreamUpdate = null;
                _snapcastClient.OnStreamProperties = null;
                _snapcastClient.OnServerUpdate = null;

                // Dispose the client if it implements IAsyncDisposable
                if (_snapcastClient is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else if (_snapcastClient is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _operationLock?.Dispose();
            LogServiceDisposed();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SnapcastService disposal");
        }
    }

    #endregion
}
