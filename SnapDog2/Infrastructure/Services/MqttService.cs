using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using Polly;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Events;
using SnapDog2.Infrastructure.Resilience;

namespace SnapDog2.Infrastructure.Services;

/// <summary>
/// Implementation of MQTT broker communication and message handling using MQTTnet.
/// Provides methods for connecting to MQTT brokers, publishing messages,
/// subscribing to topics, and handling incoming messages with resilience policies.
/// </summary>
public class MqttService : IMqttService, IDisposable, IAsyncDisposable
{
    private readonly ServicesMqttConfiguration _config;
    private readonly IAsyncPolicy _resiliencePolicy;
    private readonly ILogger<MqttService> _logger;
    private readonly IMediator _mediator;
    private readonly IManagedMqttClient _mqttClient;
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ConcurrentDictionary<string, List<Func<string, string, Task>>> _topicHandlers = new();
    private bool _disposed;

    /// <summary>
    /// Event raised when a message is received from a subscribed topic.
    /// </summary>
    public event EventHandler<MqttMessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttService"/> class.
    /// </summary>
    /// <param name="config">The MQTT configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="mediator">The mediator for publishing events.</param>
    public MqttService(IOptions<ServicesMqttConfiguration> config, ILogger<MqttService> logger, IMediator mediator)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

        _resiliencePolicy = PolicyFactory.CreateFromConfiguration(
            retryAttempts: 3,
            circuitBreakerThreshold: 3,
            circuitBreakerDuration: TimeSpan.FromSeconds(30),
            defaultTimeout: TimeSpan.FromSeconds(30),
            logger: _logger
        );

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        // Create managed MQTT client
        var factory = new MqttFactory();
        _mqttClient = factory.CreateManagedMqttClient();

        // Subscribe to client events
        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _mqttClient.ConnectedAsync += OnConnectedAsync;
        _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
        _mqttClient.ConnectingFailedAsync += OnConnectingFailedAsync;

        _logger.LogDebug("MQTT service initialized with broker {Broker}:{Port}", _config.Broker, _config.Port);
    }

    /// <summary>
    /// Establishes connection to the MQTT broker.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection was successful, false otherwise</returns>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MqttService));
        }

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_mqttClient.IsConnected)
            {
                _logger.LogDebug("MQTT client is already connected");
                return true;
            }

            return await _resiliencePolicy.ExecuteAsync(async () =>
            {
                _logger.LogInformation("Connecting to MQTT broker {Broker}:{Port}", _config.Broker, _config.Port);

                var clientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer(_config.Broker, _config.Port)
                    .WithClientId(_config.ClientId)
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(_config.KeepAliveSeconds))
                    .WithCleanSession(true);

                // Add credentials if provided
                if (!string.IsNullOrEmpty(_config.Username))
                {
                    clientOptions = clientOptions.WithCredentials(_config.Username, _config.Password);
                }

                // Add TLS if enabled
                if (_config.SslEnabled)
                {
                    clientOptions = clientOptions.WithTlsOptions(o => o.UseTls());
                }

                var managedOptions = new ManagedMqttClientOptionsBuilder()
                    .WithClientOptions(clientOptions.Build())
                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                    .Build();

                await _mqttClient.StartAsync(managedOptions);

                // Wait for connection with timeout
                var connectionTimeout = TimeSpan.FromSeconds(10);
                var startTime = DateTime.UtcNow;

                while (!_mqttClient.IsConnected && DateTime.UtcNow - startTime < connectionTimeout)
                {
                    await Task.Delay(100, cancellationToken);
                }

                if (_mqttClient.IsConnected)
                {
                    _logger.LogInformation("Successfully connected to MQTT broker");
                    return true;
                }

                _logger.LogWarning("Failed to connect to MQTT broker within timeout");
                return false;
            });
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("MQTT connection was cancelled");
            throw new OperationCanceledException("Operation was cancelled", ex, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to MQTT broker");
            return false;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Disconnects from the MQTT broker.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the disconnect operation</returns>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return;
        }

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (!_mqttClient.IsConnected)
            {
                _logger.LogDebug("MQTT client is already disconnected");
                return;
            }

            _logger.LogInformation("Disconnecting from MQTT broker");
            await _mqttClient.StopAsync();
            _logger.LogInformation("Successfully disconnected from MQTT broker");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while disconnecting from MQTT broker");
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Publishes a message to a specific MQTT topic.
    /// </summary>
    /// <param name="topic">The topic to publish to</param>
    /// <param name="payload">The message payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if message was published successfully, false otherwise</returns>
    public async Task<bool> PublishAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        return await PublishAsync(topic, payload, MqttQualityOfServiceLevel.AtMostOnce, false, cancellationToken);
    }

    /// <summary>
    /// Publishes a message to a specific MQTT topic with specified QoS and retain flag.
    /// </summary>
    /// <param name="topic">The topic to publish to</param>
    /// <param name="payload">The message payload</param>
    /// <param name="qos">Quality of service level</param>
    /// <param name="retain">Whether to retain the message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if message was published successfully, false otherwise</returns>
    public async Task<bool> PublishAsync(
        string topic,
        string payload,
        MqttQualityOfServiceLevel qos,
        bool retain,
        CancellationToken cancellationToken = default
    )
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MqttService));
        }

        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic cannot be null, empty, or whitespace", nameof(topic));
        }

        if (payload == null)
        {
            throw new ArgumentNullException(nameof(payload), "Payload cannot be null.");
        }

        // Validate topic for publishing - wildcards are not allowed
        if (topic.Contains('+') || topic.Contains('#'))
        {
            throw new ArgumentException(
                "Wildcard characters (+ and #) are not allowed in publish topics",
                nameof(topic)
            );
        }

        // Validate topic format - basic MQTT topic validation
        if (topic.StartsWith('/') || topic.EndsWith('/') || topic.Contains("//"))
        {
            throw new ArgumentException("Invalid topic format", nameof(topic));
        }

        try
        {
            if (!_mqttClient.IsConnected)
            {
                _logger.LogWarning("Cannot publish message: MQTT client is not connected");
                return false;
            }

            return await _resiliencePolicy.ExecuteAsync(async () =>
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(Encoding.UTF8.GetBytes(payload ?? string.Empty))
                    .WithQualityOfServiceLevel(qos)
                    .WithRetainFlag(retain)
                    .Build();

                await _mqttClient.EnqueueAsync(message);

                _logger.LogDebug("Published message to topic {Topic} with QoS {QoS}", topic, qos);
                return true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to topic {Topic}", topic);
            return false;
        }
    }

    /// <summary>
    /// Subscribes to messages matching a topic pattern.
    /// </summary>
    /// <param name="topicPattern">The topic pattern to subscribe to (supports wildcards)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if subscription was successful, false otherwise</returns>
    public async Task<bool> SubscribeAsync(string topicPattern, CancellationToken cancellationToken = default)
    {
        return await SubscribeAsync(topicPattern, MqttQualityOfServiceLevel.AtMostOnce, cancellationToken);
    }

    /// <summary>
    /// Subscribes to messages matching a topic pattern with specified QoS.
    /// </summary>
    /// <param name="topicPattern">The topic pattern to subscribe to (supports wildcards)</param>
    /// <param name="qos">Quality of service level</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if subscription was successful, false otherwise</returns>
    public async Task<bool> SubscribeAsync(
        string topicPattern,
        MqttQualityOfServiceLevel qos,
        CancellationToken cancellationToken = default
    )
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MqttService));
        }

        if (string.IsNullOrWhiteSpace(topicPattern))
        {
            throw new ArgumentException("Topic pattern cannot be null, empty, or whitespace", nameof(topicPattern));
        }

        try
        {
            if (!_mqttClient.IsConnected)
            {
                _logger.LogWarning("Cannot subscribe: MQTT client is not connected");
                return false;
            }

            return await _resiliencePolicy.ExecuteAsync(async () =>
            {
                var subscription = new MqttTopicFilterBuilder()
                    .WithTopic(topicPattern)
                    .WithQualityOfServiceLevel(qos)
                    .Build();

                await _mqttClient.SubscribeAsync(new[] { subscription });

                _logger.LogDebug("Subscribed to topic pattern {TopicPattern} with QoS {QoS}", topicPattern, qos);
                return true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to topic pattern {TopicPattern}", topicPattern);
            return false;
        }
    }

    /// <summary>
    /// Unsubscribes from messages matching a topic pattern.
    /// </summary>
    /// <param name="topicPattern">The topic pattern to unsubscribe from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if unsubscription was successful, false otherwise</returns>
    public async Task<bool> UnsubscribeAsync(string topicPattern, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MqttService));
        }

        if (string.IsNullOrWhiteSpace(topicPattern))
        {
            throw new ArgumentException("Topic pattern cannot be null, empty, or whitespace", nameof(topicPattern));
        }

        try
        {
            if (!_mqttClient.IsConnected)
            {
                _logger.LogWarning("Cannot unsubscribe: MQTT client is not connected");
                return false;
            }

            return await _resiliencePolicy.ExecuteAsync(async () =>
            {
                await _mqttClient.UnsubscribeAsync(topicPattern);

                _logger.LogDebug("Unsubscribed from topic pattern {TopicPattern}", topicPattern);
                return true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from topic pattern {TopicPattern}", topicPattern);
            return false;
        }
    }

    /// <summary>
    /// Handles incoming MQTT messages and raises the MessageReceived event.
    /// </summary>
    /// <param name="eventArgs">The message received event arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        try
        {
            var topic = eventArgs.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(eventArgs.ApplicationMessage.PayloadSegment);

            _logger.LogDebug("Received message on topic {Topic}: {Payload}", topic, payload);

            var args = new MqttMessageReceivedEventArgs
            {
                Topic = topic,
                Payload = payload,
                ReceivedAt = DateTime.UtcNow,
            };

            MessageReceived?.Invoke(this, args);

            // Process command messages and publish domain events
            await ProcessCommandMessageAsync(topic, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing received MQTT message on topic {Topic}",
                eventArgs.ApplicationMessage.Topic
            );
        }
    }

    /// <summary>
    /// Processes MQTT command messages and publishes appropriate domain events.
    /// </summary>
    /// <param name="topic">The MQTT topic</param>
    /// <param name="payload">The message payload</param>
    private async Task ProcessCommandMessageAsync(string topic, string payload)
    {
        try
        {
            // Parse topic structure: {BaseTopic}/{component}/{id}/{command}
            var BaseTopic = _config.BaseTopic;
            if (!topic.StartsWith(BaseTopic))
            {
                return; // Not a command topic for this service
            }

            var topicParts = topic.Substring(BaseTopic.Length + 1).Split('/');
            if (topicParts.Length < 3)
            {
                return; // Invalid topic structure
            }

            var component = topicParts[0].ToUpperInvariant();
            var id = topicParts[1];
            var command = topicParts[2].ToUpperInvariant();

            _logger.LogDebug("Processing MQTT command: {Component}/{Id}/{Command}", component, id, command);

            switch (component)
            {
                case "ZONE":
                    await ProcessZoneCommandAsync(id, command, payload);
                    break;

                case "CLIENT":
                    await ProcessClientCommandAsync(id, command, payload);
                    break;

                case "STREAM":
                    await ProcessStreamCommandAsync(id, command, payload);
                    break;

                case "SYSTEM":
                    await ProcessSystemCommandAsync(command, payload);
                    break;

                default:
                    _logger.LogTrace("Unhandled MQTT component: {Component}", component);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MQTT command message: {Topic}", topic);
        }
    }

    /// <summary>
    /// Processes zone-related MQTT commands.
    /// </summary>
    private async Task ProcessZoneCommandAsync(string zoneId, string command, string payload)
    {
        switch (command)
        {
            case "VOLUME":
                if (int.TryParse(payload, out var volume))
                {
                    await _mediator.Publish(new MqttZoneVolumeCommandEvent(int.Parse(zoneId), volume));
                    _logger.LogDebug(
                        "Published zone volume command event: Zone={ZoneId}, Volume={Volume}",
                        zoneId,
                        volume
                    );
                }
                break;

            case "MUTE":
                if (bool.TryParse(payload, out var muted))
                {
                    await _mediator.Publish(new MqttZoneMuteCommandEvent(int.Parse(zoneId), muted));
                    _logger.LogDebug("Published zone mute command event: Zone={ZoneId}, Muted={Muted}", zoneId, muted);
                }
                break;

            case "STREAM":
                if (int.TryParse(payload, out var streamId))
                {
                    await _mediator.Publish(new MqttZoneStreamCommandEvent(int.Parse(zoneId), streamId));
                    _logger.LogDebug(
                        "Published zone stream command event: Zone={ZoneId}, Stream={StreamId}",
                        zoneId,
                        streamId
                    );
                }
                break;

            default:
                _logger.LogTrace("Unhandled zone command: {Command}", command);
                break;
        }
    }

    /// <summary>
    /// Processes client-related MQTT commands.
    /// </summary>
    private async Task ProcessClientCommandAsync(string clientId, string command, string payload)
    {
        switch (command)
        {
            case "VOLUME":
                if (int.TryParse(payload, out var volume))
                {
                    await _mediator.Publish(new MqttClientVolumeCommandEvent(clientId, volume));
                    _logger.LogDebug(
                        "Published client volume command event: Client={ClientId}, Volume={Volume}",
                        clientId,
                        volume
                    );
                }
                break;

            case "MUTE":
                if (bool.TryParse(payload, out var muted))
                {
                    await _mediator.Publish(new MqttClientMuteCommandEvent(clientId, muted));
                    _logger.LogDebug(
                        "Published client mute command event: Client={ClientId}, Muted={Muted}",
                        clientId,
                        muted
                    );
                }
                break;

            default:
                _logger.LogTrace("Unhandled client command: {Command}", command);
                break;
        }
    }

    /// <summary>
    /// Processes stream-related MQTT commands.
    /// </summary>
    private async Task ProcessStreamCommandAsync(string streamId, string command, string payload)
    {
        switch (command)
        {
            case "START":
                if (int.TryParse(streamId, out var startStreamId))
                {
                    await _mediator.Publish(new MqttStreamStartCommandEvent(startStreamId));
                    _logger.LogDebug("Published stream start command event: Stream={StreamId}", streamId);
                }
                break;

            case "STOP":
                if (int.TryParse(streamId, out var stopStreamId))
                {
                    await _mediator.Publish(new MqttStreamStopCommandEvent(stopStreamId));
                    _logger.LogDebug("Published stream stop command event: Stream={StreamId}", streamId);
                }
                break;

            default:
                _logger.LogTrace("Unhandled stream command: {Command}", command);
                break;
        }
    }

    /// <summary>
    /// Processes system-related MQTT commands.
    /// </summary>
    private async Task ProcessSystemCommandAsync(string command, string payload)
    {
        switch (command)
        {
            case "SHUTDOWN":
                await _mediator.Publish(new MqttSystemShutdownCommandEvent());
                _logger.LogDebug("Published system shutdown command event");
                break;

            case "RESTART":
                await _mediator.Publish(new MqttSystemRestartCommandEvent());
                _logger.LogDebug("Published system restart command event");
                break;

            case "SYNC":
                await _mediator.Publish(new MqttSystemSyncCommandEvent());
                _logger.LogDebug("Published system sync command event");
                break;

            default:
                _logger.LogTrace("Unhandled system command: {Command}", command);
                break;
        }
    }

    /// <summary>
    /// Handles MQTT client connected events.
    /// </summary>
    /// <param name="eventArgs">The connected event arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private Task OnConnectedAsync(MqttClientConnectedEventArgs eventArgs)
    {
        _logger.LogInformation("MQTT client connected successfully");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles MQTT client disconnected events.
    /// </summary>
    /// <param name="eventArgs">The disconnected event arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
    {
        if (eventArgs.Exception != null)
        {
            _logger.LogWarning(eventArgs.Exception, "MQTT client disconnected with exception");
        }
        else
        {
            _logger.LogInformation("MQTT client disconnected");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles MQTT client connection failed events.
    /// </summary>
    /// <param name="eventArgs">The connection failed event arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private Task OnConnectingFailedAsync(ConnectingFailedEventArgs eventArgs)
    {
        _logger.LogError(eventArgs.Exception, "MQTT client connection failed");
        return Task.CompletedTask;
    }

    #region Command Framework Methods

    /// <summary>
    /// Publishes a stream status update to MQTT.
    /// </summary>
    /// <param name="streamId">The stream identifier</param>
    /// <param name="status">The stream status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if published successfully, false otherwise</returns>
    public async Task<bool> PublishStreamStatusAsync(
        int streamId,
        string status,
        CancellationToken cancellationToken = default
    )
    {
        var topic = $"{_config.BaseTopic}/STREAM/{streamId}/STATUS";
        var payload = JsonSerializer.Serialize(
            new
            {
                streamId,
                status,
                timestamp = DateTime.UtcNow,
            },
            _jsonOptions
        );

        return await PublishAsync(topic, payload, MqttQualityOfServiceLevel.AtLeastOnce, true, cancellationToken);
    }

    /// <summary>
    /// Publishes a zone volume update to MQTT.
    /// </summary>
    /// <param name="zoneId">The zone identifier</param>
    /// <param name="volume">The volume level (0-100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if published successfully, false otherwise</returns>
    public async Task<bool> PublishZoneVolumeAsync(
        int zoneId,
        int volume,
        CancellationToken cancellationToken = default
    )
    {
        var topic = $"{_config.BaseTopic}/ZONE/{zoneId}/VOLUME";
        var payload = volume.ToString();

        return await PublishAsync(topic, payload, MqttQualityOfServiceLevel.AtLeastOnce, true, cancellationToken);
    }

    /// <summary>
    /// Publishes a client volume update to MQTT.
    /// </summary>
    /// <param name="clientId">The client identifier</param>
    /// <param name="volume">The volume level (0-100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if published successfully, false otherwise</returns>
    public async Task<bool> PublishClientVolumeAsync(
        string clientId,
        int volume,
        CancellationToken cancellationToken = default
    )
    {
        var topic = $"{_config.BaseTopic}/CLIENT/{clientId}/VOLUME";
        var payload = volume.ToString();

        return await PublishAsync(topic, payload, MqttQualityOfServiceLevel.AtLeastOnce, true, cancellationToken);
    }

    /// <summary>
    /// Publishes a client connection status to MQTT.
    /// </summary>
    /// <param name="clientId">The client identifier</param>
    /// <param name="connected">Whether the client is connected</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if published successfully, false otherwise</returns>
    public async Task<bool> PublishClientStatusAsync(
        string clientId,
        bool connected,
        CancellationToken cancellationToken = default
    )
    {
        var topic = $"{_config.BaseTopic}/CLIENT/{clientId}/STATUS";
        var payload = JsonSerializer.Serialize(
            new
            {
                clientId,
                connected,
                timestamp = DateTime.UtcNow,
            },
            _jsonOptions
        );

        return await PublishAsync(topic, payload, MqttQualityOfServiceLevel.AtLeastOnce, true, cancellationToken);
    }

    /// <summary>
    /// Subscribes to all command topics for the SnapDog system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if subscriptions were successful, false otherwise</returns>
    public async Task<bool> SubscribeToCommandsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Subscribe to all command topics
            var commandTopics = new[]
            {
                $"{_config.BaseTopic}/+/COMMAND/+",
                $"{_config.BaseTopic}/ZONE/+/+",
                $"{_config.BaseTopic}/CLIENT/+/+",
                $"{_config.BaseTopic}/STREAM/+/+",
                $"{_config.BaseTopic}/SYSTEM/+",
            };

            var success = true;
            foreach (var topic in commandTopics)
            {
                var result = await SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce, cancellationToken);
                if (!result)
                {
                    _logger.LogWarning("Failed to subscribe to command topic: {Topic}", topic);
                    success = false;
                }
                else
                {
                    _logger.LogInformation("Subscribed to MQTT command topic: {Topic}", topic);
                }
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to MQTT command topics");
            return false;
        }
    }

    /// <summary>
    /// Publishes system health information to MQTT.
    /// </summary>
    /// <param name="healthData">The health data object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if published successfully, false otherwise</returns>
    public async Task<bool> PublishSystemHealthAsync(object healthData, CancellationToken cancellationToken = default)
    {
        var topic = $"{_config.BaseTopic}/SYSTEM/HEALTH";
        var payload = JsonSerializer.Serialize(healthData, _jsonOptions);

        return await PublishAsync(topic, payload, MqttQualityOfServiceLevel.AtLeastOnce, true, cancellationToken);
    }

    #endregion

    /// <summary>
    /// Disposes the MQTT service and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously disposes the MQTT service and releases all resources.
    /// </summary>
    /// <returns>A task representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the MQTT service resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                _mqttClient?.StopAsync().GetAwaiter().GetResult();
                _mqttClient?.Dispose();
                _connectionSemaphore?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while disposing MQTT service");
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Asynchronously disposes the core MQTT service resources.
    /// </summary>
    /// <returns>A task representing the asynchronous dispose operation.</returns>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            try
            {
                if (_mqttClient != null)
                {
                    await _mqttClient.StopAsync();
                    _mqttClient.Dispose();
                }

                _connectionSemaphore?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while disposing MQTT service asynchronously");
            }

            _disposed = true;
        }
    }
}
