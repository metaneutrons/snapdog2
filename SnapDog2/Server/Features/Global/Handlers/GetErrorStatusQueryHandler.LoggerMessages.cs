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

namespace SnapDog2.Server.Features.Global.Handlers;

/// <summary>
/// High-performance LoggerMessage definitions for GetErrorStatusQueryHandler.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class GetErrorStatusQueryHandler
{
    // Error Status Query Operations (10201-10203)
    [LoggerMessage(
        EventId = 4700,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Getting latest system error status"
    )]
    private partial void LogGettingLatestSystemErrorStatus();

    [LoggerMessage(
        EventId = 4701,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Successfully retrieved error status: {HasError}"
    )]
    private partial void LogSuccessfullyRetrievedErrorStatus(bool hasError);

    [LoggerMessage(
        EventId = 4702,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Failed to get error status"
    )]
    private partial void LogFailedToGetErrorStatus(Exception ex);
}
