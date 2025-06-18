using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;
using SnapDog2.Infrastructure.Data;

namespace SnapDog2.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Client entities with domain-specific operations.
/// Provides Entity Framework Core-based data access for clients.
/// </summary>
public sealed class ClientRepository : RepositoryBase<Client, string>, IClientRepository
{
    /// <summary>
    /// Initializes a new instance of the ClientRepository class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    public ClientRepository(SnapDogDbContext context)
        : base(context) { }

    /// <summary>
    /// Retrieves a client by its MAC address.
    /// </summary>
    /// <param name="macAddress">The MAC address to search for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The client if found; otherwise, null.</returns>
    public async Task<Client?> GetByMacAddressAsync(
        MacAddress macAddress,
        CancellationToken cancellationToken = default
    )
    {
        return await GetQueryableNoTracking()
            .FirstOrDefaultAsync(client => client.MacAddress.Equals(macAddress), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all connected clients.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of connected clients.</returns>
    public async Task<IEnumerable<Client>> GetConnectedClientsAsync(CancellationToken cancellationToken = default)
    {
        return await GetQueryableNoTracking()
            .Where(client => client.Status == ClientStatus.Connected)
            .OrderBy(client => client.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all clients assigned to a specific zone.
    /// </summary>
    /// <param name="zoneId">The zone ID to filter by.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of clients in the specified zone.</returns>
    /// <exception cref="ArgumentException">Thrown when zone ID is null or empty.</exception>
    public async Task<IEnumerable<Client>> GetClientsByZoneAsync(
        string zoneId,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(zoneId))
        {
            throw new ArgumentException("Zone ID cannot be null or empty.", nameof(zoneId));
        }

        return await GetQueryableNoTracking()
            .Where(client => client.ZoneId == zoneId)
            .OrderBy(client => client.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all clients with volume above the specified minimum level.
    /// </summary>
    /// <param name="minVolume">The minimum volume level (0-100).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of clients with volume above the minimum level.</returns>
    /// <exception cref="ArgumentException">Thrown when minimum volume is out of range.</exception>
    public async Task<IEnumerable<Client>> GetClientsWithVolumeAboveAsync(
        int minVolume,
        CancellationToken cancellationToken = default
    )
    {
        if (minVolume < 0 || minVolume > 100)
        {
            throw new ArgumentException("Minimum volume must be between 0 and 100.", nameof(minVolume));
        }

        return await GetQueryableNoTracking()
            .Where(client => !client.IsMuted && client.Volume > minVolume)
            .OrderByDescending(client => client.Volume)
            .ThenBy(client => client.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
