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
namespace SnapDog2.Shared.Attributes;

using System.Collections.Concurrent;
using System.Reflection;

/// <summary>
/// Registry for mapping StatusId strings to their corresponding notification types.
/// Provides compile-time safety and eliminates hardcoded strings throughout the codebase.
/// </summary>
public static class StatusIdRegistry
{
    private static readonly ConcurrentDictionary<string, Type> _statusIdToTypeMap = new();
    private static readonly ConcurrentDictionary<Type, string> _typeToStatusIdMap = new();
    private static bool _isInitialized = false;
    private static readonly object _initLock = new();

    /// <summary>
    /// Initialize the registry by scanning all loaded assemblies for StatusId attributes.
    /// This is called automatically on first access but can be called explicitly for performance.
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        lock (_initLock)
        {
            if (_isInitialized)
            {
                return;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var typesWithStatusId = assembly
                        .GetTypes()
                        .Where(type => type.GetCustomAttribute<StatusIdAttribute>() != null);

                    foreach (var type in typesWithStatusId)
                    {
                        var attribute = type.GetCustomAttribute<StatusIdAttribute>()!;
                        _statusIdToTypeMap.TryAdd(attribute.Id, type);
                        _typeToStatusIdMap.TryAdd(type, attribute.Id);
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip assemblies that can't be loaded
                }
            }

            _isInitialized = true;
        }
    }

    /// <summary>
    /// Get the StatusId for a notification type.
    /// </summary>
    /// <typeparam name="T">The notification type.</typeparam>
    /// <returns>The StatusId string.</returns>
    public static string GetStatusId<T>()
    {
        Initialize();

        if (_typeToStatusIdMap.TryGetValue(typeof(T), out var statusId))
        {
            return statusId;
        }

        // Fallback to attribute lookup for types not in registry
        return StatusIdAttribute.GetStatusId<T>();
    }

    /// <summary>
    /// Get the notification type for a StatusId string.
    /// </summary>
    /// <param name="statusId">The StatusId string.</param>
    /// <returns>The notification type, or null if not found.</returns>
    public static Type? GetNotificationType(string statusId)
    {
        Initialize();
        _statusIdToTypeMap.TryGetValue(statusId, out var type);
        return type;
    }

    /// <summary>
    /// Check if a StatusId is registered.
    /// </summary>
    /// <param name="statusId">The StatusId string to check.</param>
    /// <returns>True if the StatusId is registered.</returns>
    public static bool IsRegistered(string statusId)
    {
        Initialize();
        return _statusIdToTypeMap.ContainsKey(statusId);
    }

    /// <summary>
    /// Get all registered StatusIds.
    /// </summary>
    /// <returns>Collection of all registered StatusId strings.</returns>
    public static IReadOnlyCollection<string> GetAllStatusIds()
    {
        Initialize();
        return _statusIdToTypeMap.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Get all registered notification types.
    /// </summary>
    /// <returns>Collection of all registered notification types.</returns>
    public static IReadOnlyCollection<Type> GetAllNotificationTypes()
    {
        Initialize();
        return _typeToStatusIdMap.Keys.ToList().AsReadOnly();
    }
}
