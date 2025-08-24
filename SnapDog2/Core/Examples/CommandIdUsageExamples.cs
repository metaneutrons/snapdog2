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
namespace SnapDog2.Core.Examples;

using SnapDog2.Core.Attributes;
using SnapDog2.Core.Constants;
using SnapDog2.Core.Enums;
using SnapDog2.Server.Features.Zones.Commands.Playback;
using SnapDog2.Server.Features.Zones.Commands.Volume;

/// <summary>
/// Examples demonstrating the three approaches for using CommandId values:
/// 1. CommandIds Constants - Simple, compile-time safe constants
/// 2. CommandEventType Enum - Ultimate type safety with enum switching
/// 3. CommandIdRegistry - Dynamic runtime discovery and validation
/// </summary>
public static class CommandIdUsageExamples
{
    /// <summary>
    /// Example 1: Using CommandIds constants for simple, direct access.
    /// Best for straightforward command ID comparisons and mappings.
    /// </summary>
    public static void ConstantsApproach()
    {
        // Direct usage in comparisons
        var incomingCommand = "PLAY";
        if (incomingCommand == CommandIds.Play)
        {
            // Process play command
        }

        // Dictionary-based command mapping
        var commandMappings = new Dictionary<string, string>
        {
            [CommandIds.Play] = "start_playback",
            [CommandIds.Pause] = "pause_playback",
            [CommandIds.Stop] = "stop_playback",
            [CommandIds.VolumeUp] = "increase_volume",
            [CommandIds.VolumeDown] = "decrease_volume",
        };

        // Use in method parameters
        ProcessCommand(CommandIds.Play, new { ZoneIndex = 1 });
    }

    /// <summary>
    /// Example 2: Using CommandEventType enum for ultimate type safety.
    /// Best for complex command processing logic with compile-time validation.
    /// </summary>
    public static void EnumApproach()
    {
        // Type-safe parsing from external systems
        var incomingCommand = "VOLUME_UP";
        var commandType = CommandEventTypeExtensions.FromCommandString(incomingCommand);

        if (commandType.HasValue)
        {
            var result = commandType.Value switch
            {
                CommandEventType.Play => ProcessPlaybackCommand("play"),
                CommandEventType.Pause => ProcessPlaybackCommand("pause"),
                CommandEventType.Stop => ProcessPlaybackCommand("stop"),
                CommandEventType.VolumeUp => ProcessVolumeCommand("up"),
                CommandEventType.VolumeDown => ProcessVolumeCommand("down"),
                CommandEventType.Mute => ProcessVolumeCommand("mute"),
                CommandEventType.MuteToggle => ProcessVolumeCommand("mute_toggle"),
                _ => ProcessUnknownCommand(),
            };
        }

        // Convert back to string for external systems
        var commandString = CommandEventType.Play.ToCommandString(); // Returns "PLAY"
    }

    /// <summary>
    /// Example 3: Using CommandIdRegistry for dynamic scenarios.
    /// Best for runtime command discovery, validation, and reflection-based operations.
    /// </summary>
    public static void RegistryApproach()
    {
        // Runtime command type discovery
        var commandType = CommandIdRegistry.GetCommandType("PLAY");
        if (commandType != null)
        {
            // Create command instance dynamically
            var command = Activator.CreateInstance(commandType);
            // Process command...
        }

        // Validation of incoming commands
        var incomingCommandId = "VOLUME_UP";
        if (CommandIdRegistry.IsRegistered(incomingCommandId))
        {
            ProcessRegisteredCommand(incomingCommandId);
        }

        // Get all available commands for documentation or UI
        var allCommands = CommandIdRegistry.GetAllCommandIds();
        foreach (var commandId in allCommands)
        {
            Console.WriteLine($"Available command: {commandId}");
        }

        // Reverse lookup: Get CommandId from type
        var playCommandId = CommandIdRegistry.GetCommandId<PlayCommand>();
        Console.WriteLine($"Play command ID: {playCommandId}"); // Outputs: PLAY
    }

    /// <summary>
    /// Example 4: MQTT command processing with enum-based approach.
    /// Demonstrates real-world usage in service integration.
    /// </summary>
    public static string? ProcessMqttCommand(string commandString, object payload)
    {
        // Parse command string to enum for type safety
        var commandType = CommandEventTypeExtensions.FromCommandString(commandString);
        if (commandType == null)
        {
            return null; // Unknown command
        }

        // Type-safe command processing
        return commandType.Value switch
        {
            // Playback commands
            CommandEventType.Play => "snapdog/zone/1/control/play",
            CommandEventType.Pause => "snapdog/zone/1/control/pause",
            CommandEventType.Stop => "snapdog/zone/1/control/stop",

            // Volume commands
            CommandEventType.Volume => "snapdog/zone/1/volume/set",
            CommandEventType.VolumeUp => "snapdog/zone/1/volume/up",
            CommandEventType.VolumeDown => "snapdog/zone/1/volume/down",
            CommandEventType.Mute => "snapdog/zone/1/mute/set",
            CommandEventType.MuteToggle => "snapdog/zone/1/mute/toggle",

            // Client commands
            CommandEventType.ClientVolume => "snapdog/client/1/volume/set",
            CommandEventType.ClientMute => "snapdog/client/1/mute/set",
            CommandEventType.ClientMuteToggle => "snapdog/client/1/mute/toggle",

            _ => null, // Unmapped command
        };
    }

    // Helper methods for examples
    private static string ProcessPlaybackCommand(string action) => $"Processed playback: {action}";

    private static string ProcessVolumeCommand(string action) => $"Processed volume: {action}";

    private static string ProcessUnknownCommand() => "Unknown command processed";

    private static void ProcessCommand(string commandId, object parameters) { }

    private static void ProcessRegisteredCommand(string commandId) { }
}

/// <summary>
/// Benefits of the DRY CommandId System:
///
/// 1. **Compile-time Safety**: All command references are validated at build time
/// 2. **IntelliSense Support**: Full autocomplete and refactoring support
/// 3. **Single Source of Truth**: CommandIdAttribute on command classes is authoritative
/// 4. **Performance**: Enum switches are compiler-optimized, constants have zero overhead
/// 5. **Flexibility**: Three approaches for different use cases and complexity levels
/// 6. **Maintainability**: Adding new commands automatically updates all approaches
/// 7. **Type Safety**: Eliminates string typos and runtime command ID errors
/// 8. **Documentation**: Clear mapping between code and external system specifications
///
/// Architecture Decision Records:
///
/// **Why Three Approaches?**
/// - Constants: Simple, fast, IntelliSense-friendly for direct usage
/// - Enum: Type-safe switching, compiler optimizations, complex logic
/// - Registry: Dynamic scenarios, reflection-based operations, runtime discovery
///
/// **Performance Considerations:**
/// - Registry initialization is lazy and cached for optimal startup performance
/// - Enum switches are compiler-optimized with jump tables
/// - Constants provide zero-overhead access to command IDs
/// - All approaches maintain thread safety for concurrent access
///
/// **Maintenance Strategy:**
/// - CommandIdAttribute remains the single source of truth
/// - Constants and enum values are derived, not duplicated
/// - Registry provides runtime validation and discovery capabilities
/// - All approaches work together seamlessly in the same codebase
/// </summary>
internal static class CommandIdSystemDocumentation { }
