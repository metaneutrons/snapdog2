using MediatR;
using SnapDog2.Core.Configuration;

namespace SnapDog2.Server.Features.Knx.Commands;

/// <summary>
/// Command to unsubscribe from value changes on a specific KNX group address.
/// </summary>
public record UnsubscribeFromGroupCommand : IRequest<bool>
{
    /// <summary>
    /// Gets the KNX group address to stop monitoring.
    /// </summary>
    public required KnxAddress Address { get; init; }

    /// <summary>
    /// Gets the optional description for logging purposes.
    /// </summary>
    public string? Description { get; init; }
}
