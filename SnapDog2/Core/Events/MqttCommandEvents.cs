namespace SnapDog2.Core.Events;

/// <summary>
/// Base class for MQTT command events.
/// </summary>
public abstract record MqttCommandEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
}

// Zone command events
public record MqttZoneVolumeCommandEvent(int ZoneId, int Volume) : MqttCommandEvent;
public record MqttZoneMuteCommandEvent(int ZoneId, bool Muted) : MqttCommandEvent;
public record MqttZoneStreamCommandEvent(int ZoneId, int StreamId) : MqttCommandEvent;

// Client command events
public record MqttClientVolumeCommandEvent(string ClientId, int Volume) : MqttCommandEvent;
public record MqttClientMuteCommandEvent(string ClientId, bool Muted) : MqttCommandEvent;

// Stream command events
public record MqttStreamStartCommandEvent(int StreamId) : MqttCommandEvent;
public record MqttStreamStopCommandEvent(int StreamId) : MqttCommandEvent;

// System command events
public record MqttSystemShutdownCommandEvent() : MqttCommandEvent;
public record MqttSystemRestartCommandEvent() : MqttCommandEvent;
public record MqttSystemSyncCommandEvent() : MqttCommandEvent;