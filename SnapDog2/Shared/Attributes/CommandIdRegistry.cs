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
/// Registry for mapping CommandId strings to command types and vice versa.
/// Automatically discovers all CommandId attributes at runtime through reflection.
/// Thread-safe implementation with lazy initialization.
/// </summary>
public static class CommandIdRegistry
{
    private static readonly ConcurrentDictionary<string, Type> _commandIdToTypeMap = new();
    private static readonly ConcurrentDictionary<Type, string> _typeToCommandIdMap = new();
    private static readonly object _initializationLock = new();
    private static bool _isInitialized;

    /// <summary>
    /// Initializes the registry by scanning all loaded assemblies for CommandId attributes.
    /// This method is thread-safe and will only perform initialization once.
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        lock (_initializationLock)
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
                    var typesWithCommandId = assembly
                        .GetTypes()
                        .Where(type => type.GetCustomAttribute<CommandIdAttribute>() != null);

                    foreach (var type in typesWithCommandId)
                    {
                        var attribute = type.GetCustomAttribute<CommandIdAttribute>()!;
                        _commandIdToTypeMap.TryAdd(attribute.Id, type);
                        _typeToCommandIdMap.TryAdd(type, attribute.Id);
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
    /// Gets the CommandId for a command type.
    /// Initializes the registry if not already initialized.
    /// </summary>
    /// <typeparam name="T">The command type.</typeparam>
    /// <returns>The CommandId string, or null if not found.</returns>
    public static string? GetCommandId<T>()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        return _typeToCommandIdMap.TryGetValue(typeof(T), out var commandId) ? commandId : null;
    }

    /// <summary>
    /// Gets the command type for a CommandId string.
    /// Initializes the registry if not already initialized.
    /// </summary>
    /// <param name="commandId">The CommandId string.</param>
    /// <returns>The command type, or null if not found.</returns>
    public static Type? GetCommandType(string commandId)
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        return _commandIdToTypeMap.TryGetValue(commandId, out var type) ? type : null;
    }

    /// <summary>
    /// Checks if a CommandId is registered in the system.
    /// Initializes the registry if not already initialized.
    /// </summary>
    /// <param name="commandId">The CommandId string to check.</param>
    /// <returns>True if the CommandId is registered, false otherwise.</returns>
    public static bool IsRegistered(string commandId)
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        return _commandIdToTypeMap.ContainsKey(commandId);
    }

    /// <summary>
    /// Gets all registered CommandId strings.
    /// Initializes the registry if not already initialized.
    /// </summary>
    /// <returns>A readonly collection of all CommandId strings.</returns>
    public static IReadOnlyCollection<string> GetAllCommandIds()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        return _commandIdToTypeMap.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets all registered command types.
    /// Initializes the registry if not already initialized.
    /// </summary>
    /// <returns>A readonly collection of all command types.</returns>
    public static IReadOnlyCollection<Type> GetAllCommandTypes()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        return _typeToCommandIdMap.Keys.ToList().AsReadOnly();
    }
}
