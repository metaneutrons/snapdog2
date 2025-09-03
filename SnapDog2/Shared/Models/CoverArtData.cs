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
/// Represents cover art image data.
/// </summary>
public record CoverArtData
{
    /// <summary>
    /// The binary image data.
    /// </summary>
    public required byte[] Data { get; init; }

    /// <summary>
    /// The MIME content type of the image.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Optional ETag for caching.
    /// </summary>
    public string? ETag { get; init; }
}
