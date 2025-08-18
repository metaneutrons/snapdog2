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
    /// <param name="clientIndex">The client ID.</param>
    /// <returns>A result containing the client if found.</returns>
    Task<Result<IClient>> GetClientAsync(int clientIndex);

    /// <summary>
    /// Gets a client by its ID.
    /// </summary>
    /// <param name="clientIndex">The client ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the client state if found.</returns>
    Task<Result<ClientState>> GetClientAsync(int clientIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the state of a specific client.
    /// </summary>
    /// <param name="clientIndex">The client ID.</param>
    /// <returns>A result containing the client state if found.</returns>
    Task<Result<ClientState>> GetClientStateAsync(int clientIndex);

    /// <summary>
    /// Gets the state of all known clients.
    /// </summary>
    /// <returns>A result containing the list of all client states.</returns>
    Task<Result<List<ClientState>>> GetAllClientsAsync();

    /// <summary>
    /// Gets the state of all known clients.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the list of all client states.</returns>
    Task<Result<List<ClientState>>> GetAllClientsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all clients assigned to a specific zone.
    /// </summary>
    /// <param name="zoneIndex">The zone ID.</param>
    /// <returns>A result containing the list of client states for the zone.</returns>
    Task<Result<List<ClientState>>> GetClientsByZoneAsync(int zoneIndex);

    /// <summary>
    /// Gets all clients assigned to a specific zone.
    /// </summary>
    /// <param name="zoneIndex">The zone ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the list of client states for the zone.</returns>
    Task<Result<List<ClientState>>> GetClientsByZoneAsync(int zoneIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a client to a zone.
    /// </summary>
    /// <param name="clientIndex">The client ID.</param>
    /// <param name="zoneIndex">The zone ID.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> AssignClientToZoneAsync(int clientIndex, int zoneIndex);

    /// <summary>
    /// Gets a client by its Snapcast client ID for event bridging.
    /// Used internally by SnapcastService to bridge external events to IClient notifications.
    /// </summary>
    /// <param name="snapcastClientId">The Snapcast client ID.</param>
    /// <returns>The client if found, null otherwise.</returns>
    Task<IClient?> GetClientBySnapcastIdAsync(string snapcastClientId);
}
