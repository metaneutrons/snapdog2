using FluentValidation;
using SnapDog2.Server.Features.Mqtt.Commands;

namespace SnapDog2.Server.Features.Mqtt.Validators;

/// <summary>
/// Validator for the UnsubscribeFromMqttTopicCommand.
/// </summary>
public class UnsubscribeFromMqttTopicValidator : AbstractValidator<UnsubscribeFromMqttTopicCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsubscribeFromMqttTopicValidator"/> class.
    /// </summary>
    public UnsubscribeFromMqttTopicValidator()
    {
        RuleFor(static x => x.TopicPattern)
            .NotEmpty()
            .WithMessage("Topic pattern is required")
            .MaximumLength(1000)
            .WithMessage("Topic pattern cannot exceed 1000 characters")
            .Must(BeValidMqttTopicFilter)
            .WithMessage("Topic pattern contains invalid characters or format");
    }

    /// <summary>
    /// Validates that the topic pattern is a valid MQTT topic filter for unsubscriptions.
    /// </summary>
    /// <param name="topicPattern">The topic pattern to validate.</param>
    /// <returns>True if the topic pattern is valid for unsubscriptions, false otherwise.</returns>
    private static bool BeValidMqttTopicFilter(string topicPattern)
    {
        if (string.IsNullOrEmpty(topicPattern))
        {
            return false;
        }

        // Topic cannot contain null characters
        if (topicPattern.Contains('\0'))
        {
            return false;
        }

        // Topic cannot start with /
        if (topicPattern.StartsWith('/'))
        {
            return false;
        }

        // Topic levels cannot be empty (no consecutive slashes) unless it's a wildcard
        if (topicPattern.Contains("//"))
        {
            return false;
        }

        // Validate wildcard usage
        var levels = topicPattern.Split('/');
        for (int i = 0; i < levels.Length; i++)
        {
            var level = levels[i];

            // Single-level wildcard (+) must be alone in its level
            if (level.Contains('+') && level != "+")
            {
                return false;
            }

            // Multi-level wildcard (#) must be alone in its level and must be the last level
            if (level.Contains('#'))
            {
                if (level != "#" || i != levels.Length - 1)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
