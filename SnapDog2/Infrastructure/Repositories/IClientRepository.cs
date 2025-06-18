using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.ValueObjects;

namespace SnapDog2.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Client entities with domain-specific operations.
/// Extends the base repository with client-specific query methods.
/// </summary>
public interface IClientRepository : IRepository<Client, string>
{
    /// <summary>
    /// Retrieves a client by its MAC address.
    /// </summary>
    /// <param name="macAddress">The MAC address to search for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The client if found; otherwise, null.</returns>
    Task<Client?> GetByMacAddressAsync(MacAddress macAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all connected clients.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of connected clients.</returns>
    Task<IEnumerable<Client>> GetConnectedClientsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all clients assigned to a specific zone.
    /// </summary>
    /// <param name="zoneId">The zone ID to filter by.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of clients in the specified zone.</returns>
    Task<IEnumerable<Client>> GetClientsByZoneAsync(string zoneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all clients with volume above the specified minimum level.
    /// </summary>
    /// <param name="minVolume">The minimum volume level (0-100).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of clients with volume above the minimum level.</returns>
    Task<IEnumerable<Client>> GetClientsWithVolumeAboveAsync(
        int minVolume,
        CancellationToken cancellationToken = default
    );
}
