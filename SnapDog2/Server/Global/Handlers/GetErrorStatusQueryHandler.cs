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
namespace SnapDog2.Server.Global.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Global.Queries;
using SnapDog2.Shared.Models;

/// <summary>
/// Handler for getting the latest system error information.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GetErrorStatusQueryHandler"/> class.
/// </remarks>
/// <param name="logger">The logger instance.</param>
/// <param name="errorTrackingService">The error tracking service instance.</param>
public partial class GetErrorStatusQueryHandler(
    ILogger<GetErrorStatusQueryHandler> logger,
    IErrorTrackingService errorTrackingService
) : IQueryHandler<GetErrorStatusQuery, Result<ErrorDetails?>>
{
    private readonly ILogger<GetErrorStatusQueryHandler> _logger = logger;
    private readonly IErrorTrackingService _errorTrackingService = errorTrackingService;

    /// <summary>
    /// Handles the get error status query.
    /// </summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the error details result.</returns>
    public async Task<Result<ErrorDetails?>> Handle(GetErrorStatusQuery query, CancellationToken cancellationToken)
    {
        try
        {
            this.LogGettingLatestSystemErrorStatus();

            // Get the latest error from the error tracking service
            var errorDetails = await _errorTrackingService.GetLatestErrorAsync();

            this.LogSuccessfullyRetrievedErrorStatus(errorDetails != null);

            return Result<ErrorDetails?>.Success(errorDetails);
        }
        catch (Exception ex)
        {
            this.LogFailedToGetErrorStatus(ex);
            return Result<ErrorDetails?>.Failure("Failed to retrieve error status");
        }
    }
}
