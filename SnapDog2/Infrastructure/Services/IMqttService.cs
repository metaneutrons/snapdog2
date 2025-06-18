namespace SnapDog2.Infrastructure.Services;

/// <summary>
/// Interface for MQTT broker communication and message handling.
/// Provides methods for connecting to MQTT brokers, publishing messages,
/// subscribing to topics, and handling incoming messages.
/// </summary>
public interface IMqttService
{
    /// <summary>
    /// Establishes connection to the MQTT broker.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection was successful, false otherwise</returns>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the MQTT broker.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the disconnect operation</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a message to a specific MQTT topic.
    /// </summary>
    /// <param name="topic">The topic to publish to</param>
    /// <param name="payload">The message payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if message was published successfully, false otherwise</returns>
    Task<bool> PublishAsync(string topic, string payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to messages matching a topic pattern.
    /// </summary>
    /// <param name="topicPattern">The topic pattern to subscribe to (supports wildcards)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if subscription was successful, false otherwise</returns>
    Task<bool> SubscribeAsync(string topicPattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from messages matching a topic pattern.
    /// </summary>
    /// <param name="topicPattern">The topic pattern to unsubscribe from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if unsubscription was successful, false otherwise</returns>
    Task<bool> UnsubscribeAsync(string topicPattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a message is received from a subscribed topic.
    /// </summary>
    event EventHandler<MqttMessageReceivedEventArgs> MessageReceived;
}

/// <summary>
/// Event arguments for MQTT message received events.
/// Contains the topic, payload, and timestamp of the received message.
/// </summary>
public class MqttMessageReceivedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the topic on which the message was received.
    /// </summary>
    public string Topic { get; init; } = string.Empty;

    /// <summary>
    /// Gets the message payload.
    /// </summary>
    public string Payload { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the message was received.
    /// </summary>
    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
}
