namespace SnapDog2.Server.Features.Clients.Queries;

using System.Collections.Generic;
using Cortex.Mediator.Queries;
using SnapDog2.Core.Models;

/// <summary>
/// Query to retrieve the state of all known clients.
/// </summary>
public record GetAllClientsQuery : IQuery<Result<List<ClientState>>>;

/// <summary>
/// Query to retrieve the state of a specific client.
/// </summary>
public record GetClientQuery : IQuery<Result<ClientState>>
{
    /// <summary>
    /// Gets the index of the client to retrieve (1-based).
    /// </summary>
    public required int ClientIndex { get; init; }
}

/// <summary>
/// Query to retrieve clients assigned to a specific zone.
/// </summary>
public record GetClientsByZoneQuery : IQuery<Result<List<ClientState>>>
{
    /// <summary>
    /// Gets the index of the zone (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }
}
