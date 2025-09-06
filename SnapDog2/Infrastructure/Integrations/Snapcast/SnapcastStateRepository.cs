//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
namespace SnapDog2.Infrastructure.Integrations.Snapcast;

using System.Collections.Concurrent;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Infrastructure.Integrations.Snapcast.Models;
using SnapDog2.Shared.Configuration;

/// <summary>
/// Thread-safe repository holding the last known state received from Snapcast server.
/// Uses raw SnapcastClient models to maintain fidelity with the external system.
/// </summary>
public partial class SnapcastStateRepository(
    ILogger<SnapcastStateRepository> logger,
    SnapDogConfiguration configuration
) : ISnapcastStateRepository
{
    private readonly ConcurrentDictionary<string, SnapClient> _clients = new();
    private readonly ConcurrentDictionary<string, Group> _groups = new();
    private readonly ConcurrentDictionary<string, Stream> _streams = new();
    private Server _serverInfo;
    private readonly object _serverInfoLock = new();
    private readonly ILogger<SnapcastStateRepository> _logger = logger;
    private readonly SnapDogConfiguration _configuration = configuration;

    #region Logging

    [LoggerMessage(
        EventId = 7300,
        Level = LogLevel.Debug,
        Message = "Updating full Snapcast server state. Groups: {GroupCount}, Clients: {ClientCount}, Streams: {StreamCount}"
    )]
    private partial void LogUpdatingServerState(int groupCount, int clientCount, int streamCount);

    [LoggerMessage(
        EventId = 7301,
        Level = LogLevel.Debug,
        Message = "Updating Snapcast client {ClientIndex}"
    )]
    private partial void LogUpdatingClient(string clientIndex);

    [LoggerMessage(
        EventId = 7302,
        Level = LogLevel.Debug,
        Message = "Removing Snapcast client {ClientIndex}"
    )]
    private partial void LogRemovingClient(string clientIndex);

    [LoggerMessage(
        EventId = 7303,
        Level = LogLevel.Debug,
        Message = "Updating Snapcast group {GroupId}"
    )]
    private partial void LogUpdatingGroup(string groupId);

    [LoggerMessage(
        EventId = 7304,
        Level = LogLevel.Debug,
        Message = "Removing Snapcast group {GroupId}"
    )]
    private partial void LogRemovingGroup(string groupId);

    [LoggerMessage(
        EventId = 7305,
        Level = LogLevel.Debug,
        Message = "Updating Snapcast stream {StreamId}"
    )]
    private partial void LogUpdatingStream(string streamId);

    [LoggerMessage(
        EventId = 7306,
        Level = LogLevel.Debug,
        Message = "Removing Snapcast stream {StreamId}"
    )]
    private partial void LogRemovingStream(string streamId);

    [LoggerMessage(
        EventId = 7307,
        Level = LogLevel.Warning,
        Message = "Client index {ClientIndex} is out of range. Valid range: 1-{MaxClients}"
    )]
    private partial void LogClientIndexOutOfRange(int clientIndex, int maxClients);

    [LoggerMessage(
        EventId = 7308,
        Level = LogLevel.Warning,
        Message = "Client {ClientIndex} ({ClientName}) has no MAC address configured"
    )]
    private partial void LogClientMacNotConfigured(int clientIndex, string clientName);

    [LoggerMessage(
        EventId = 7309,
        Level = LogLevel.Warning,
        Message = "Client {ClientIndex} ({ClientName}) with MAC {MacAddress} not found in Snapcast"
    )]
    private partial void LogClientNotFoundByMac(int clientIndex, string clientName, string macAddress);

    [LoggerMessage(
        EventId = 7310,
        Level = LogLevel.Debug,
        Message = "Client {ClientIndex} ({ClientName}) with MAC {MacAddress} found as Snapcast client {SnapcastClientId}"
    )]
    private partial void LogClientFoundByMac(
        int clientIndex,
        string clientName,
        string macAddress,
        string snapcastClientId
    );

    [LoggerMessage(
        EventId = 7311,
        Level = LogLevel.Debug,
        Message = "Available Snapcast clients: {AvailableClients}"
    )]
    private partial void LogAvailableClients(string availableClients);

    [LoggerMessage(
        EventId = 7312,
        Level = LogLevel.Debug,
        Message = "Total Snapcast clients in repository: {ClientCount}"
    )]
    private partial void LogTotalClientCount(int clientCount);

    [LoggerMessage(
        EventId = 7313,
        Level = LogLevel.Debug,
        Message = "🔄 UpdateServerState called with {GroupCount} groups"
    )]
    private partial void LogUpdateServerStateCalled(int groupCount);

    [LoggerMessage(
        EventId = 7314,
        Level = LogLevel.Debug,
        Message = "📝 Processing group {GroupId} with {ClientCount} clients"
    )]
    private partial void LogProcessingGroup(string groupId, int clientCount);

    [LoggerMessage(
        EventId = 7315,
        Level = LogLevel.Debug,
        Message = "👤 Adding/updating client {ClientIndex} (MAC: {MacAddress})"
    )]
    private partial void LogAddingClient(string clientIndex, string macAddress);

    #endregion

    #region Server State Management

    public void UpdateServerState(Server server)
    {
        var allClients =
            server.Groups.SelectMany(g => g.Clients).DistinctBy(c => c.Id);
        var clientCount = allClients.Count();
        var groupCount = server.Groups.Length;
        var streamCount = server.Streams.Length;

        this.LogUpdateServerStateCalled(groupCount);
        this.LogUpdatingServerState(groupCount, clientCount, streamCount);

        // Debug: Log each group and its clients
        foreach (var group in server.Groups)
        {
            this.LogProcessingGroup(group.Id, group.Clients.Length);

            foreach (var client in group.Clients)
            {
                this.LogAddingClient(client.Id, client.Host.Mac);
            }
        }

        // Update server info
        lock (this._serverInfoLock)
        {
            this._serverInfo = server;
        }

        // Update groups
        var newGroups = server.Groups.ToDictionary(g => g.Id, g => g);
        UpdateDictionary(this._groups, newGroups);

        // Update clients from all groups
        var newClients = allClients.ToDictionary(c => c.Id, c => c);
        UpdateDictionary(this._clients, newClients);

        // Debug: Log final client count
        this.LogTotalClientCount(this._clients.Count);

        // Update streams
        var newStreams = server.Streams.ToDictionary(s => s.Id, s => s);
        UpdateDictionary(this._streams, newStreams);
    }

    public Server GetServerInfo()
    {
        lock (this._serverInfoLock)
        {
            return this._serverInfo;
        }
    }

    #endregion

    #region Client Management

    public void UpdateClient(SnapClient client)
    {
        this.LogUpdatingClient(client.Id);
        this._clients[client.Id] = client;
    }

    public void RemoveClient(string clientIndex)
    {
        this.LogRemovingClient(clientIndex);
        this._clients.TryRemove(clientIndex, out _);
    }

    public SnapClient? GetClient(string clientIndex)
    {
        return this._clients.TryGetValue(clientIndex, out var client) ? client : null;
    }

    public SnapClient? GetClientByIndex(int clientIndex)
    {
        // Convert 1-based index to 0-based array index
        var arrayIndex = clientIndex - 1;

        // Check if index is valid
        if (arrayIndex < 0 || arrayIndex >= this._configuration.Clients.Count)
        {
            this.LogClientIndexOutOfRange(clientIndex, this._configuration.Clients.Count);
            return null;
        }

        var clientConfig = this._configuration.Clients[arrayIndex];

        // If no MAC address configured, we can't look up the client
        if (string.IsNullOrEmpty(clientConfig.Mac))
        {
            this.LogClientMacNotConfigured(clientIndex, clientConfig.Name);
            return null;
        }

        // Try to find client by MAC address
        var matchingClient = this._clients.Values.FirstOrDefault(c =>
            string.Equals(c.Host.Mac, clientConfig.Mac, StringComparison.OrdinalIgnoreCase)
        );

        if (matchingClient.Id == null) // SnapClient is a struct, check if ID is null/empty
        {
            this.LogClientNotFoundByMac(clientIndex, clientConfig.Name, clientConfig.Mac);
            // Log available clients for debugging
            var availableClients = this._clients.Values.Select(c => $"{c.Id} (MAC: {c.Host.Mac})");
            this.LogAvailableClients(string.Join(", ", availableClients));

            // Also log total client count for debugging
            this.LogTotalClientCount(this._clients.Count);
            return null;
        }

        this.LogClientFoundByMac(clientIndex, clientConfig.Name, clientConfig.Mac, matchingClient.Id);

        return matchingClient;
    }

    public IEnumerable<SnapClient> GetAllClients()
    {
        return this._clients.Values.ToList(); // Return a copy to avoid concurrent modification
    }

    /// <summary>
    /// Gets the SnapDog client index (1-based) for a given Snapcast client ID by matching MAC addresses.
    /// </summary>
    /// <param name="snapcastClientId">The Snapcast client ID (e.g., "bedroom", "living-room")</param>
    /// <returns>The 1-based client index, or null if not found</returns>
    public int? GetClientIndexBySnapcastId(string snapcastClientId)
    {
        if (string.IsNullOrEmpty(snapcastClientId))
        {
            return null;
        }

        // Find the Snapcast client with this ID
        if (!this._clients.TryGetValue(snapcastClientId, out var snapcastClient))
        {
            return null;
        }

        // Find the matching SnapDog client config by MAC address
        for (int i = 0; i < this._configuration.Clients.Count; i++)
        {
            var clientConfig = this._configuration.Clients[i];
            if (string.Equals(clientConfig.Mac, snapcastClient.Host.Mac, StringComparison.OrdinalIgnoreCase))
            {
                return i + 1; // Convert 0-based to 1-based index
            }
        }

        return null;
    }

    #endregion

    #region Group Management

    public void UpdateGroup(Group group)
    {
        this.LogUpdatingGroup(group.Id);
        this._groups[group.Id] = group;

        // Also update all clients in this group
        foreach (var client in group.Clients)
        {
            this.UpdateClient(client);
        }
    }

    public void RemoveGroup(string groupId)
    {
        this.LogRemovingGroup(groupId);
        this._groups.TryRemove(groupId, out _);
    }

    public Group? GetGroup(string groupId)
    {
        return this._groups.TryGetValue(groupId, out var group) ? group : null;
    }

    public IEnumerable<Group> GetAllGroups()
    {
        return this._groups.Values.ToList(); // Return a copy to avoid concurrent modification
    }

    #endregion

    #region Stream Management

    public void UpdateStream(Stream stream)
    {
        this.LogUpdatingStream(stream.Id);
        this._streams[stream.Id] = stream;
    }

    public void RemoveStream(string streamId)
    {
        this.LogRemovingStream(streamId);
        this._streams.TryRemove(streamId, out _);
    }

    public Stream? GetStream(string streamId)
    {
        return this._streams.TryGetValue(streamId, out var stream) ? stream : null;
    }

    public IEnumerable<Stream> GetAllStreams()
    {
        return this._streams.Values.ToList(); // Return a copy to avoid concurrent modification
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Updates a concurrent dictionary by removing keys not in source and adding/updating keys from source.
    /// </summary>
    private static void UpdateDictionary<TKey, TValue>(
        ConcurrentDictionary<TKey, TValue> target,
        IDictionary<TKey, TValue> source
    )
        where TKey : notnull
    {
        // Remove keys that are no longer present
        foreach (var key in target.Keys.Except(source.Keys))
        {
            target.TryRemove(key, out _);
        }

        // Add or update keys from source
        foreach (var kvp in source)
        {
            target[kvp.Key] = kvp.Value;
        }
    }

    #endregion
}
