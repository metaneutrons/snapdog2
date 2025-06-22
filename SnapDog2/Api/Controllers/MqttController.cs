using FluentValidation; // Added for ValidationException
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MQTTnet.Protocol;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Server.Features.Mqtt.Commands;
using SnapDog2.Server.Features.Mqtt.Queries;

namespace SnapDog2.Api.Controllers;

/// <summary>
/// Controller for MQTT operations including message publishing, topic subscription management, and connection status monitoring.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MqttController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<MqttController> _logger;

    public MqttController(IMediator mediator, ILogger<MqttController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Publishes a message to an MQTT topic.
    /// </summary>
    /// <param name="command">The publish message command containing topic, payload, and options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Success response if message was published successfully.</returns>
    /// <response code="200">Message published successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="500">Internal server error or MQTT service unavailable</response>
    [HttpPost("publish")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 500)]
    public async Task<ActionResult<object>> PublishMessage(
        [FromBody] PublishMqttMessageCommand command,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Publishing MQTT message to topic: {Topic}", command.Topic);

        try
        {
            var result = await _mediator.Send(command, cancellationToken);

            if (result)
            {
                return Ok(new { message = "Message published successfully", topic = command.Topic });
            }

            return StatusCode(
                500,
                "Failed to publish MQTT message. The operation returned false without specific errors."
            );
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(
                ex,
                "Validation failed for PublishMqttMessageCommand: {ValidationErrors}",
                string.Join(", ", ex.Errors.Select(e => e.ErrorMessage))
            );
            var errorMessages = ex.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new { message = "Validation failed.", errors = errorMessages });
        }
        // Other exceptions will be handled by global handlers or result in a 500 if not caught by UseExceptionHandler.
    }

    /// <summary>
    /// Subscribes to an MQTT topic pattern to receive messages.
    /// </summary>
    /// <param name="command">The subscription command containing topic pattern and options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Success response if subscription was created successfully.</returns>
    /// <response code="200">Subscription created successfully</response>
    /// <response code="400">Invalid topic pattern or parameters</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="500">Internal server error or MQTT service unavailable</response>
    [HttpPost("subscribe")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 500)]
    public async Task<ActionResult<object>> SubscribeToTopic(
        [FromBody] SubscribeToMqttTopicCommand command,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Subscribing to MQTT topic pattern: {TopicPattern}", command.TopicPattern);

        var result = await _mediator.Send(command, cancellationToken);

        if (result)
        {
            return Ok(new { message = "Subscription created successfully", topicPattern = command.TopicPattern });
        }

        return StatusCode(500, "Failed to subscribe to MQTT topic");
    }

    /// <summary>
    /// Unsubscribes from an MQTT topic pattern to stop receiving messages.
    /// </summary>
    /// <param name="command">The unsubscription command containing topic pattern.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Success response if unsubscription was successful.</returns>
    /// <response code="200">Unsubscription successful</response>
    /// <response code="400">Invalid topic pattern</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="500">Internal server error or MQTT service unavailable</response>
    [HttpPost("unsubscribe")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 500)]
    public async Task<ActionResult<object>> UnsubscribeFromTopic(
        [FromBody] UnsubscribeFromMqttTopicCommand command,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Unsubscribing from MQTT topic pattern: {TopicPattern}", command.TopicPattern);

        var result = await _mediator.Send(command, cancellationToken);

        if (result)
        {
            return Ok(new { message = "Unsubscription successful", topicPattern = command.TopicPattern });
        }

        return StatusCode(500, "Failed to unsubscribe from MQTT topic");
    }

    /// <summary>
    /// Gets the current MQTT connection status and service information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>MQTT connection status and service details.</returns>
    /// <response code="200">Connection status retrieved successfully</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(MqttConnectionStatusResponse), 200)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 500)]
    public async Task<ActionResult<MqttConnectionStatusResponse>> GetConnectionStatus(
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Getting MQTT connection status");

        var query = new GetMqttConnectionStatusQuery();
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Publishes a simple text message to an MQTT topic with default settings.
    /// </summary>
    /// <param name="topic">The MQTT topic to publish to.</param>
    /// <param name="message">The message content to publish.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Success response if message was published successfully.</returns>
    /// <response code="200">Message published successfully</response>
    /// <response code="400">Invalid topic or message</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="500">Internal server error or MQTT service unavailable</response>
    [HttpPost("publish/simple")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 500)]
    public async Task<ActionResult<object>> PublishSimpleMessage(
        [FromQuery] string topic,
        [FromBody] string message,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return BadRequest("Topic cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return BadRequest("Message cannot be empty");
        }

        _logger.LogInformation("Publishing simple MQTT message to topic: {Topic}", topic);

        var command = new PublishMqttMessageCommand
        {
            Topic = topic,
            Payload = message,
            QoS = MqttQualityOfServiceLevel.AtMostOnce,
            Retain = false,
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (result)
        {
            return Ok(new { message = "Simple message published successfully", topic = topic });
        }

        return StatusCode(500, "Failed to publish simple MQTT message");
    }
}
