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
using Microsoft.Extensions.Logging;

namespace SnapDog2.Infrastructure.Integrations.Snapcast;

/// <summary>
/// High-performance LoggerMessage definitions for SnapcastService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class SnapcastService
{
    // Service Disposal Operations (10801)
    [LoggerMessage(10801, LogLevel.Error, "Error during SnapcastService disposal")]
    private partial void LogErrorDuringSnapcastServiceDisposal(Exception ex);
}
