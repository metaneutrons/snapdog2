using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Infrastructure.Data;

namespace SnapDog2.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Zone entities with domain-specific operations.
/// Provides Entity Framework Core-based data access for zones.
/// </summary>
public sealed class ZoneRepository : RepositoryBase<Zone, string>, IZoneRepository
{
    /// <summary>
    /// Initializes a new instance of the ZoneRepository class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    public ZoneRepository(SnapDogDbContext context)
        : base(context) { }

    /// <summary>
    /// Retrieves all zones that have clients assigned to them.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of zones with clients.</returns>
    public async Task<IEnumerable<Zone>> GetZonesWithClientsAsync(CancellationToken cancellationToken = default)
    {
        return await GetQueryableNoTracking()
            .Where(static zone => zone.ClientIds.Count > 0)
            .OrderBy(static zone => zone.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves a zone by its name.
    /// </summary>
    /// <param name="name">The zone name to search for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The zone if found; otherwise, null.</returns>
    /// <exception cref="ArgumentException">Thrown when name is null or empty.</exception>
    public async Task<Zone?> GetZoneByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Zone name cannot be null or empty.", nameof(name));
        }

        return await GetQueryableNoTracking()
            .FirstOrDefaultAsync(zone => zone.Name.ToLower() == name.ToLower(), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all active zones (enabled and optionally with clients).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of active zones.</returns>
    public async Task<IEnumerable<Zone>> GetActiveZonesAsync(CancellationToken cancellationToken = default)
    {
        return await GetQueryableNoTracking()
            .Where(static zone => zone.IsEnabled && zone.ClientIds.Count > 0)
            .OrderByDescending(static zone => zone.Priority)
            .ThenBy(static zone => zone.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the count of clients assigned to a specific zone.
    /// </summary>
    /// <param name="zoneId">The zone ID to count clients for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The number of clients in the zone.</returns>
    /// <exception cref="ArgumentException">Thrown when zone ID is null or empty.</exception>
    public async Task<int> GetClientCountInZoneAsync(string zoneId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(zoneId))
        {
            throw new ArgumentException("Zone ID cannot be null or empty.", nameof(zoneId));
        }

        var zone = await GetQueryableNoTracking()
            .Where(z => z.Id == zoneId)
            .Select(z => new { z.ClientIds })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return zone?.ClientIds.Count ?? 0;
    }
}
