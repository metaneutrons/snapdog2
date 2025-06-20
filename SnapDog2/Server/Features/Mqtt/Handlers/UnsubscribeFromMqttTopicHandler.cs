using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Server.Features.Mqtt.Commands;

namespace SnapDog2.Server.Features.Mqtt.Handlers;

/// <summary>
/// Handler for unsubscribing from MQTT topics.
/// </summary>
public class UnsubscribeFromMqttTopicHandler : IRequestHandler<UnsubscribeFromMqttTopicCommand, bool>
{
    private readonly IMqttService _mqttService;
    private readonly ILogger<UnsubscribeFromMqttTopicHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsubscribeFromMqttTopicHandler"/> class.
    /// </summary>
    /// <param name="mqttService">The MQTT service.</param>
    /// <param name="logger">The logger.</param>
    public UnsubscribeFromMqttTopicHandler(IMqttService mqttService, ILogger<UnsubscribeFromMqttTopicHandler> logger)
    {
        _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the unsubscribe from MQTT topic command.
    /// </summary>
    /// <param name="request">The unsubscribe command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the unsubscription was successful, false otherwise.</returns>
    public async Task<bool> Handle(UnsubscribeFromMqttTopicCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Unsubscribing from MQTT topic pattern {TopicPattern}", request.TopicPattern);

        try
        {
            // Unsubscribe from the topic pattern
            var result = await _mqttService.UnsubscribeAsync(request.TopicPattern, cancellationToken);

            if (result)
            {
                _logger.LogDebug(
                    "Successfully unsubscribed from MQTT topic pattern {TopicPattern}",
                    request.TopicPattern
                );
            }
            else
            {
                _logger.LogWarning(
                    "Failed to unsubscribe from MQTT topic pattern {TopicPattern}",
                    request.TopicPattern
                );
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while unsubscribing from MQTT topic pattern {TopicPattern}",
                request.TopicPattern
            );
            return false;
        }
    }
}
