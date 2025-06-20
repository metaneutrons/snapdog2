using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MQTTnet.Protocol;
using SnapDog2.Api.Models;
using SnapDog2.Server.Features.Mqtt.Commands;
using SnapDog2.Server.Features.Mqtt.Queries;

namespace SnapDog2.Api.Controllers;

/// <summary>
/// Controller for MQTT operations including message publishing, topic subscription management, and connection status monitoring.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MqttController : ApiControllerBase
{
    /// <summary>
    /// Initializes a new instance of the MqttController.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance.</param>
    /// <param name="logger">The logger instance.</param>
    public MqttController(IMediator mediator, ILogger<MqttController> logger)
        : base(mediator, logger) { }

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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> PublishMessage(
        [FromBody] PublishMqttMessageCommand command,
        CancellationToken cancellationToken = default
    )
    {
        Logger.LogInformation("Publishing MQTT message to topic: {Topic}", command.Topic);

        var result = await Mediator.Send(command, cancellationToken);

        if (result)
        {
            return Ok(
                ApiResponse<object>.Ok(new { message = "Message published successfully", topic = command.Topic })
            );
        }

        return ErrorResponse<object>("Failed to publish MQTT message", 500);
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> SubscribeToTopic(
        [FromBody] SubscribeToMqttTopicCommand command,
        CancellationToken cancellationToken = default
    )
    {
        Logger.LogInformation("Subscribing to MQTT topic pattern: {TopicPattern}", command.TopicPattern);

        var result = await Mediator.Send(command, cancellationToken);

        if (result)
        {
            return Ok(
                ApiResponse<object>.Ok(
                    new { message = "Subscription created successfully", topicPattern = command.TopicPattern }
                )
            );
        }

        return ErrorResponse<object>("Failed to subscribe to MQTT topic", 500);
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> UnsubscribeFromTopic(
        [FromBody] UnsubscribeFromMqttTopicCommand command,
        CancellationToken cancellationToken = default
    )
    {
        Logger.LogInformation("Unsubscribing from MQTT topic pattern: {TopicPattern}", command.TopicPattern);

        var result = await Mediator.Send(command, cancellationToken);

        if (result)
        {
            return Ok(
                ApiResponse<object>.Ok(
                    new { message = "Unsubscription successful", topicPattern = command.TopicPattern }
                )
            );
        }

        return ErrorResponse<object>("Failed to unsubscribe from MQTT topic", 500);
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
    [ProducesResponseType(typeof(ApiResponse<MqttConnectionStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MqttConnectionStatusResponse>>> GetConnectionStatus(
        CancellationToken cancellationToken = default
    )
    {
        Logger.LogInformation("Getting MQTT connection status");

        var query = new GetMqttConnectionStatusQuery();
        var result = await Mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<MqttConnectionStatusResponse>.Ok(result));
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> PublishSimpleMessage(
        [FromQuery] string topic,
        [FromBody] string message,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return BadRequest(ApiResponse<object>.Fail("Topic cannot be empty"));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return BadRequest(ApiResponse<object>.Fail("Message cannot be empty"));
        }

        Logger.LogInformation("Publishing simple MQTT message to topic: {Topic}", topic);

        var command = new PublishMqttMessageCommand
        {
            Topic = topic,
            Payload = message,
            QoS = MqttQualityOfServiceLevel.AtMostOnce,
            Retain = false,
        };

        var result = await Mediator.Send(command, cancellationToken);

        if (result)
        {
            return Ok(ApiResponse<object>.Ok(new { message = "Simple message published successfully", topic = topic }));
        }

        return ErrorResponse<object>("Failed to publish simple MQTT message", 500);
    }
}
