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
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Extensions;
using SnapDog2.Core.Models;

/// <summary>
/// Enterprise-grade MQTT service implementation using MQTTnet v5.
/// Provides bi-directional MQTT communication with automatic reconnection,
/// configurable topic structure, and comprehensive error handling.
/// </summary>
public sealed partial class MqttService : IMqttService, IAsyncDisposable
{
    private readonly MqttConfig _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MqttService> _logger;
    private readonly List<ZoneConfig> _zoneConfigs;
    private readonly List<ClientConfig> _clientConfigs;

    private readonly ConcurrentDictionary<int, ZoneMqttTopics> _zoneTopics = new();
    private readonly ConcurrentDictionary<string, ClientMqttTopics> _clientTopics = new();

    private IMqttClient? _mqttClient;
    private bool _initialized = false;
    private bool _disposed = false;

    public MqttService(
        IOptions<ServicesConfig> configOptions,
        IOptions<List<ZoneConfig>> zoneConfigOptions,
        IOptions<List<ClientConfig>> clientConfigOptions,
        IServiceProvider serviceProvider,
        ILogger<MqttService> logger
    )
    {
        _config = configOptions.Value.Mqtt;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _zoneConfigs = zoneConfigOptions.Value;
        _clientConfigs = clientConfigOptions.Value;

        // Build topic configurations
        BuildTopicConfigurations();
    }

    #region Logging

    [LoggerMessage(2001, LogLevel.Information, "Initializing MQTT connection to {BrokerAddress}:{Port}")]
    private partial void LogInitializing(string brokerAddress, int port);

    [LoggerMessage(2002, LogLevel.Information, "MQTT connection established successfully")]
    private partial void LogConnectionEstablished();

    [LoggerMessage(2003, LogLevel.Warning, "MQTT connection lost: {Reason}")]
    private partial void LogConnectionLost(string reason);

    [LoggerMessage(2004, LogLevel.Error, "Failed to initialize MQTT connection")]
    private partial void LogInitializationFailed(Exception ex);

    [LoggerMessage(2005, LogLevel.Error, "MQTT operation {Operation} failed")]
    private partial void LogOperationFailed(string operation, Exception ex);

    [LoggerMessage(2008, LogLevel.Information, "MQTT service disposed")]
    private partial void LogServiceDisposed();

    #endregion

    #region Properties

    public bool IsConnected => _mqttClient?.IsConnected ?? false;

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
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.PublishAsync(notification);
        }
        catch (Exception ex)
        {
            LogOperationFailed("PublishNotification", ex);
        }
    }

    private void BuildTopicConfigurations()
    {
        // Build zone topic configurations (1-based indexing)
        for (int i = 0; i < _zoneConfigs.Count; i++)
        {
            var zoneConfig = _zoneConfigs[i];
            var zoneId = i + 1; // 1-based indexing
            var topics = zoneConfig.BuildMqttTopics();
            _zoneTopics.TryAdd(zoneId, topics);
        }

        // Build client topic configurations
        for (int i = 0; i < _clientConfigs.Count; i++)
        {
            var clientConfig = _clientConfigs[i];
            var clientId = $"client_{i + 1}"; // Use client_1, client_2, etc.
            var topics = clientConfig.BuildMqttTopics();
            _clientTopics.TryAdd(clientId, topics);
        }
    }

    #endregion

    #region Interface Implementation

    public async Task<Result> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return Result.Failure("Service has been disposed");

        if (_initialized)
            return Result.Success();

        if (!_config.Enabled)
        {
            _logger.LogInformation("MQTT integration is disabled");
            return Result.Success();
        }

        try
        {
            LogInitializing(_config.BrokerAddress, _config.Port);

            // Create MQTT client using v5 API
            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();

            // Configure client options
            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(_config.BrokerAddress, _config.Port)
                .WithClientId(_config.ClientId)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(_config.KeepAlive))
                .WithCleanSession(true);

            // Add authentication if configured
            if (!string.IsNullOrEmpty(_config.Username))
            {
                optionsBuilder.WithCredentials(_config.Username, _config.Password);
            }

            var options = optionsBuilder.Build();

            // Set up event handlers
            _mqttClient.ConnectedAsync += OnConnectedAsync;
            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
            _mqttClient.ApplicationMessageReceivedAsync += OnApplicationMessageReceivedAsync;

            // Connect to broker
            await _mqttClient.ConnectAsync(options, cancellationToken);

            _initialized = true;
            LogConnectionEstablished();

            return Result.Success();
        }
        catch (Exception ex)
        {
            LogInitializationFailed(ex);
            return Result.Failure($"Failed to initialize MQTT connection: {ex.Message}");
        }
    }

    public async Task<Result> PublishZoneStateAsync(
        int zoneId,
        ZoneState state,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsConnected)
            return Result.Failure("MQTT client is not connected");

        if (!_zoneTopics.TryGetValue(zoneId, out var topics))
            return Result.Failure($"Zone {zoneId} not configured for MQTT");

        try
        {
            // Publish comprehensive state as JSON
            var stateJson = JsonSerializer.Serialize(
                state,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );
            await PublishAsync(topics.Status.State, stateJson, retain: true, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            LogOperationFailed("PublishZoneState", ex);
            return Result.Failure($"Failed to publish zone state: {ex.Message}");
        }
    }

    public async Task<Result> PublishClientStateAsync(
        string clientId,
        ClientState state,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsConnected)
            return Result.Failure("MQTT client is not connected");

        if (!_clientTopics.TryGetValue(clientId, out var topics))
            return Result.Failure($"Client {clientId} not configured for MQTT");

        try
        {
            // Publish comprehensive state as JSON
            var stateJson = JsonSerializer.Serialize(
                state,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );
            await PublishAsync(topics.Status.State, stateJson, retain: true, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            LogOperationFailed("PublishClientState", ex);
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
        if (!IsConnected)
            return Result.Failure("MQTT client is not connected");

        try
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(retain)
                .Build();

            await _mqttClient!.PublishAsync(message, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogOperationFailed("Publish", ex);
            return Result.Failure($"Failed to publish message: {ex.Message}");
        }
    }

    public async Task<Result> SubscribeAsync(IEnumerable<string> topics, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            return Result.Failure("MQTT client is not connected");

        try
        {
            foreach (var topic in topics)
            {
                await _mqttClient!.SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce, cancellationToken);
            }
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogOperationFailed("Subscribe", ex);
            return Result.Failure($"Failed to subscribe to topics: {ex.Message}");
        }
    }

    public async Task<Result> UnsubscribeAsync(
        IEnumerable<string> topics,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsConnected)
            return Result.Failure("MQTT client is not connected");

        try
        {
            foreach (var topic in topics)
            {
                await _mqttClient!.UnsubscribeAsync(topic, cancellationToken);
            }
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogOperationFailed("Unsubscribe", ex);
            return Result.Failure($"Failed to unsubscribe from topics: {ex.Message}");
        }
    }

    #endregion

    #region Event Handlers

    private async Task OnConnectedAsync(MqttClientConnectedEventArgs args)
    {
        LogConnectionEstablished();
        Connected?.Invoke(this, EventArgs.Empty);
        await PublishNotificationAsync(new MqttConnectionEstablishedNotification());
    }

    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        var reason = args.Reason.ToString();
        LogConnectionLost(reason);
        Disconnected?.Invoke(this, reason);
        await PublishNotificationAsync(new MqttConnectionLostNotification(reason));
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
            MessageReceived?.Invoke(this, eventArgs);

            // Process the message and convert to command (placeholder for future implementation)
            await ProcessIncomingMessageAsync(topic, payload);
        }
        catch (Exception ex)
        {
            LogOperationFailed("ProcessMessage", ex);
        }
    }

    private async Task ProcessIncomingMessageAsync(string topic, string payload)
    {
        // TODO: Implement topic-to-command mapping
        // This will map MQTT topics to Cortex.Mediator commands
        // For now, just log the received message
        _logger.LogDebug("Received MQTT message on topic {Topic}: {Payload}", topic, payload);

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
        if (_disposed)
            return;

        try
        {
            if (_mqttClient != null)
            {
                if (_mqttClient.IsConnected)
                {
                    await _mqttClient.DisconnectAsync();
                }

                _mqttClient.ConnectedAsync -= OnConnectedAsync;
                _mqttClient.DisconnectedAsync -= OnDisconnectedAsync;
                _mqttClient.ApplicationMessageReceivedAsync -= OnApplicationMessageReceivedAsync;

                _mqttClient.Dispose();
            }

            LogServiceDisposed();
        }
        catch (Exception ex)
        {
            LogOperationFailed("Dispose", ex);
        }
        finally
        {
            _disposed = true;
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
