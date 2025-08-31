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
using System.Reflection;

namespace SnapDog2.Application.Extensions;

/// <summary>
/// Extension methods for Assembly to help with version information extraction
/// </summary>
public static class AssemblyExtensions
{
    /// <summary>
    /// Gets the build date from assembly metadata
    /// </summary>
    public static DateTime? GetBuildDate(this Assembly assembly)
    {
        try
        {
            var buildDateMetadata = assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attr => attr.Key == "BuildDate")
                ?.Value;

            if (!string.IsNullOrEmpty(buildDateMetadata) && DateTime.TryParse(buildDateMetadata, out var buildDate))
            {
                return buildDate;
            }

            // Fallback: try to get from file creation time
            if (!string.IsNullOrEmpty(assembly.Location) && File.Exists(assembly.Location))
            {
                return File.GetCreationTimeUtc(assembly.Location);
            }
        }
        catch
        {
            // Ignore errors
        }

        return null;
    }

    /// <summary>
    /// Gets the build machine from assembly metadata
    /// </summary>
    public static string? GetBuildMachine(this Assembly assembly)
    {
        try
        {
            return assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attr => attr.Key == "BuildMachine")
                ?.Value;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the build user from assembly metadata
    /// </summary>
    public static string? GetBuildUser(this Assembly assembly)
    {
        try
        {
            return assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attr => attr.Key == "BuildUser")
                ?.Value;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a safe display name for the assembly location
    /// </summary>
    public static string GetSafeLocation(this Assembly assembly)
    {
        try
        {
            if (assembly.IsDynamic)
            {
                return "Dynamic";
            }

            var location = assembly.Location;
            if (string.IsNullOrEmpty(location))
            {
                return "In Memory";
            }

            return Path.GetFileName(location);
        }
        catch
        {
            return "Unknown";
        }
    }
}
