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

    [LoggerMessage(2001, LogLevel.Information, "MQTT service created for {BrokerAddress}:{Port}, enabled: {Enabled}")]
    private partial void LogServiceCreated(string brokerAddress, int port, bool enabled);

    [LoggerMessage(2002, LogLevel.Information, "Initializing MQTT connection to {BrokerAddress}:{Port}")]
    private partial void LogInitializing(string brokerAddress, int port);

    [LoggerMessage(2003, LogLevel.Information, "MQTT connection established successfully")]
    private partial void LogConnectionEstablished();

    [LoggerMessage(2004, LogLevel.Warning, "MQTT connection lost: {Reason}")]
    private partial void LogConnectionLost(string reason);

    [LoggerMessage(2005, LogLevel.Error, "Failed to initialize MQTT connection")]
    private partial void LogInitializationFailed(Exception ex);

    [LoggerMessage(2006, LogLevel.Error, "MQTT operation {Operation} failed")]
    private partial void LogOperationFailed(string operation, Exception ex);

    [LoggerMessage(2007, LogLevel.Information, "MQTT service disposed")]
    private partial void LogServiceDisposed();

    [LoggerMessage(2008, LogLevel.Warning, "MQTT service not connected for operation: {Operation}")]
    private partial void LogNotConnected(string operation);

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
        return new ResiliencePipelineBuilder()
            .AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(2),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                }
            )
            .AddTimeout(TimeSpan.FromSeconds(30)) // Default 30 second timeout
            .Build();
    }

    /// <summary>
    /// Creates resilience policy for operation calls.
    /// </summary>
    private ResiliencePipeline CreateOperationPolicy()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromMilliseconds(500),
                    BackoffType = DelayBackoffType.Linear,
                }
            )
            .AddTimeout(TimeSpan.FromSeconds(10))
            .Build();
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
            this.LogInitializationFailed(ex);
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
        this.LogConnectionEstablished();
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
        // TODO: Implement topic-to-command mapping
        // This will map MQTT topics to Cortex.Mediator commands
        // For now, just log the received message
        this._logger.LogDebug("Received MQTT message on topic {Topic}: {Payload}", topic, payload);

        // Placeholder await to satisfy async pattern - will be replaced with actual command processing
        await Task.CompletedTask;

        // Example of how this would work:
        // var command = MapTopicToCommand(topic, payload);
        // if (command != null)
        // {
        //     using var scope = _serviceProvider.CreateScope();
        //     var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        //     await mediator.SendAsync(command);
        // }
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
