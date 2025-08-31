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
namespace SnapDog2.Shared.Models;

/// <summary>
/// Represents software version information.
/// </summary>
public record VersionDetails
{
    /// <summary>
    /// Gets the application version.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets the major version number.
    /// </summary>
    public int Major { get; init; }

    /// <summary>
    /// Gets the minor version number.
    /// </summary>
    public int Minor { get; init; }

    /// <summary>
    /// Gets the patch version number.
    /// </summary>
    public int Patch { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the version info was generated.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the UTC build date.
    /// </summary>
    public DateTime? BuildDateUtc { get; init; }

    /// <summary>
    /// Gets the Git commit hash.
    /// </summary>
    public string? GitCommit { get; init; }

    /// <summary>
    /// Gets the Git branch name.
    /// </summary>
    public string? GitBranch { get; init; }

    /// <summary>
    /// Gets the build configuration (Debug, Release).
    /// </summary>
    public string? BuildConfiguration { get; init; }
}
