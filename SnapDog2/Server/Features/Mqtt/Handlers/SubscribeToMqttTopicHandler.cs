using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Server.Features.Mqtt.Commands;

namespace SnapDog2.Server.Features.Mqtt.Handlers;

/// <summary>
/// Handler for subscribing to MQTT topics.
/// </summary>
public class SubscribeToMqttTopicHandler : IRequestHandler<SubscribeToMqttTopicCommand, bool>
{
    private readonly IMqttService _mqttService;
    private readonly ILogger<SubscribeToMqttTopicHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscribeToMqttTopicHandler"/> class.
    /// </summary>
    /// <param name="mqttService">The MQTT service.</param>
    /// <param name="logger">The logger.</param>
    public SubscribeToMqttTopicHandler(IMqttService mqttService, ILogger<SubscribeToMqttTopicHandler> logger)
    {
        _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the subscribe to MQTT topic command.
    /// </summary>
    /// <param name="request">The subscribe command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the subscription was successful, false otherwise.</returns>
    public async Task<bool> Handle(SubscribeToMqttTopicCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "Subscribing to MQTT topic pattern {TopicPattern} with QoS {QoS}",
            request.TopicPattern,
            request.QoS
        );

        try
        {
            // Ensure connection is established
            var isConnected = await _mqttService.ConnectAsync(cancellationToken);
            if (!isConnected)
            {
                _logger.LogWarning("Failed to connect to MQTT broker before subscribing to topic");
                return false;
            }

            // Subscribe to the topic pattern
            var result = await _mqttService.SubscribeAsync(request.TopicPattern, cancellationToken);

            if (result)
            {
                _logger.LogDebug("Successfully subscribed to MQTT topic pattern {TopicPattern}", request.TopicPattern);
            }
            else
            {
                _logger.LogWarning("Failed to subscribe to MQTT topic pattern {TopicPattern}", request.TopicPattern);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while subscribing to MQTT topic pattern {TopicPattern}",
                request.TopicPattern
            );
            return false;
        }
    }
}
