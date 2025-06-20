using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using Polly;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.Resilience;

namespace SnapDog2.Infrastructure.Services;

/// <summary>
/// Implementation of MQTT broker communication and message handling using MQTTnet.
/// Provides methods for connecting to MQTT brokers, publishing messages,
/// subscribing to topics, and handling incoming messages with resilience policies.
/// </summary>
public class MqttService : IMqttService, IDisposable, IAsyncDisposable
{
    private readonly MqttConfiguration _config;
    private readonly IAsyncPolicy _resiliencePolicy;
    private readonly ILogger<MqttService> _logger;
    private readonly IManagedMqttClient _mqttClient;
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
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
    public MqttService(IOptions<MqttConfiguration> config, ILogger<MqttService> logger)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _resiliencePolicy = PolicyFactory.CreateFromConfiguration(
            retryAttempts: 3,
            circuitBreakerThreshold: 3,
            circuitBreakerDuration: TimeSpan.FromSeconds(30),
            defaultTimeout: TimeSpan.FromSeconds(30),
            logger: _logger
        );

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
    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        try
        {
            var topic = eventArgs.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(eventArgs.ApplicationMessage.PayloadSegment);

            _logger.LogDebug("Received message on topic {Topic}", topic);

            var args = new MqttMessageReceivedEventArgs
            {
                Topic = topic,
                Payload = payload,
                ReceivedAt = DateTime.UtcNow,
            };

            MessageReceived?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing received MQTT message");
        }

        return Task.CompletedTask;
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
