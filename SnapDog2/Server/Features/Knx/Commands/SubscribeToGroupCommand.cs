using MediatR;
using SnapDog2.Core.Configuration;

namespace SnapDog2.Server.Features.Knx.Commands;

/// <summary>
/// Command to subscribe to value changes on a specific KNX group address.
/// </summary>
public record SubscribeToGroupCommand : IRequest<bool>
{
    /// <summary>
    /// Gets the KNX group address to monitor.
    /// </summary>
    public required KnxAddress Address { get; init; }

    /// <summary>
    /// Gets the optional description for logging purposes.
    /// </summary>
    public string? Description { get; init; }
}
