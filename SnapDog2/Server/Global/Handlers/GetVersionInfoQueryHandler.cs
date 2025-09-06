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

using System.Reflection;
using Cortex.Mediator.Queries;
using SnapDog2.Server.Global.Queries;
using SnapDog2.Shared.Models;

/// <summary>
/// Handles the GetVersionInfoQuery.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GetVersionInfoQueryHandler"/> class.
/// </remarks>
/// <param name="logger">The logger instance.</param>
public partial class GetVersionInfoQueryHandler(ILogger<GetVersionInfoQueryHandler> logger)
    : IQueryHandler<GetVersionInfoQuery, Result<VersionDetails>>
{
    private readonly ILogger<GetVersionInfoQueryHandler> _logger = logger;

    /// <inheritdoc/>
    public async Task<Result<VersionDetails>> Handle(GetVersionInfoQuery request, CancellationToken cancellationToken)
    {
        this.LogHandling();

        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "Unknown";
            var buildDate = File.GetLastWriteTime(assembly.Location);

            var versionDetails = new VersionDetails
            {
                Version = version,
                BuildDateUtc = buildDate,
                BuildConfiguration = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            };

            return await Task.FromResult(Result<VersionDetails>.Success(versionDetails)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.LogError(ex);
            return Result<VersionDetails>.Failure(ex);
        }
    }

    [LoggerMessage(EventId = 112800, Level = LogLevel.Information, Message = "Handling GetVersionInfoQuery"
)]
    private partial void LogHandling();

    [LoggerMessage(EventId = 112801, Level = LogLevel.Error, Message = "Error retrieving version information"
)]
    private partial void LogError(Exception ex);
}
