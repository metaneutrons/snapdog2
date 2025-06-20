using MediatR;

namespace SnapDog2.Server.Features.Mqtt.Commands;

/// <summary>
/// Command to unsubscribe from an MQTT topic pattern.
/// </summary>
public class UnsubscribeFromMqttTopicCommand : IRequest<bool>
{
    /// <summary>
    /// Gets or sets the MQTT topic pattern to unsubscribe from.
    /// </summary>
    public string TopicPattern { get; set; } = string.Empty;
}
