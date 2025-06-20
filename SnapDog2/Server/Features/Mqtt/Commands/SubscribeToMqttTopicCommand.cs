using MediatR;
using MQTTnet.Protocol;

namespace SnapDog2.Server.Features.Mqtt.Commands;

/// <summary>
/// Command to subscribe to an MQTT topic pattern.
/// </summary>
public class SubscribeToMqttTopicCommand : IRequest<bool>
{
    /// <summary>
    /// Gets or sets the MQTT topic pattern to subscribe to (supports wildcards + and #).
    /// </summary>
    public string TopicPattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quality of service level for the subscription.
    /// </summary>
    public MqttQualityOfServiceLevel QoS { get; set; } = MqttQualityOfServiceLevel.AtMostOnce;
}
