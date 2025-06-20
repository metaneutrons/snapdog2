using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Server.Features.Mqtt.Commands;

namespace SnapDog2.Server.Features.Mqtt.Handlers;

/// <summary>
/// Handler for publishing messages to MQTT topics.
/// </summary>
public class PublishMqttMessageHandler : IRequestHandler<PublishMqttMessageCommand, bool>
{
    private readonly IMqttService _mqttService;
    private readonly ILogger<PublishMqttMessageHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublishMqttMessageHandler"/> class.
    /// </summary>
    /// <param name="mqttService">The MQTT service.</param>
    /// <param name="logger">The logger.</param>
    public PublishMqttMessageHandler(IMqttService mqttService, ILogger<PublishMqttMessageHandler> logger)
    {
        _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the publish MQTT message command.
    /// </summary>
    /// <param name="request">The publish message command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the message was published successfully, false otherwise.</returns>
    public async Task<bool> Handle(PublishMqttMessageCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "Publishing MQTT message to topic {Topic} with QoS {QoS}, Retain: {Retain}",
            request.Topic,
            request.QoS,
            request.Retain
        );

        try
        {
            // Ensure connection is established
            var isConnected = await _mqttService.ConnectAsync(cancellationToken);
            if (!isConnected)
            {
                _logger.LogWarning("Failed to connect to MQTT broker before publishing message");
                return false;
            }

            // Publish the message using the extended method with QoS and retain
            var result = await _mqttService.PublishAsync(
                request.Topic,
                request.Payload,
                request.QoS,
                request.Retain,
                cancellationToken
            );

            if (result)
            {
                _logger.LogDebug("Successfully published MQTT message to topic {Topic}", request.Topic);
            }
            else
            {
                _logger.LogWarning("Failed to publish MQTT message to topic {Topic}", request.Topic);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while publishing MQTT message to topic {Topic}", request.Topic);
            return false;
        }
    }
}
