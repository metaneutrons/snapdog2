namespace SnapDog2.Core.Abstractions;

using System.Collections.Generic;
using System.Threading.Tasks;
using SnapDog2.Core.Models;

/// <summary>
/// Provides management operations for Snapcast clients.
/// </summary>
public interface IClientManager
{
    /// <summary>
    /// Gets a client by its ID.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <returns>A result containing the client if found.</returns>
    Task<Result<IClient>> GetClientAsync(int clientId);

    /// <summary>
    /// Gets the state of a specific client.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <returns>A result containing the client state if found.</returns>
    Task<Result<ClientState>> GetClientStateAsync(int clientId);

    /// <summary>
    /// Gets the state of all known clients.
    /// </summary>
    /// <returns>A result containing the list of all client states.</returns>
    Task<Result<List<ClientState>>> GetAllClientsAsync();

    /// <summary>
    /// Gets all clients assigned to a specific zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <returns>A result containing the list of client states for the zone.</returns>
    Task<Result<List<ClientState>>> GetClientsByZoneAsync(int zoneId);

    /// <summary>
    /// Assigns a client to a zone.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="zoneId">The zone ID.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> AssignClientToZoneAsync(int clientId, int zoneId);
}
