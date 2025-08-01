using FluentValidation;
using SnapDog2.Server.Features.Mqtt.Commands;

namespace SnapDog2.Server.Features.Mqtt.Validators;

/// <summary>
/// Validator for the PublishMqttMessageCommand.
/// </summary>
public class PublishMqttMessageValidator : AbstractValidator<PublishMqttMessageCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PublishMqttMessageValidator"/> class.
    /// </summary>
    public PublishMqttMessageValidator()
    {
        RuleFor(static x => x.Topic)
            .NotEmpty()
            .WithMessage("Topic is required")
            .MaximumLength(1000)
            .WithMessage("Topic cannot exceed 1000 characters")
            .Must(BeValidMqttTopic)
            .WithMessage(
                "Topic contains invalid characters. MQTT topics cannot contain wildcards (+, #) when publishing"
            );

        RuleFor(static x => x.Payload)
            .NotNull()
            .WithMessage("Payload cannot be null")
            .MaximumLength(268435456) // 256 MB limit
            .WithMessage("Payload cannot exceed 256 MB");

        RuleFor(static x => x.QoS)
            .IsInEnum()
            .WithMessage("QoS must be a valid MQTT quality of service level (0, 1, or 2)");
    }

    /// <summary>
    /// Validates that the topic is a valid MQTT topic for publishing.
    /// </summary>
    /// <param name="topic">The topic to validate.</param>
    /// <returns>True if the topic is valid for publishing, false otherwise.</returns>
    private static bool BeValidMqttTopic(string topic)
    {
        if (string.IsNullOrEmpty(topic))
        {
            return false;
        }

        // MQTT topics for publishing cannot contain wildcards
        if (topic.Contains('+') || topic.Contains('#'))
        {
            return false;
        }

        // Topic cannot start or end with /
        if (topic.StartsWith('/') || topic.EndsWith('/'))
        {
            return false;
        }

        // Topic cannot contain null characters
        if (topic.Contains('\0'))
        {
            return false;
        }

        // Topic levels cannot be empty (no consecutive slashes)
        if (topic.Contains("//"))
        {
            return false;
        }

        return true;
    }
}
