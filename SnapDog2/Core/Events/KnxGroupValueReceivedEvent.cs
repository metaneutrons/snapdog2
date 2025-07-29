using SnapDog2.Core.Configuration;

namespace SnapDog2.Core.Events;

/// <summary>
/// Event published when a KNX group value is received.
/// </summary>
/// <param name="Address">The KNX group address</param>
/// <param name="Value">The received value as byte array</param>
public record KnxGroupValueReceivedEvent(KnxAddress Address, byte[] Value) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
}