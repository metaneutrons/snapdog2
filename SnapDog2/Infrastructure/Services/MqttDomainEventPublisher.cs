using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Protocol;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Events;

namespace SnapDog2.Infrastructure.Services;

/// <summary>
/// Service for publishing domain events to MQTT topics.
/// Automatically converts domain events to MQTT messages and publishes them to appropriate topics.
/// </summary>
public class MqttDomainEventPublisher : IDisposable
{
    private readonly IMqttService _mqttService;
    private readonly MqttConfiguration _config;
    private readonly ILogger<MqttDomainEventPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttDomainEventPublisher"/> class.
    /// </summary>
    /// <param name="mqttService">The MQTT service.</param>
    /// <param name="config">The MQTT configuration.</param>
    /// <param name="logger">The logger.</param>
    public MqttDomainEventPublisher(
        IMqttService mqttService,
        IOptions<MqttConfiguration> config,
        ILogger<MqttDomainEventPublisher> logger
    )
    {
        _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };
    }

    /// <summary>
    /// Publishes a domain event to the appropriate MQTT topic.
    /// </summary>
    /// <param name="domainEvent">The domain event to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the event was published successfully, false otherwise.</returns>
    public async Task<bool> PublishDomainEventAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default
    )
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MqttDomainEventPublisher));
        }

        ArgumentNullException.ThrowIfNull(domainEvent);

        try
        {
            // Generate topic based on event type
            var topic = GenerateTopicForEvent(domainEvent);

            // Create event message
            var eventMessage = new
            {
                EventType = domainEvent.GetType().Name,
                EventId = domainEvent.EventId,
                Timestamp = domainEvent.OccurredAt,
                Data = domainEvent,
            };

            // Serialize to JSON
            var payload = JsonSerializer.Serialize(eventMessage, _jsonOptions);

            _logger.LogDebug(
                "Publishing domain event {EventType} with ID {EventId} to MQTT topic {Topic}",
                domainEvent.GetType().Name,
                domainEvent.EventId,
                topic
            );

            // Publish to MQTT
            var result = await _mqttService.PublishAsync(
                topic,
                payload,
                MqttQualityOfServiceLevel.AtLeastOnce,
                false,
                cancellationToken
            );

            if (result)
            {
                _logger.LogDebug(
                    "Successfully published domain event {EventType} to MQTT topic {Topic}",
                    domainEvent.GetType().Name,
                    topic
                );
            }
            else
            {
                _logger.LogWarning(
                    "Failed to publish domain event {EventType} to MQTT topic {Topic}",
                    domainEvent.GetType().Name,
                    topic
                );
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while publishing domain event {EventType} to MQTT",
                domainEvent.GetType().Name
            );
            return false;
        }
    }

    /// <summary>
    /// Publishes multiple domain events to MQTT topics.
    /// </summary>
    /// <param name="domainEvents">The domain events to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of events successfully published.</returns>
    public async Task<int> PublishDomainEventsAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default
    )
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MqttDomainEventPublisher));
        }

        ArgumentNullException.ThrowIfNull(domainEvents);

        var successCount = 0;
        var events = domainEvents.ToList();

        _logger.LogDebug("Publishing {EventCount} domain events to MQTT", events.Count);

        foreach (var domainEvent in events)
        {
            var success = await PublishDomainEventAsync(domainEvent, cancellationToken);
            if (success)
            {
                successCount++;
            }
        }

        _logger.LogInformation(
            "Published {SuccessCount} of {TotalCount} domain events to MQTT",
            successCount,
            events.Count
        );

        return successCount;
    }

    /// <summary>
    /// Generates an MQTT topic for a domain event based on its type and properties.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    /// <returns>The MQTT topic for the event.</returns>
    private static string GenerateTopicForEvent(IDomainEvent domainEvent)
    {
        var eventTypeName = domainEvent.GetType().Name;

        // Remove "Event" suffix if present
        if (eventTypeName.EndsWith("Event", StringComparison.OrdinalIgnoreCase))
        {
            eventTypeName = eventTypeName[..^5];
        }

        // Convert to snake_case for MQTT topic convention
        var topicName = ConvertToSnakeCase(eventTypeName);

        // Build topic hierarchy: snapdog/events/{category}/{event_name}
        var category = GetEventCategory(domainEvent);
        return $"snapdog/events/{category}/{topicName}";
    }

    /// <summary>
    /// Gets the event category based on the event type.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    /// <returns>The event category.</returns>
    private static string GetEventCategory(IDomainEvent domainEvent)
    {
        var eventType = domainEvent.GetType();
        var eventName = eventType.Name;

        // Categorize events based on their names
        return eventName switch
        {
            var name when name.Contains("Client") => "clients",
            var name when name.Contains("Volume") => "audio",
            var name when name.Contains("AudioStream") => "audio",
            var name when name.Contains("Playlist") => "playlists",
            var name when name.Contains("Zone") => "zones",
            _ => "general",
        };
    }

    /// <summary>
    /// Converts a PascalCase string to snake_case.
    /// </summary>
    /// <param name="input">The input string in PascalCase.</param>
    /// <returns>The string converted to snake_case.</returns>
    private static string ConvertToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var result = new System.Text.StringBuilder();

        for (int i = 0; i < input.Length; i++)
        {
            var currentChar = input[i];

            if (char.IsUpper(currentChar))
            {
                if (i > 0)
                {
                    result.Append('_');
                }
                result.Append(char.ToLowerInvariant(currentChar));
            }
            else
            {
                result.Append(currentChar);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Disposes the MQTT domain event publisher.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
