using MediatR;

namespace SnapDog2.Server.Features.Mqtt.Queries;

/// <summary>
/// Query to get the current MQTT connection status and service information.
/// </summary>
public class GetMqttConnectionStatusQuery : IRequest<MqttConnectionStatusResponse>
{
    // No parameters needed for this query
}

/// <summary>
/// Response containing MQTT connection status and service information.
/// </summary>
public class MqttConnectionStatusResponse
{
    /// <summary>
    /// Gets or sets whether the MQTT client is connected.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Gets or sets the broker host.
    /// </summary>
    public string BrokerHost { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the broker port.
    /// </summary>
    public int BrokerPort { get; set; }

    /// <summary>
    /// Gets or sets the client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp of the last connection attempt.
    /// </summary>
    public DateTime? LastConnectionAttempt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the connection was established.
    /// </summary>
    public DateTime? ConnectedAt { get; set; }

    /// <summary>
    /// Gets or sets the last error message, if any.
    /// </summary>
    public string? LastError { get; set; }
}
