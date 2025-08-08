namespace SnapDog2.Infrastructure.Services;

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Cortex.Mediator.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Extensions;
using SnapDog2.Core.Helpers;
using SnapDog2.Core.Models;

/// <summary>
/// Enterprise-grade MQTT service implementation using MQTTnet v5.
/// Provides bi-directional MQTT communication with Polly resilience policies,
/// configurable topic structure, and comprehensive error handling.
/// </summary>
public sealed partial class MqttService : IMqttService, IAsyncDisposable
{
    private readonly MqttConfig _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MqttService> _logger;
    private readonly List<ZoneConfig> _zoneConfigs;
    private readonly List<ClientConfig> _clientConfigs;
    private readonly ResiliencePipeline _connectionPolicy;
    private readonly ResiliencePipeline _operationPolicy;

    private readonly ConcurrentDictionary<int, ZoneMqttTopics> _zoneTopics = new();
    private readonly ConcurrentDictionary<string, ClientMqttTopics> _clientTopics = new();

    private IMqttClient? _mqttClient;
    private bool _initialized = false;
    private bool _disposed = false;

    public MqttService(
        IOptions<SnapDogConfiguration> configOptions,
        IOptions<List<ZoneConfig>> zoneConfigOptions,
        IOptions<List<ClientConfig>> clientConfigOptions,
        IServiceProvider serviceProvider,
        ILogger<MqttService> logger
    )
    {
        this._config = configOptions.Value.Services.Mqtt;
        this._serviceProvider = serviceProvider;
        this._logger = logger;
        this._zoneConfigs = zoneConfigOptions.Value;
        this._clientConfigs = clientConfigOptions.Value;

        // Configure resilience policies
        this._connectionPolicy = CreateConnectionPolicy();
        this._operationPolicy = CreateOperationPolicy();

        // Build topic configurations
        this.BuildTopicConfigurations();

        LogServiceCreated(_config.BrokerAddress, _config.Port, _config.Enabled);
    }

    #region Logging

    [LoggerMessage(7001, LogLevel.Information, "MQTT service created for {BrokerAddress}:{Port}, enabled: {Enabled}")]
    private partial void LogServiceCreated(string brokerAddress, int port, bool enabled);

    [LoggerMessage(7002, LogLevel.Information, "Initializing MQTT connection to {BrokerAddress}:{Port}")]
    private partial void LogInitializing(string brokerAddress, int port);

    [LoggerMessage(7003, LogLevel.Information, "MQTT connection established successfully")]
    private partial void LogConnectionEstablished();

    [LoggerMessage(7004, LogLevel.Warning, "MQTT connection lost: {Reason}")]
    private partial void LogConnectionLost(string reason);

    [LoggerMessage(7005, LogLevel.Error, "Failed to initialize MQTT connection")]
    private partial void LogInitializationFailed(Exception ex);

    [LoggerMessage(7009, LogLevel.Error, "MQTT connection error: {ErrorMessage}")]
    private partial void LogConnectionErrorMessage(string errorMessage);

    [LoggerMessage(7006, LogLevel.Error, "MQTT operation {Operation} failed")]
    private partial void LogOperationFailed(string operation, Exception ex);

    [LoggerMessage(7007, LogLevel.Information, "MQTT service disposed")]
    private partial void LogServiceDisposed();

    [LoggerMessage(7008, LogLevel.Warning, "MQTT service not connected for operation: {Operation}")]
    private partial void LogNotConnected(string operation);

    [LoggerMessage(
        2010,
        LogLevel.Information,
        "🚀 Attempting MQTT connection to {BrokerAddress}:{Port} (attempt {AttemptNumber}/{MaxAttempts}: {ErrorMessage})"
    )]
    private partial void LogConnectionRetryAttempt(
        string brokerAddress,
        int port,
        int attemptNumber,
        int maxAttempts,
        string errorMessage
    );

    #endregion

    #region Properties

    public bool IsConnected => this._mqttClient?.IsConnected ?? false;

    public event EventHandler? Connected;
    public event EventHandler<string>? Disconnected;
    public event EventHandler<MqttMessageReceivedEventArgs>? MessageReceived;

    #endregion

    #region Helper Methods

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
            this.LogOperationFailed("PublishNotification", ex);
        }
    }

    /// <summary>
    /// Creates resilience policy for connection operations.
    /// </summary>
    private ResiliencePipeline CreateConnectionPolicy()
    {
        var validatedConfig = ResiliencePolicyFactory.ValidateAndNormalize(_config.Resilience.Connection);

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
                    // Explicitly handle all exceptions - MQTT connection issues should be retried
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    OnRetry = args =>
                    {
                        LogConnectionRetryAttempt(
                            _config.BrokerAddress,
                            _config.Port,
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
        var validatedConfig = ResiliencePolicyFactory.ValidateAndNormalize(_config.Resilience.Operation);
        return ResiliencePolicyFactory.CreatePipeline(validatedConfig, "MQTT-Operation");
    }

    private void BuildTopicConfigurations()
    {
        // Build zone topic configurations (1-based indexing)
        for (var i = 0; i < this._zoneConfigs.Count; i++)
        {
            var zoneConfig = this._zoneConfigs[i];
            var zoneId = i + 1; // 1-based indexing
            var topics = zoneConfig.BuildMqttTopics();
            this._zoneTopics.TryAdd(zoneId, topics);
        }

        // Build client topic configurations
        for (var i = 0; i < this._clientConfigs.Count; i++)
        {
            var clientConfig = this._clientConfigs[i];
            var clientId = $"client_{i + 1}"; // Use client_1, client_2, etc.
            var topics = clientConfig.BuildMqttTopics();
            this._clientTopics.TryAdd(clientId, topics);
        }
    }

    #endregion

    #region Interface Implementation

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

        if (!this._config.Enabled)
        {
            this._logger.LogInformation("MQTT integration is disabled");
            return Result.Success();
        }

        try
        {
            this.LogInitializing(this._config.BrokerAddress, this._config.Port);

            // Log first attempt before Polly execution
            var config = ResiliencePolicyFactory.ValidateAndNormalize(_config.Resilience.Connection);
            LogConnectionRetryAttempt(
                this._config.BrokerAddress,
                this._config.Port,
                1,
                config.MaxRetries + 1,
                "Initial attempt"
            );

            var result = await _connectionPolicy.ExecuteAsync(
                async (ct) =>
                {
                    // Create MQTT client using v5 API
                    var factory = new MqttClientFactory();
                    this._mqttClient = factory.CreateMqttClient();

                    // Configure client options
                    var optionsBuilder = new MqttClientOptionsBuilder()
                        .WithTcpServer(this._config.BrokerAddress, this._config.Port)
                        .WithClientId(this._config.ClientId)
                        .WithKeepAlivePeriod(TimeSpan.FromSeconds(this._config.KeepAlive))
                        .WithCleanSession(true);

                    // Add authentication if configured
                    if (!string.IsNullOrEmpty(this._config.Username))
                    {
                        optionsBuilder.WithCredentials(this._config.Username, this._config.Password);
                    }

                    var options = optionsBuilder.Build();

                    // Set up event handlers
                    this._mqttClient.ConnectedAsync += this.OnConnectedAsync;
                    this._mqttClient.DisconnectedAsync += this.OnDisconnectedAsync;
                    this._mqttClient.ApplicationMessageReceivedAsync += this.OnApplicationMessageReceivedAsync;

                    // Connect to broker
                    await this._mqttClient.ConnectAsync(options, ct);

                    this._initialized = true;
                    this.LogConnectionEstablished();

                    return Result.Success();
                },
                cancellationToken
            );

            if (result.IsSuccess)
            {
                // Publish connection established notification
                await this.PublishNotificationAsync(new MqttConnectionEstablishedNotification());
            }

            return result;
        }
        catch (Exception ex)
        {
            // For common MQTT connection errors, only log the message without stack trace to reduce noise
            if (
                ex is System.Net.Sockets.SocketException
                || ex is TimeoutException
                || ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("broker", StringComparison.OrdinalIgnoreCase)
            )
            {
                LogConnectionErrorMessage(ex.Message);
            }
            else
            {
                LogInitializationFailed(ex);
            }
            return Result.Failure($"Failed to initialize MQTT connection: {ex.Message}");
        }
    }

    public async Task<Result> PublishZoneStateAsync(
        int zoneId,
        ZoneState state,
        CancellationToken cancellationToken = default
    )
    {
        if (!this.IsConnected)
        {
            return Result.Failure("MQTT client is not connected");
        }

        if (!this._zoneTopics.TryGetValue(zoneId, out var topics))
        {
            return Result.Failure($"Zone {zoneId} not configured for MQTT");
        }

        try
        {
            // Publish comprehensive state as JSON
            var stateJson = JsonSerializer.Serialize(
                state,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );
            await this.PublishAsync(topics.Status.State, stateJson, true, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed("PublishZoneState", ex);
            return Result.Failure($"Failed to publish zone state: {ex.Message}");
        }
    }

    public async Task<Result> PublishClientStateAsync(
        string clientId,
        ClientState state,
        CancellationToken cancellationToken = default
    )
    {
        if (!this.IsConnected)
        {
            return Result.Failure("MQTT client is not connected");
        }

        if (!this._clientTopics.TryGetValue(clientId, out var topics))
        {
            return Result.Failure($"Client {clientId} not configured for MQTT");
        }

        try
        {
            // Publish comprehensive state as JSON
            var stateJson = JsonSerializer.Serialize(
                state,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );
            await this.PublishAsync(topics.Status.State, stateJson, true, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed("PublishClientState", ex);
            return Result.Failure($"Failed to publish client state: {ex.Message}");
        }
    }

    public async Task<Result> PublishAsync(
        string topic,
        string payload,
        bool retain = false,
        CancellationToken cancellationToken = default
    )
    {
        if (!this.IsConnected)
        {
            LogNotConnected(nameof(PublishAsync));
            return Result.Failure("MQTT client is not connected");
        }

        try
        {
            return await _operationPolicy.ExecuteAsync(
                async (ct) =>
                {
                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithPayload(payload)
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .WithRetainFlag(retain)
                        .Build();

                    await this._mqttClient!.PublishAsync(message, ct);
                    return Result.Success();
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            this.LogOperationFailed("Publish", ex);
            return Result.Failure($"Failed to publish message: {ex.Message}");
        }
    }

    public async Task<Result> SubscribeAsync(IEnumerable<string> topics, CancellationToken cancellationToken = default)
    {
        if (!this.IsConnected)
        {
            return Result.Failure("MQTT client is not connected");
        }

        try
        {
            foreach (var topic in topics)
            {
                await this._mqttClient!.SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce, cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed("Subscribe", ex);
            return Result.Failure($"Failed to subscribe to topics: {ex.Message}");
        }
    }

    public async Task<Result> UnsubscribeAsync(
        IEnumerable<string> topics,
        CancellationToken cancellationToken = default
    )
    {
        if (!this.IsConnected)
        {
            return Result.Failure("MQTT client is not connected");
        }

        try
        {
            foreach (var topic in topics)
            {
                await this._mqttClient!.UnsubscribeAsync(topic, cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed("Unsubscribe", ex);
            return Result.Failure($"Failed to unsubscribe from topics: {ex.Message}");
        }
    }

    #endregion

    #region Event Handlers

    private async Task OnConnectedAsync(MqttClientConnectedEventArgs args)
    {
        this.Connected?.Invoke(this, EventArgs.Empty);
        await this.PublishNotificationAsync(new MqttConnectionEstablishedNotification());
    }

    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        var reason = args.Reason.ToString();
        this.LogConnectionLost(reason);
        this.Disconnected?.Invoke(this, reason);
        await this.PublishNotificationAsync(new MqttConnectionLostNotification(reason));
    }

    private async Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            var topic = args.ApplicationMessage.Topic;
            var payload =
                args.ApplicationMessage.Payload.Length > 0
                    ? Encoding.UTF8.GetString(
                        args.ApplicationMessage.Payload.IsSingleSegment
                            ? args.ApplicationMessage.Payload.FirstSpan
                            : args.ApplicationMessage.Payload.ToArray()
                    )
                    : string.Empty;

            var eventArgs = new MqttMessageReceivedEventArgs
            {
                Topic = topic,
                Payload = payload,
                Retained = args.ApplicationMessage.Retain,
                QoS = (int)args.ApplicationMessage.QualityOfServiceLevel,
            };
            this.MessageReceived?.Invoke(this, eventArgs);

            // Process the message and convert to command (placeholder for future implementation)
            await this.ProcessIncomingMessageAsync(topic, payload);
        }
        catch (Exception ex)
        {
            this.LogOperationFailed("ProcessMessage", ex);
        }
    }

    private async Task ProcessIncomingMessageAsync(string topic, string payload)
    {
        try
        {
            this._logger.LogDebug("Processing MQTT message on topic {Topic}: {Payload}", topic, payload);

            // Map topic to command and execute via Mediator
            var command = this.MapTopicToCommand(topic, payload);
            if (command != null)
            {
                using var scope = this._serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<Cortex.Mediator.IMediator>();

                // Send the command - the mediator will handle the type resolution
                switch (command)
                {
                    case SnapDog2.Server.Features.Zones.Commands.SetZoneVolumeCommand zoneVolumeCmd:
                        await mediator.SendCommandAsync<
                            SnapDog2.Server.Features.Zones.Commands.SetZoneVolumeCommand,
                            Result
                        >(zoneVolumeCmd);
                        break;
                    case SnapDog2.Server.Features.Zones.Commands.SetZoneMuteCommand zoneMuteCmd:
                        await mediator.SendCommandAsync<
                            SnapDog2.Server.Features.Zones.Commands.SetZoneMuteCommand,
                            Result
                        >(zoneMuteCmd);
                        break;
                    case SnapDog2.Server.Features.Clients.Commands.SetClientVolumeCommand clientVolumeCmd:
                        await mediator.SendCommandAsync<
                            SnapDog2.Server.Features.Clients.Commands.SetClientVolumeCommand,
                            Result
                        >(clientVolumeCmd);
                        break;
                    case SnapDog2.Server.Features.Clients.Commands.SetClientMuteCommand clientMuteCmd:
                        await mediator.SendCommandAsync<
                            SnapDog2.Server.Features.Clients.Commands.SetClientMuteCommand,
                            Result
                        >(clientMuteCmd);
                        break;
                    case SnapDog2.Server.Features.Clients.Commands.SetClientLatencyCommand clientLatencyCmd:
                        await mediator.SendCommandAsync<
                            SnapDog2.Server.Features.Clients.Commands.SetClientLatencyCommand,
                            Result
                        >(clientLatencyCmd);
                        break;
                    default:
                        this._logger.LogWarning("Unknown command type: {CommandType}", command.GetType().Name);
                        break;
                }

                this._logger.LogDebug("Successfully processed MQTT command for topic {Topic}", topic);
            }
            else
            {
                this._logger.LogDebug("No command mapping found for topic {Topic}", topic);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to process MQTT message on topic {Topic}", topic);
        }
    }

    /// <summary>
    /// Maps MQTT topics to Cortex.Mediator commands based on the topic structure.
    /// </summary>
    private object? MapTopicToCommand(string topic, string payload)
    {
        try
        {
            // Parse the topic structure: snapdog/{zone|client}/{id}/{command}
            var topicParts = topic.Split('/');
            if (topicParts.Length < 4 || !topicParts[0].Equals("snapdog", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var entityType = topicParts[1].ToLowerInvariant();
            var entityId = topicParts[2];
            var command = topicParts[3].ToLowerInvariant();

            // Handle zone commands
            if (entityType == "zone" && int.TryParse(entityId, out var zoneId))
            {
                return this.MapZoneCommand(zoneId, command, payload);
            }

            // Handle client commands
            if (entityType == "client")
            {
                return this.MapClientCommand(entityId, command, payload);
            }

            return null;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to map topic {Topic} to command", topic);
            return null;
        }
    }

    /// <summary>
    /// Maps zone-specific MQTT commands to Mediator commands.
    /// </summary>
    private object? MapZoneCommand(int zoneId, string command, string payload)
    {
        // Import zone command types
        using var scope = this._serviceProvider.CreateScope();

        return command switch
        {
            // TODO: Implement these zone commands
            // "play" => new SnapDog2.Server.Features.Zones.Commands.PlayZoneCommand { ZoneId = zoneId },
            // "pause" => new SnapDog2.Server.Features.Zones.Commands.PauseZoneCommand { ZoneId = zoneId },
            // "stop" => new SnapDog2.Server.Features.Zones.Commands.StopZoneCommand { ZoneId = zoneId },
            "volume" when int.TryParse(payload, out var volume) =>
                new SnapDog2.Server.Features.Zones.Commands.SetZoneVolumeCommand { ZoneId = zoneId, Volume = volume },
            "mute" when bool.TryParse(payload, out var mute) =>
                new SnapDog2.Server.Features.Zones.Commands.SetZoneMuteCommand { ZoneId = zoneId, Enabled = mute },
            // "track" when int.TryParse(payload, out var trackIndex) =>
            //     new SnapDog2.Server.Features.Zones.Commands.SetZoneTrackCommand { ZoneId = zoneId, TrackIndex = trackIndex },
            // "playlist" when int.TryParse(payload, out var playlistIndex) =>
            //     new SnapDog2.Server.Features.Zones.Commands.SetZonePlaylistCommand { ZoneId = zoneId, PlaylistIndex = playlistIndex },
            // "next" => new SnapDog2.Server.Features.Zones.Commands.NextZoneTrackCommand { ZoneId = zoneId },
            // "previous" => new SnapDog2.Server.Features.Zones.Commands.PreviousZoneTrackCommand { ZoneId = zoneId },
            _ => null,
        };
    }

    /// <summary>
    /// Maps client-specific MQTT commands to Mediator commands.
    /// </summary>
    private object? MapClientCommand(string clientId, string command, string payload)
    {
        return command switch
        {
            "volume" when int.TryParse(payload, out var volume) && int.TryParse(clientId, out var clientIdInt) =>
                new SnapDog2.Server.Features.Clients.Commands.SetClientVolumeCommand
                {
                    ClientId = clientIdInt,
                    Volume = volume,
                },
            "mute" when bool.TryParse(payload, out var mute) && int.TryParse(clientId, out var clientIdInt2) =>
                new SnapDog2.Server.Features.Clients.Commands.SetClientMuteCommand
                {
                    ClientId = clientIdInt2,
                    Enabled = mute,
                },
            "latency" when int.TryParse(payload, out var latency) && int.TryParse(clientId, out var clientIdInt3) =>
                new SnapDog2.Server.Features.Clients.Commands.SetClientLatencyCommand
                {
                    ClientId = clientIdInt3,
                    LatencyMs = latency,
                },
            _ => null,
        };
    }

    /// <summary>
    /// Publishes client status updates to MQTT topics.
    /// </summary>
    public async Task<Result> PublishClientStatusAsync<T>(
        string clientId,
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (!this.IsConnected)
            {
                return Result.Failure("MQTT client is not connected");
            }

            // Get client topics for this client
            if (!this._clientTopics.TryGetValue(clientId, out var topics))
            {
                this._logger.LogWarning("No MQTT topics configured for client {ClientId}", clientId);
                return Result.Success();
            }

            // Map event type to specific topic
            var topic = eventType.ToUpperInvariant() switch
            {
                "CLIENT_VOLUME" => topics.Status.Volume,
                "CLIENT_MUTE" => topics.Status.Mute,
                "CLIENT_LATENCY" => topics.Status.Latency,
                "CLIENT_CONNECTION" => topics.Status.Connected,
                "CLIENT_ZONE_ASSIGNMENT" => topics.Status.Zone,
                "CLIENT_STATE" => topics.Status.State,
                _ => null,
            };

            if (topic == null)
            {
                this._logger.LogDebug("No MQTT topic mapping for event type {EventType}", eventType);
                return Result.Success();
            }

            // Serialize payload
            var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);

            // Publish to MQTT
            return await this.PublishAsync(topic, jsonPayload, retain: true, cancellationToken);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to publish client status {EventType} for client {ClientId}",
                eventType,
                clientId
            );
            return Result.Failure($"Failed to publish client status: {ex.Message}");
        }
    }

    /// <summary>
    /// Publishes zone status updates to MQTT topics.
    /// </summary>
    public async Task<Result> PublishZoneStatusAsync<T>(
        int zoneId,
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (!this.IsConnected)
            {
                return Result.Failure("MQTT client is not connected");
            }

            // Get zone topics for this zone
            if (!this._zoneTopics.TryGetValue(zoneId, out var topics))
            {
                this._logger.LogWarning("No MQTT topics configured for zone {ZoneId}", zoneId);
                return Result.Success();
            }

            // Map event type to specific topic
            var topic = eventType.ToUpperInvariant() switch
            {
                "ZONE_VOLUME" => topics.Status.Volume,
                "ZONE_MUTE" => topics.Status.Mute,
                "ZONE_PLAYBACK_STATE" => topics.Status.Control,
                "ZONE_TRACK" => topics.Status.TrackInfo,
                "ZONE_PLAYLIST" => topics.Status.PlaylistInfo,
                "ZONE_REPEAT_MODE" => topics.Status.PlaylistRepeat,
                "ZONE_SHUFFLE_MODE" => topics.Status.PlaylistShuffle,
                "ZONE_STATE" => topics.Status.State,
                _ => null,
            };

            if (topic == null)
            {
                this._logger.LogDebug("No MQTT topic mapping for event type {EventType}", eventType);
                return Result.Success();
            }

            // Serialize payload
            var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);

            // Publish to MQTT
            return await this.PublishAsync(topic, jsonPayload, retain: true, cancellationToken);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to publish zone status {EventType} for zone {ZoneId}", eventType, zoneId);
            return Result.Failure($"Failed to publish zone status: {ex.Message}");
        }
    }

    #endregion

    #region Disposal

    public async ValueTask DisposeAsync()
    {
        if (this._disposed)
        {
            return;
        }

        try
        {
            if (this._mqttClient != null)
            {
                if (this._mqttClient.IsConnected)
                {
                    await this._mqttClient.DisconnectAsync();
                }

                this._mqttClient.ConnectedAsync -= this.OnConnectedAsync;
                this._mqttClient.DisconnectedAsync -= this.OnDisconnectedAsync;
                this._mqttClient.ApplicationMessageReceivedAsync -= this.OnApplicationMessageReceivedAsync;

                this._mqttClient.Dispose();
            }

            this.LogServiceDisposed();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed("Dispose", ex);
        }
        finally
        {
            this._disposed = true;
        }
    }

    #endregion
}

/// <summary>
/// Notification published when MQTT connection is established.
/// </summary>
public record MqttConnectionEstablishedNotification : INotification;

/// <summary>
/// Notification published when MQTT connection is lost.
/// </summary>
/// <param name="Reason">The reason for connection loss.</param>
public record MqttConnectionLostNotification(string Reason) : INotification;
