using MediatR;
using MQTTnet.Protocol;

namespace SnapDog2.Server.Features.Mqtt.Commands;

/// <summary>
/// Command to publish a message to an MQTT topic.
/// </summary>
public class PublishMqttMessageCommand : IRequest<bool>
{
    /// <summary>
    /// Gets or sets the MQTT topic to publish to.
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message payload.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quality of service level.
    /// </summary>
    public MqttQualityOfServiceLevel QoS { get; set; } = MqttQualityOfServiceLevel.AtMostOnce;

    /// <summary>
    /// Gets or sets whether the message should be retained by the broker.
    /// </summary>
    public bool Retain { get; set; } = false;
}
