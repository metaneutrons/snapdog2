namespace SnapDog2.Infrastructure.Integrations.Snapcast;

using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;
using SnapcastClient.Models;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;

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
    private Server _serverInfo = new Server();
    private readonly object _serverInfoLock = new();
    private readonly ILogger<SnapcastStateRepository> _logger = logger;
    private readonly SnapDogConfiguration _configuration = configuration;

    #region Logging

    [LoggerMessage(
        1,
        LogLevel.Debug,
        "Updating full Snapcast server state. Groups: {GroupCount}, Clients: {ClientCount}, Streams: {StreamCount}"
    )]
    private partial void LogUpdatingServerState(int groupCount, int clientCount, int streamCount);

    [LoggerMessage(2, LogLevel.Debug, "Updating Snapcast client {ClientIndex}")]
    private partial void LogUpdatingClient(string clientIndex);

    [LoggerMessage(3, LogLevel.Debug, "Removing Snapcast client {ClientIndex}")]
    private partial void LogRemovingClient(string clientIndex);

    [LoggerMessage(4, LogLevel.Debug, "Updating Snapcast group {GroupId}")]
    private partial void LogUpdatingGroup(string groupId);

    [LoggerMessage(5, LogLevel.Debug, "Removing Snapcast group {GroupId}")]
    private partial void LogRemovingGroup(string groupId);

    [LoggerMessage(6, LogLevel.Debug, "Updating Snapcast stream {StreamId}")]
    private partial void LogUpdatingStream(string streamId);

    [LoggerMessage(7, LogLevel.Debug, "Removing Snapcast stream {StreamId}")]
    private partial void LogRemovingStream(string streamId);

    [LoggerMessage(8, LogLevel.Warning, "Client index {ClientIndex} is out of range. Valid range: 1-{MaxClients}")]
    private partial void LogClientIndexOutOfRange(int clientIndex, int maxClients);

    [LoggerMessage(9, LogLevel.Warning, "Client {ClientIndex} ({ClientName}) has no MAC address configured")]
    private partial void LogClientMacNotConfigured(int clientIndex, string clientName);

    [LoggerMessage(
        10,
        LogLevel.Warning,
        "Client {ClientIndex} ({ClientName}) with MAC {MacAddress} not found in Snapcast"
    )]
    private partial void LogClientNotFoundByMac(int clientIndex, string clientName, string macAddress);

    [LoggerMessage(
        11,
        LogLevel.Debug,
        "Client {ClientIndex} ({ClientName}) with MAC {MacAddress} found as Snapcast client {SnapcastClientIndex}"
    )]
    private partial void LogClientFoundByMac(
        int clientIndex,
        string clientName,
        string macAddress,
        string snapcastClientIndex
    );

    [LoggerMessage(12, LogLevel.Debug, "Available Snapcast clients: {AvailableClients}")]
    private partial void LogAvailableClients(string availableClients);

    [LoggerMessage(13, LogLevel.Debug, "Total Snapcast clients in repository: {ClientCount}")]
    private partial void LogTotalClientCount(int clientCount);

    [LoggerMessage(14, LogLevel.Debug, "🔄 UpdateServerState called with {GroupCount} groups")]
    private partial void LogUpdateServerStateCalled(int groupCount);

    [LoggerMessage(15, LogLevel.Debug, "📝 Processing group {GroupId} with {ClientCount} clients")]
    private partial void LogProcessingGroup(string groupId, int clientCount);

    [LoggerMessage(16, LogLevel.Debug, "👤 Adding/updating client {ClientIndex} (MAC: {MacAddress})")]
    private partial void LogAddingClient(string clientIndex, string macAddress);

    #endregion

    #region Server State Management

    public void UpdateServerState(Server server)
    {
        var allClients =
            server.Groups?.SelectMany(g => g.Clients).DistinctBy(c => c.Id) ?? Enumerable.Empty<SnapClient>();
        var clientCount = allClients.Count();
        var groupCount = server.Groups?.Count ?? 0;
        var streamCount = server.Streams?.Count ?? 0;

        this.LogUpdateServerStateCalled(groupCount);
        this.LogUpdatingServerState(groupCount, clientCount, streamCount);

        // Debug: Log each group and its clients
        if (server.Groups != null)
        {
            foreach (var group in server.Groups)
            {
                this.LogProcessingGroup(group.Id, group.Clients?.Count ?? 0);

                if (group.Clients != null)
                {
                    foreach (var client in group.Clients)
                    {
                        this.LogAddingClient(client.Id, client.Host.Mac ?? "unknown");
                    }
                }
            }
        }

        // Update server info
        lock (this._serverInfoLock)
        {
            this._serverInfo = server;
        }

        // Update groups
        var newGroups = server.Groups?.ToDictionary(g => g.Id, g => g) ?? new Dictionary<string, Group>();
        UpdateDictionary(this._groups, newGroups);

        // Update clients from all groups
        var newClients = allClients.ToDictionary(c => c.Id, c => c);
        UpdateDictionary(this._clients, newClients);

        // Debug: Log final client count
        this.LogTotalClientCount(this._clients.Count);

        // Update streams
        var newStreams = server.Streams?.ToDictionary(s => s.Id, s => s) ?? new Dictionary<string, Stream>();
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
        else
        {
            this.LogClientFoundByMac(clientIndex, clientConfig.Name, clientConfig.Mac, matchingClient.Id);
        }

        return matchingClient;
    }

    public IEnumerable<SnapClient> GetAllClients()
    {
        return this._clients.Values.ToList(); // Return a copy to avoid concurrent modification
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
