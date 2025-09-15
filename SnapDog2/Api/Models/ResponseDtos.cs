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
namespace SnapDog2.Api.Models;

using SnapDog2.Shared.Models;

/// <summary>
/// Paginated collection with metadata.
/// </summary>
public record Page<T>(T[] Items, int Total, int PageSize = 20, int PageNumber = 1)
{
    public int TotalPages => (int)Math.Ceiling((double)this.Total / this.PageSize);
    public bool HasNext => this.PageNumber < this.TotalPages;
    public bool HasPrevious => this.PageNumber > 1;
}

/// <summary>
/// Zone summary for listings.
/// </summary>
public record Zone(string Name, int Index, bool Active, string Status, string Icon = "");

/// <summary>
/// Client summary for listings.
/// </summary>
public record Client(int Id, string Name, bool Connected, int? Zone = null, string Icon = "");

/// <summary>
/// Playlist with tracks for detailed endpoints.
/// </summary>
public record PlaylistWithTracks(PlaylistInfo Info, List<TrackInfo> Tracks);
