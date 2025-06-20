using MediatR;
using SnapDog2.Core.Configuration;

namespace SnapDog2.Server.Features.Knx.Commands;

/// <summary>
/// Command to read a value from a specific KNX group address.
/// </summary>
public record ReadGroupValueCommand : IRequest<byte[]?>
{
    /// <summary>
    /// Gets the KNX group address to read from.
    /// </summary>
    public required KnxAddress Address { get; init; }

    /// <summary>
    /// Gets the optional description for logging purposes.
    /// </summary>
    public string? Description { get; init; }
}
