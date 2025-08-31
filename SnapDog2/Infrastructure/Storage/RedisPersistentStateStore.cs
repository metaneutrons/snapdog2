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
namespace SnapDog2.Infrastructure.Storage;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.Resilience;
using SnapDog2.Core.Models;
using StackExchange.Redis;

/// <summary>
/// Redis-based implementation of persistent state storage.
/// Provides fast, reliable state persistence with atomic operations.
/// </summary>
public partial class RedisPersistentStateStore : IPersistentStateStore, IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisPersistentStateStore> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline _connectionPipeline;
    private readonly ResiliencePipeline _operationPipeline;

    // Redis key prefixes
    private const string ZoneStatePrefix = "snapdog:zone:";
    private const string ClientStatePrefix = "snapdog:client:";
    private const string ConfigFingerprintKey = "snapdog:config:fingerprint";
    private const string StatsKey = "snapdog:stats";

    public RedisPersistentStateStore(
        IConnectionMultiplexer redis,
        RedisConfig redisConfig,
        ILogger<RedisPersistentStateStore> logger)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _logger = logger;

        // Create resilience pipelines using approved pattern
        _connectionPipeline = ResiliencePolicyFactory.CreatePipeline(
            redisConfig.Resilience.Connection,
            "Redis-Connection"
        );
        _operationPipeline = ResiliencePolicyFactory.CreatePipeline(
            redisConfig.Resilience.Operation,
            "Redis-Operation"
        );

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    #region Zone State Persistence

    public async Task SaveZoneStateAsync(int zoneIndex, ZoneState zoneState)
    {
        try
        {
            var key = $"{ZoneStatePrefix}{zoneIndex}";
            var json = JsonSerializer.Serialize(zoneState, _jsonOptions);

            await _operationPipeline.ExecuteAsync(async _ =>
            {
                await _database.StringSetAsync(key, json);
                await UpdateStatsAsync("zone", zoneIndex);
            });

            LogZoneStateSaved(zoneIndex, zoneState.Name);
        }
        catch (Exception ex)
        {
            LogZoneStateSaveFailed(ex, zoneIndex);
            throw;
        }
    }

    public async Task<Dictionary<int, ZoneState>> LoadAllZoneStatesAsync()
    {
        try
        {
            return await _operationPipeline.ExecuteAsync(async _ =>
            {
                var pattern = $"{ZoneStatePrefix}*";
                var keys = await GetKeysAsync(pattern);
                var states = new Dictionary<int, ZoneState>();

                foreach (var key in keys)
                {
                    var json = await _database.StringGetAsync(key);
                    if (json.HasValue)
                    {
                        var zoneState = JsonSerializer.Deserialize<ZoneState>(json!, _jsonOptions);
                        if (zoneState != null)
                        {
                            // Extract zone index from key
                            var keyStr = key.ToString();
                            var indexStr = keyStr.Substring(ZoneStatePrefix.Length);
                            if (int.TryParse(indexStr, out var zoneIndex))
                            {
                                states[zoneIndex] = zoneState;
                            }
                        }
                    }
                }

                LogZoneStatesLoaded(states.Count);
                return states;
            });
        }
        catch (Exception ex)
        {
            LogZoneStatesLoadFailed(ex);
            throw;
        }
    }

    #endregion

    #region Client State Persistence

    public async Task SaveClientStateAsync(int clientIndex, ClientState clientState)
    {
        try
        {
            var key = $"{ClientStatePrefix}{clientIndex}";
            var json = JsonSerializer.Serialize(clientState, _jsonOptions);

            await _operationPipeline.ExecuteAsync(async _ =>
            {
                await _database.StringSetAsync(key, json);
                await UpdateStatsAsync("client", clientIndex);
            });

            LogClientStateSaved(clientIndex, clientState.Name);
        }
        catch (Exception ex)
        {
            LogClientStateSaveFailed(ex, clientIndex);
            throw;
        }
    }

    public async Task<Dictionary<int, ClientState>> LoadAllClientStatesAsync()
    {
        try
        {
            var pattern = $"{ClientStatePrefix}*";
            var keys = await GetKeysAsync(pattern);
            var states = new Dictionary<int, ClientState>();

            foreach (var key in keys)
            {
                var json = await _database.StringGetAsync(key);
                if (json.HasValue)
                {
                    var clientState = JsonSerializer.Deserialize<ClientState>(json!, _jsonOptions);
                    if (clientState != null)
                    {
                        // Extract client index from key
                        var keyStr = key.ToString();
                        var indexStr = keyStr.Substring(ClientStatePrefix.Length);
                        if (int.TryParse(indexStr, out var clientIndex))
                        {
                            states[clientIndex] = clientState;
                        }
                    }
                }
            }

            LogClientStatesLoaded(states.Count);
            return states;
        }
        catch (Exception ex)
        {
            LogClientStatesLoadFailed(ex);
            throw;
        }
    }

    #endregion

    #region Configuration Management

    public async Task<ConfigurationFingerprint?> GetConfigurationFingerprintAsync()
    {
        try
        {
            var json = await _database.StringGetAsync(ConfigFingerprintKey);
            if (!json.HasValue)
            {
                return null;
            }

            return JsonSerializer.Deserialize<ConfigurationFingerprint>(json!, _jsonOptions);
        }
        catch (Exception ex)
        {
            LogConfigFingerprintLoadFailed(ex);
            return null;
        }
    }

    public async Task SaveConfigurationFingerprintAsync(ConfigurationFingerprint fingerprint)
    {
        try
        {
            var json = JsonSerializer.Serialize(fingerprint, _jsonOptions);
            await _database.StringSetAsync(ConfigFingerprintKey, json);

            LogConfigFingerprintSaved(fingerprint.Hash);
        }
        catch (Exception ex)
        {
            LogConfigFingerprintSaveFailed(ex);
            throw;
        }
    }

    public async Task ClearAllStateAsync()
    {
        try
        {
            // Clear zone states
            var zoneKeys = await GetKeysAsync($"{ZoneStatePrefix}*");
            if (zoneKeys.Any())
            {
                await _database.KeyDeleteAsync(zoneKeys.ToArray());
            }

            // Clear client states
            var clientKeys = await GetKeysAsync($"{ClientStatePrefix}*");
            if (clientKeys.Any())
            {
                await _database.KeyDeleteAsync(clientKeys.ToArray());
            }

            // Clear configuration fingerprint
            await _database.KeyDeleteAsync(ConfigFingerprintKey);

            // Clear stats
            await _database.KeyDeleteAsync(StatsKey);

            LogAllStateCleared();
        }
        catch (Exception ex)
        {
            LogAllStateClearFailed(ex);
            throw;
        }
    }

    #endregion

    #region Health and Diagnostics

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            return await _connectionPipeline.ExecuteAsync(async _ =>
            {
                // Simple ping test
                var pong = await _database.PingAsync();
                return pong.TotalMilliseconds < 1000; // Consider healthy if ping < 1s
            });
        }
        catch
        {
            return false;
        }
    }

    public async Task<PersistentStoreStats> GetStatsAsync()
    {
        try
        {
            var zoneKeys = await GetKeysAsync($"{ZoneStatePrefix}*");
            var clientKeys = await GetKeysAsync($"{ClientStatePrefix}*");

            var statsJson = await _database.StringGetAsync(StatsKey);
            var lastSaveTime = DateTime.UtcNow;

            if (statsJson.HasValue)
            {
                var statsData = JsonSerializer.Deserialize<Dictionary<string, object>>(statsJson!, _jsonOptions);
                if (statsData?.TryGetValue("lastSaveTime", out var lastSaveObj) == true)
                {
                    if (DateTime.TryParse(lastSaveObj.ToString(), out var parsedTime))
                    {
                        lastSaveTime = parsedTime;
                    }
                }
            }

            // Estimate storage size (rough approximation)
            var storageSizeBytes = (zoneKeys.Count() + clientKeys.Count()) * 1024; // Rough estimate

            return new PersistentStoreStats
            {
                ZoneStatesCount = zoneKeys.Count(),
                ClientStatesCount = clientKeys.Count(),
                LastSaveTime = lastSaveTime,
                StorageSizeBytes = storageSizeBytes,
                IsHealthy = await IsHealthyAsync()
            };
        }
        catch (Exception ex)
        {
            LogStatsRetrievalFailed(ex);

            return new PersistentStoreStats
            {
                ZoneStatesCount = 0,
                ClientStatesCount = 0,
                LastSaveTime = DateTime.MinValue,
                StorageSizeBytes = 0,
                IsHealthy = false
            };
        }
    }

    #endregion

    #region Private Helper Methods

    private Task<IEnumerable<RedisKey>> GetKeysAsync(string pattern)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        return Task.FromResult(server.Keys(pattern: pattern));
    }

    private async Task UpdateStatsAsync(string type, int index)
    {
        try
        {
            var stats = new Dictionary<string, object>
            {
                ["lastSaveTime"] = DateTime.UtcNow.ToString("O"),
                [$"last{type}Index"] = index
            };

            var json = JsonSerializer.Serialize(stats, _jsonOptions);
            await _database.StringSetAsync(StatsKey, json);
        }
        catch
        {
            // Stats update failure shouldn't break the main operation
        }
    }

    #endregion

    #region LoggerMessage Methods

    [LoggerMessage(
        EventId = 7300,
        Level = LogLevel.Debug,
        Message = "💾 Zone {ZoneIndex} ({ZoneName}) state saved to Redis"
    )]
    private partial void LogZoneStateSaved(int ZoneIndex, string ZoneName);

    [LoggerMessage(
        EventId = 7301,
        Level = LogLevel.Error,
        Message = "❌ Failed to save zone {ZoneIndex} state to Redis"
    )]
    private partial void LogZoneStateSaveFailed(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 7302,
        Level = LogLevel.Information,
        Message = "📥 Loaded {Count} zone states from Redis"
    )]
    private partial void LogZoneStatesLoaded(int Count);

    [LoggerMessage(
        EventId = 7303,
        Level = LogLevel.Error,
        Message = "❌ Failed to load zone states from Redis"
    )]
    private partial void LogZoneStatesLoadFailed(Exception ex);

    [LoggerMessage(
        EventId = 7304,
        Level = LogLevel.Debug,
        Message = "💾 Client {ClientIndex} ({ClientName}) state saved to Redis"
    )]
    private partial void LogClientStateSaved(int ClientIndex, string ClientName);

    [LoggerMessage(
        EventId = 7305,
        Level = LogLevel.Error,
        Message = "❌ Failed to save client {ClientIndex} state to Redis"
    )]
    private partial void LogClientStateSaveFailed(Exception ex, int ClientIndex);

    [LoggerMessage(
        EventId = 7306,
        Level = LogLevel.Information,
        Message = "📥 Loaded {Count} client states from Redis"
    )]
    private partial void LogClientStatesLoaded(int Count);

    [LoggerMessage(
        EventId = 7307,
        Level = LogLevel.Error,
        Message = "❌ Failed to load client states from Redis"
    )]
    private partial void LogClientStatesLoadFailed(Exception ex);

    [LoggerMessage(
        EventId = 7308,
        Level = LogLevel.Debug,
        Message = "🔍 Configuration fingerprint saved: {Hash}"
    )]
    private partial void LogConfigFingerprintSaved(string Hash);

    [LoggerMessage(
        EventId = 7309,
        Level = LogLevel.Error,
        Message = "❌ Failed to save configuration fingerprint"
    )]
    private partial void LogConfigFingerprintSaveFailed(Exception ex);

    [LoggerMessage(
        EventId = 7310,
        Level = LogLevel.Error,
        Message = "❌ Failed to load configuration fingerprint"
    )]
    private partial void LogConfigFingerprintLoadFailed(Exception ex);

    [LoggerMessage(
        EventId = 7311,
        Level = LogLevel.Warning,
        Message = "🧹 All persistent state cleared from Redis"
    )]
    private partial void LogAllStateCleared();

    [LoggerMessage(
        EventId = 7312,
        Level = LogLevel.Error,
        Message = "❌ Failed to clear all state from Redis"
    )]
    private partial void LogAllStateClearFailed(Exception ex);

    [LoggerMessage(
        EventId = 7313,
        Level = LogLevel.Error,
        Message = "❌ Failed to retrieve stats from Redis"
    )]
    private partial void LogStatsRetrievalFailed(Exception ex);

    #endregion

    public void Dispose()
    {
        // Redis connection is managed by DI container, don't dispose here
    }
}
