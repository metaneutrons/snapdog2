namespace SnapDog2.Infrastructure.Services;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SnapcastClient.Models;
using SnapDog2.Core.Abstractions;

/// <summary>
/// Thread-safe repository holding the last known state received from Snapcast server.
/// Uses raw SnapcastClient models to maintain fidelity with the external system.
/// </summary>
public partial class SnapcastStateRepository : ISnapcastStateRepository
{
    private readonly ConcurrentDictionary<string, SnapClient> _clients = new();
    private readonly ConcurrentDictionary<string, Group> _groups = new();
    private readonly ConcurrentDictionary<string, Stream> _streams = new();
    private Server _serverInfo;
    private readonly object _serverInfoLock = new();
    private readonly ILogger<SnapcastStateRepository> _logger;

    public SnapcastStateRepository(ILogger<SnapcastStateRepository> logger)
    {
        this._logger = logger;
        this._serverInfo = new Server(); // Initialize with empty server info
    }

    #region Logging

    [LoggerMessage(
        1,
        LogLevel.Debug,
        "Updating full Snapcast server state. Groups: {GroupCount}, Clients: {ClientCount}, Streams: {StreamCount}"
    )]
    private partial void LogUpdatingServerState(int groupCount, int clientCount, int streamCount);

    [LoggerMessage(2, LogLevel.Debug, "Updating Snapcast client {ClientId}")]
    private partial void LogUpdatingClient(string clientId);

    [LoggerMessage(3, LogLevel.Debug, "Removing Snapcast client {ClientId}")]
    private partial void LogRemovingClient(string clientId);

    [LoggerMessage(4, LogLevel.Debug, "Updating Snapcast group {GroupId}")]
    private partial void LogUpdatingGroup(string groupId);

    [LoggerMessage(5, LogLevel.Debug, "Removing Snapcast group {GroupId}")]
    private partial void LogRemovingGroup(string groupId);

    [LoggerMessage(6, LogLevel.Debug, "Updating Snapcast stream {StreamId}")]
    private partial void LogUpdatingStream(string streamId);

    [LoggerMessage(7, LogLevel.Debug, "Removing Snapcast stream {StreamId}")]
    private partial void LogRemovingStream(string streamId);

    #endregion

    #region Server State Management

    public void UpdateServerState(Server server)
    {
        var allClients =
            server.Groups?.SelectMany(g => g.Clients).DistinctBy(c => c.Id) ?? Enumerable.Empty<SnapClient>();
        var clientCount = allClients.Count();
        var groupCount = server.Groups?.Count ?? 0;
        var streamCount = server.Streams?.Count ?? 0;

        this.LogUpdatingServerState(groupCount, clientCount, streamCount);

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

    public void RemoveClient(string clientId)
    {
        this.LogRemovingClient(clientId);
        this._clients.TryRemove(clientId, out _);
    }

    public SnapClient? GetClient(string clientId)
    {
        return this._clients.TryGetValue(clientId, out var client) ? client : null;
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
