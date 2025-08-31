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
namespace SnapDog2.Application.Behaviors;

using System.Diagnostics;
using Cortex.Mediator.Commands;
using Cortex.Mediator.Queries;
using SnapDog2.Shared.Models;

/// <summary>
/// Command pipeline behavior with shared logging implementation.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="SharedLoggingCommandBehavior{TCommand, TResponse}"/> class.
/// </remarks>
/// <param name="logger">The logger instance.</param>
public partial class SharedLoggingCommandBehavior<TCommand, TResponse>(
    ILogger<SharedLoggingCommandBehavior<TCommand, TResponse>> logger
) : ICommandPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
    where TResponse : IResult
{
    private readonly ILogger<SharedLoggingCommandBehavior<TCommand, TResponse>> _logger = logger;
    private static readonly ActivitySource ActivitySource = new("SnapDog2.CortexMediator");

    /// <inheritdoc/>
    public async Task<TResponse> Handle(
        TCommand command,
        CommandHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var commandName = typeof(TCommand).Name;
        using var activity = ActivitySource.StartActivity($"CortexMediator.Command.{commandName}");

        this.LogStartingCommand(commandName);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);
            stopwatch.Stop();

            this.LogCompletedCommand(commandName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            this.LogCommandFailed(ex, commandName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

/// <summary>
/// Query pipeline behavior with shared logging implementation.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="SharedLoggingQueryBehavior{TQuery, TResponse}"/> class.
/// </remarks>
/// <param name="logger">The logger instance.</param>
public partial class SharedLoggingQueryBehavior<TQuery, TResponse>(
    ILogger<SharedLoggingQueryBehavior<TQuery, TResponse>> logger
) : IQueryPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
    where TResponse : IResult
{
    private readonly ILogger<SharedLoggingQueryBehavior<TQuery, TResponse>> _logger = logger;
    private static readonly ActivitySource ActivitySource = new("SnapDog2.CortexMediator");

    /// <inheritdoc/>
    public async Task<TResponse> Handle(
        TQuery query,
        QueryHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var queryName = typeof(TQuery).Name;
        using var activity = ActivitySource.StartActivity($"CortexMediator.Query.{queryName}");

        this.LogStartingQuery(queryName);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);
            stopwatch.Stop();

            this.LogCompletedQuery(queryName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            this.LogQueryFailed(ex, queryName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
