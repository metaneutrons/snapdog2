using MediatR;
using SnapDog2.Core.Configuration;

namespace SnapDog2.Server.Features.Knx.Commands;

/// <summary>
/// Command to write a value to a specific KNX group address.
/// </summary>
public record WriteGroupValueCommand : IRequest<bool>
{
    /// <summary>
    /// Gets the KNX group address to write to.
    /// </summary>
    public required KnxAddress Address { get; init; }

    /// <summary>
    /// Gets the value to write as byte array.
    /// </summary>
    public required byte[] Value { get; init; }

    /// <summary>
    /// Gets the optional description for logging purposes.
    /// </summary>
    public string? Description { get; init; }
}
