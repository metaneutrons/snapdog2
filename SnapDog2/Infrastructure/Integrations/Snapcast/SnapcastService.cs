namespace SnapDog2.Infrastructure.Integrations.Snapcast;

using System;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Cortex.Mediator.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using SnapcastClient.Models;
using SnapcastClient.Params;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Helpers;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Snapcast.Notifications;

/// <summary>
/// Enterprise-grade Snapcast service implementation using SnapcastClient library.
/// Provides resilient operations with Polly policies and comprehensive Mediator integration.
/// </summary>
public partial class SnapcastService : ISnapcastService, IAsyncDisposable
{
    private readonly SnapcastConfig _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISnapcastStateRepository _stateRepository;
    private readonly ILogger<SnapcastService> _logger;
    private readonly SnapcastClient.IClient? _snapcastClient;
    private readonly ResiliencePipeline _connectionPolicy;
    private readonly ResiliencePipeline _operationPolicy;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private bool _disposed = false;
    private bool _initialized = false;

    /// <summary>
    /// Helper method to resolve IClientManager in a scoped way to avoid service lifetime issues.
    /// </summary>
    private async Task<IClient?> GetClientBySnapcastIdAsync(string snapcastClientId)
    {
        using var scope = this._serviceProvider.CreateScope();
        var clientManager = scope.ServiceProvider.GetRequiredService<IClientManager>();
        return await clientManager.GetClientBySnapcastIdAsync(snapcastClientId);
    }

    public SnapcastService(
        IOptions<SnapDogConfiguration> configOptions,
        IServiceProvider serviceProvider,
        ISnapcastStateRepository stateRepository,
        ILogger<SnapcastService> logger,
        SnapcastClient.IClient? snapcastClient
    )
    {
        this._config = configOptions.Value.Services.Snapcast;
        this._serviceProvider = serviceProvider;
        this._stateRepository = stateRepository;
        this._logger = logger;
        this._snapcastClient = snapcastClient;

        // Configure resilience policies
        this._connectionPolicy = this.CreateConnectionPolicy();
        this._operationPolicy = this.CreateOperationPolicy();

        this.LogServiceCreated(this._config.Address, this._config.JsonRpcPort, this._config.AutoReconnect);
    }

    /// <inheritdoc />
    public bool IsConnected => this._initialized; // SnapcastClient doesn't expose IsConnected, use initialization status

    /// <inheritdoc />
    public ServiceStatus Status =>
        this._initialized switch
        {
            false => ServiceStatus.Stopped,
            true when this.IsConnected => ServiceStatus.Running,
            true => ServiceStatus.Error,
        };

    #region Logging

    [LoggerMessage(
        1001,
        LogLevel.Information,
        "Snapcast service created for {Host}:{Port}, auto-reconnect: {AutoReconnect}"
    )]
    private partial void LogServiceCreated(string host, int port, bool autoReconnect);

    [LoggerMessage(6002, LogLevel.Information, "🚀 Initializing Snapcast connection to {Host}:{Port}")]
    private partial void LogInitializing(string host, int port);

    [LoggerMessage(6003, LogLevel.Information, "Snapcast connection established successfully")]
    private partial void LogConnectionEstablished();

    [LoggerMessage(6004, LogLevel.Warning, "Snapcast connection lost: {Reason}")]
    private partial void LogConnectionLost(string reason);

    [LoggerMessage(6005, LogLevel.Error, "Failed to initialize Snapcast connection")]
    private partial void LogInitializationFailed(Exception ex);

    [LoggerMessage(6013, LogLevel.Error, "Snapcast connection error: {ErrorMessage}")]
    private partial void LogConnectionErrorMessage(string errorMessage);

    [LoggerMessage(
        1014,
        LogLevel.Information,
        "🚀 Attempting Snapcast connection to {Host}:{Port} (attempt {AttemptNumber}/{MaxAttempts}: {ErrorMessage})"
    )]
    private partial void LogConnectionRetryAttempt(
        string host,
        int port,
        int attemptNumber,
        int maxAttempts,
        string errorMessage
    );

    [LoggerMessage(6006, LogLevel.Error, "Snapcast operation {Operation} failed")]
    private partial void LogOperationFailed(string operation, Exception ex);

    [LoggerMessage(6007, LogLevel.Debug, "Processing Snapcast event: {EventType}")]
    private partial void LogProcessingEvent(string eventType);

    [LoggerMessage(6008, LogLevel.Error, "Error processing Snapcast event {EventType}")]
    private partial void LogEventProcessingError(string eventType, Exception ex);

    [LoggerMessage(6009, LogLevel.Information, "Snapcast service disposed")]
    private partial void LogServiceDisposed();

    [LoggerMessage(6010, LogLevel.Warning, "Snapcast service not connected for operation: {Operation}")]
    private partial void LogNotConnected(string operation);

    [LoggerMessage(
        1011,
        LogLevel.Error,
        "Error handling Snapcast status notification {StatusType} for target {TargetId}"
    )]
    private partial void LogStatusNotificationError(string statusType, string targetId, Exception exception);

    [LoggerMessage(6012, LogLevel.Information, "Snapcast service stopped successfully")]
    private partial void LogServiceStopped();

    [LoggerMessage(6014, LogLevel.Debug, "🔍 Getting server status from Snapcast")]
    private partial void LogGettingServerStatus();

    [LoggerMessage(
        6015,
        LogLevel.Debug,
        "📊 Retrieved server status: {GroupCount} groups, {ClientCount} clients, {StreamCount} streams"
    )]
    private partial void LogServerStatusRetrieved(int groupCount, int clientCount, int streamCount);

    [LoggerMessage(6016, LogLevel.Warning, "⚠️ Failed to get server status: {Error}")]
    private partial void LogServerStatusFailed(string error);

    [LoggerMessage(6017, LogLevel.Debug, "🔍 Updating state repository with server status")]
    private partial void LogUpdatingStateRepository();

    #endregion

    #region Helper Methods

    /// <summary>
    /// Publishes notifications using the injected mediator for better performance and reliability.
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

    /// <summary>
    /// Creates resilience policy for connection operations.
    /// </summary>
    private ResiliencePipeline CreateConnectionPolicy()
    {
        var validatedConfig = ResiliencePolicyFactory.ValidateAndNormalize(this._config.Resilience.Connection);

        var builder = new ResiliencePipelineBuilder();

        // Add retry policy with logging
        if (validatedConfig.MaxRetries > 0)
        {
            builder.AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = validatedConfig.MaxRetries,
                    Delay = TimeSpan.FromMilliseconds(validatedConfig.RetryDelayMs),
                    BackoffType = validatedConfig.BackoffType?.ToLowerInvariant() switch
                    {
                        "linear" => DelayBackoffType.Linear,
                        "constant" => DelayBackoffType.Constant,
                        _ => DelayBackoffType.Exponential,
                    },
                    UseJitter = validatedConfig.UseJitter,
                    // Explicitly handle all exceptions - Snapcast connection issues should be retried
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    OnRetry = args =>
                    {
                        this.LogConnectionRetryAttempt(
                            this._config.Address,
                            this._config.JsonRpcPort,
                            args.AttemptNumber + 1,
                            validatedConfig.MaxRetries + 1,
                            args.Outcome.Exception?.Message ?? "Unknown error"
                        );
                        return ValueTask.CompletedTask;
                    },
                }
            );
        }

        // Add timeout policy
        if (validatedConfig.TimeoutSeconds > 0)
        {
            builder.AddTimeout(TimeSpan.FromSeconds(validatedConfig.TimeoutSeconds));
        }

        return builder.Build();
    }

    /// <summary>
    /// Creates resilience policy for operation calls.
    /// </summary>
    private ResiliencePipeline CreateOperationPolicy()
    {
        var validatedConfig = ResiliencePolicyFactory.ValidateAndNormalize(this._config.Resilience.Operation);
        return ResiliencePolicyFactory.CreatePipeline(validatedConfig, "Snapcast-Operation");
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

                // Log first attempt before Polly execution
                var config = ResiliencePolicyFactory.ValidateAndNormalize(this._config.Resilience.Connection);
                this.LogConnectionRetryAttempt(
                    this._config.Address,
                    this._config.JsonRpcPort,
                    1,
                    config.MaxRetries + 1,
                    "Initial attempt"
                );

                // Use Polly resilience for connection establishment
                var result = await this._connectionPolicy.ExecuteAsync(
                    async (ct) =>
                    {
                        // Check if client is available
                        if (this._snapcastClient == null)
                        {
                            return Result.Failure(
                                "Snapcast client is not available - connection failed during startup"
                            );
                        }

                        // Test the connection by making a simple RPC call
                        await this._snapcastClient!.ServerGetRpcVersionAsync();
                        return Result.Success();
                    },
                    cancellationToken
                );

                if (result.IsSuccess)
                {
                    this._initialized = true;
                    this.LogConnectionEstablished();

                    // Subscribe to events after successful connection
                    this.SubscribeToEvents();

                    // Get initial server status to populate client data
                    var statusResult = await this.GetServerStatusAsync(cancellationToken);
                    if (!statusResult.IsSuccess)
                    {
                        this.LogServerStatusFailed($"Failed to get initial server status: {statusResult.ErrorMessage}");
                    }

                    // Publish connection established notification
                    await this.PublishNotificationAsync(new SnapcastConnectionEstablishedNotification());
                }

                return result;
            }
            finally
            {
                this._operationLock.Release();
            }
        }
        catch (Exception ex)
        {
            // For common Snapcast connection errors, only log the message without stack trace to reduce noise
            if (
                ex is System.Net.Sockets.SocketException
                || ex is TimeoutException
                || ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("refused", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            )
            {
                this.LogConnectionErrorMessage(ex.Message);
            }
            else
            {
                this.LogInitializationFailed(ex);
            }
            return Result.Failure(ex);
        }
    }

    #endregion

    #region Server Operations

    /// <summary>
    /// Checks if the Snapcast client is available and returns an appropriate error if not.
    /// </summary>
    private Result CheckClientAvailability()
    {
        if (this._snapcastClient == null)
        {
            return Result.Failure(
                "Snapcast client is not available - connection failed during startup. Please check server connectivity."
            );
        }
        return Result.Success();
    }

    public async Task<Result<SnapcastServerStatus>> GetServerStatusAsync(CancellationToken cancellationToken = default)
    {
        if (this._disposed)
        {
            return Result<SnapcastServerStatus>.Failure("Service has been disposed");
        }

        // Check if client is available
        var clientCheck = this.CheckClientAvailability();
        if (!clientCheck.IsSuccess)
        {
            return Result<SnapcastServerStatus>.Failure("Snapcast client is not available");
        }

        this.LogGettingServerStatus();

        try
        {
            var serverStatus = await this._snapcastClient!.ServerGetStatusAsync().ConfigureAwait(false);

            this.LogServerStatusRetrieved(
                serverStatus.Groups?.Count ?? 0,
                serverStatus.Groups?.SelectMany(g => g.Clients)?.Count() ?? 0,
                serverStatus.Streams?.Count ?? 0
            );

            this.LogUpdatingStateRepository();

            // Update our state repository with the raw data
            this._stateRepository.UpdateServerState(serverStatus);

            // Map to our domain model
            var mappedStatus = MapToSnapcastServerStatus(serverStatus);

            return Result<SnapcastServerStatus>.Success(mappedStatus);
        }
        catch (Exception ex)
        {
            this.LogServerStatusFailed(ex.Message);
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
            var version = await this._snapcastClient!.ServerGetRpcVersionAsync().ConfigureAwait(false);

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
        string snapcastClientIndex,
        int volumePercent,
        CancellationToken cancellationToken = default
    )
    {
        if (this._disposed)
        {
            return Result.Failure("Service has been disposed");
        }

        if (!this.IsConnected)
        {
            this.LogNotConnected(nameof(this.SetClientVolumeAsync));
            return Result.Failure("Snapcast service is not connected");
        }

        try
        {
            return await this._operationPolicy.ExecuteAsync(
                async (ct) =>
                {
                    await this._snapcastClient!.ClientSetVolumeAsync(snapcastClientIndex, volumePercent);
                    return Result.Success();
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            this.LogOperationFailed(nameof(this.SetClientVolumeAsync), ex);
            return Result.Failure($"Failed to set client volume: {ex.Message}");
        }
    }

    public async Task<Result> SetClientMuteAsync(
        string snapcastClientIndex,
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
            var client = this._stateRepository.GetClient(snapcastClientIndex);
            if (client == null)
            {
                return Result.Failure($"Client {snapcastClientIndex} not found");
            }

            var currentVolume = client.Value.Config.Volume.Percent;
            var newVolume = muted ? 0 : currentVolume;

            await this._snapcastClient!.ClientSetVolumeAsync(snapcastClientIndex, newVolume).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed(nameof(this.SetClientMuteAsync), ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> SetClientLatencyAsync(
        string snapcastClientIndex,
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
            await this._snapcastClient!.ClientSetLatencyAsync(snapcastClientIndex, latencyMs).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed(nameof(this.SetClientLatencyAsync), ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> SetClientNameAsync(
        string snapcastClientIndex,
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
            await this._snapcastClient!.ClientSetNameAsync(snapcastClientIndex, name).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed(nameof(this.SetClientNameAsync), ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> SetClientGroupAsync(
        string snapcastClientIndex,
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
            await this._snapcastClient!.GroupSetClientsAsync(groupId, new List<string> { snapcastClientIndex })
                .ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed(nameof(this.SetClientGroupAsync), ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> DeleteClientAsync(
        string snapcastClientIndex,
        CancellationToken cancellationToken = default
    )
    {
        if (this._disposed)
        {
            return Result.Failure("Service has been disposed");
        }

        try
        {
            await this._snapcastClient!.ServerDeleteClientAsync(snapcastClientIndex).ConfigureAwait(false);
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
            await this._snapcastClient!.GroupSetMuteAsync(groupId, muted).ConfigureAwait(false);
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

        this._logger.LogDebug("Setting group {GroupId} to stream {StreamId}", groupId, streamId);

        try
        {
            await this._snapcastClient!.GroupSetStreamAsync(groupId, streamId).ConfigureAwait(false);
            this._logger.LogDebug("Successfully set group {GroupId} to stream {StreamId}", groupId, streamId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to set group {GroupId} to stream {StreamId}", groupId, streamId);
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
            await this._snapcastClient!.GroupSetNameAsync(groupId, name).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed(nameof(this.SetGroupNameAsync), ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> SetGroupClientsAsync(
        string groupId,
        IEnumerable<string> clientIds,
        CancellationToken cancellationToken = default
    )
    {
        if (this._disposed)
        {
            return Result.Failure("Service has been disposed");
        }

        try
        {
            var clientIdList = clientIds.ToList();
            this._logger.LogDebug(
                "Setting group {GroupId} clients to: {ClientIds}",
                groupId,
                string.Join(", ", clientIdList)
            );

            // Use the Snapcast client library to set group clients
            await this._snapcastClient!.GroupSetClientsAsync(groupId, clientIdList).ConfigureAwait(false);

            this._logger.LogInformation(
                "Successfully set {ClientCount} clients for group {GroupId}",
                clientIdList.Count,
                groupId
            );
            return Result.Success();
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to set clients for group {GroupId}", groupId);
            this.LogOperationFailed(nameof(this.SetGroupClientsAsync), ex);
            return Result.Failure(ex);
        }
    }

    public Task<Result<string>> CreateGroupAsync(
        IEnumerable<string> clientIndexs,
        CancellationToken cancellationToken = default
    )
    {
        if (this._disposed)
        {
            return Task.FromResult(Result<string>.Failure("Service has been disposed"));
        }

        try
        {
            var clientIndexArray = clientIndexs.ToArray();
            if (clientIndexArray.Length == 0)
            {
                return Task.FromResult(Result<string>.Failure("At least one client ID is required"));
            }

            // For now, we'll use the first client's current group as a template
            // In a real implementation, you might want to create a new group ID
            var firstClient = this._stateRepository.GetClient(clientIndexArray[0]);
            if (firstClient == null)
            {
                return Task.FromResult(Result<string>.Failure($"Client {clientIndexArray[0]} not found"));
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
        if (this._snapcastClient == null)
        {
            this._logger.LogWarning("Cannot subscribe to Snapcast events - client is not available");
            return;
        }

        this._snapcastClient!.OnClientConnect = this.HandleClientConnect;
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

            // Bridge to IClient status notification
            _ = this.BridgeClientConnectionStatusAsync(client.Id, true);
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

            // Bridge to IClient status notification
            _ = this.BridgeClientConnectionStatusAsync(client.Id, false);
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

            // Bridge to IClient status notifications
            _ = this.BridgeClientVolumeStatusAsync(volumeChange.Id, volumeChange.Volume.Percent);
            _ = this.BridgeClientMuteStatusAsync(volumeChange.Id, volumeChange.Volume.Muted);
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

            // Bridge to IClient status notification
            _ = this.BridgeClientLatencyStatusAsync(latencyChange.Id, latencyChange.Latency);
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

    private void UnsubscribeFromEvents()
    {
        if (this._snapcastClient != null)
        {
            this._snapcastClient!.OnClientConnect = null;
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
        }
    }

    #region Event Bridge Methods

    /// <summary>
    /// Bridges Snapcast client connection events to IClient status notifications.
    /// </summary>
    private async Task BridgeClientConnectionStatusAsync(string snapcastClientId, bool isConnected)
    {
        try
        {
            var client = await this.GetClientBySnapcastIdAsync(snapcastClientId);
            if (client != null)
            {
                await client.PublishConnectionStatusAsync(isConnected);
                this.LogEventBridged("ClientConnection", snapcastClientId, client.Id);
            }
            else
            {
                this.LogClientNotFoundForBridge(snapcastClientId, "connection");
            }
        }
        catch (ObjectDisposedException)
        {
            // Service provider has been disposed (likely during shutdown) - ignore this event
            // This is expected behavior during application shutdown
        }
        catch (Exception ex)
        {
            this.LogEventBridgeError("ClientConnection", snapcastClientId, ex);
        }
    }

    /// <summary>
    /// Bridges Snapcast client volume events to IClient status notifications.
    /// </summary>
    private async Task BridgeClientVolumeStatusAsync(string snapcastClientId, int volume)
    {
        try
        {
            var client = await this.GetClientBySnapcastIdAsync(snapcastClientId);
            if (client != null)
            {
                await client.PublishVolumeStatusAsync(volume);
                this.LogEventBridged("ClientVolume", snapcastClientId, client.Id);
            }
            else
            {
                this.LogClientNotFoundForBridge(snapcastClientId, "volume");
            }
        }
        catch (ObjectDisposedException)
        {
            // Service provider has been disposed (likely during shutdown) - ignore this event
            // This is expected behavior during application shutdown
        }
        catch (Exception ex)
        {
            this.LogEventBridgeError("ClientVolume", snapcastClientId, ex);
        }
    }

    /// <summary>
    /// Bridges Snapcast client mute events to IClient status notifications.
    /// </summary>
    private async Task BridgeClientMuteStatusAsync(string snapcastClientId, bool muted)
    {
        try
        {
            var client = await this.GetClientBySnapcastIdAsync(snapcastClientId);
            if (client != null)
            {
                await client.PublishMuteStatusAsync(muted);
                this.LogEventBridged("ClientMute", snapcastClientId, client.Id);
            }
            else
            {
                this.LogClientNotFoundForBridge(snapcastClientId, "mute");
            }
        }
        catch (ObjectDisposedException)
        {
            // Service provider has been disposed (likely during shutdown) - ignore this event
            // This is expected behavior during application shutdown
        }
        catch (Exception ex)
        {
            this.LogEventBridgeError("ClientMute", snapcastClientId, ex);
        }
    }

    /// <summary>
    /// Bridges Snapcast client latency events to IClient status notifications.
    /// </summary>
    private async Task BridgeClientLatencyStatusAsync(string snapcastClientId, int latencyMs)
    {
        try
        {
            var client = await this.GetClientBySnapcastIdAsync(snapcastClientId);
            if (client != null)
            {
                await client.PublishLatencyStatusAsync(latencyMs);
                this.LogEventBridged("ClientLatency", snapcastClientId, client.Id);
            }
            else
            {
                this.LogClientNotFoundForBridge(snapcastClientId, "latency");
            }
        }
        catch (ObjectDisposedException)
        {
            // Service provider has been disposed (likely during shutdown) - ignore this event
            // This is expected behavior during application shutdown
        }
        catch (Exception ex)
        {
            this.LogEventBridgeError("ClientLatency", snapcastClientId, ex);
        }
    }

    [LoggerMessage(
        6020,
        LogLevel.Debug,
        "Bridged {EventType} event from Snapcast client {SnapcastClientId} to IClient {ClientId}"
    )]
    private partial void LogEventBridged(string eventType, string snapcastClientId, int clientId);

    [LoggerMessage(
        6021,
        LogLevel.Warning,
        "Client not found for bridging {EventType} event from Snapcast client {SnapcastClientId}"
    )]
    private partial void LogClientNotFoundForBridge(string snapcastClientId, string eventType);

    [LoggerMessage(6022, LogLevel.Error, "Error bridging {EventType} event from Snapcast client {SnapcastClientId}")]
    private partial void LogEventBridgeError(string eventType, string snapcastClientId, Exception ex);

    #endregion

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
                this._snapcastClient!.OnClientConnect = null;
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
            this.LogErrorDuringSnapcastServiceDisposal(ex);
        }
    }

    #endregion
}
