using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SnapDog2.Core.Models.Entities;

namespace SnapDog2.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Zone entities with domain-specific operations.
/// Extends the base repository with zone-specific query methods.
/// </summary>
public interface IZoneRepository : IRepository<Zone, string>
{
    /// <summary>
    /// Retrieves all zones that have clients assigned to them.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of zones with clients.</returns>
    Task<IEnumerable<Zone>> GetZonesWithClientsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a zone by its name.
    /// </summary>
    /// <param name="name">The zone name to search for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The zone if found; otherwise, null.</returns>
    Task<Zone?> GetZoneByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active zones (enabled and optionally with clients).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of active zones.</returns>
    Task<IEnumerable<Zone>> GetActiveZonesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of clients assigned to a specific zone.
    /// </summary>
    /// <param name="zoneId">The zone ID to count clients for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The number of clients in the zone.</returns>
    Task<int> GetClientCountInZoneAsync(string zoneId, CancellationToken cancellationToken = default);
}
