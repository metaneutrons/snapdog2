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

using Cortex.Mediator.Queries;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SnapDog2.Shared.Models;

/// <summary>
/// Pipeline behavior that validates queries using FluentValidation before processing.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ValidationQueryBehavior{TQuery, TResponse}"/> class.
/// </remarks>
/// <param name="validators">The validators for the query type.</param>
/// <param name="logger">The logger instance.</param>
public partial class ValidationQueryBehavior<TQuery, TResponse>(
    IEnumerable<IValidator<TQuery>> validators,
    ILogger<ValidationQueryBehavior<TQuery, TResponse>> logger
) : IQueryPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
    where TResponse : IResult
{
    private readonly IEnumerable<IValidator<TQuery>> _validators = validators;
    private readonly ILogger<ValidationQueryBehavior<TQuery, TResponse>> _logger = logger;

    /// <inheritdoc/>
    public async Task<TResponse> Handle(
        TQuery query,
        QueryHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var queryName = typeof(TQuery).Name;

        if (this._validators.Any())
        {
            this.LogValidatingQuery(queryName);

            var context = new ValidationContext<TQuery>(query);
            var validationResults = await Task.WhenAll(
                    this._validators.Select(v => v.ValidateAsync(context, cancellationToken))
                )
                .ConfigureAwait(false);

            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

            if (failures.Count != 0)
            {
                this.LogValidationFailed(queryName, failures.Count);
                throw new ValidationException(failures);
            }
        }

        return await next().ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 1200,
        Level = LogLevel.Debug,
        Message = "Validating query {QueryName}"
    )]
    private partial void LogValidatingQuery(string queryName);

    [LoggerMessage(
        EventId = 1201,
        Level = LogLevel.Warning,
        Message = "Validation failed for query {QueryName} with {FailureCount} errors"
    )]
    private partial void LogValidationFailed(string queryName, int failureCount);
}
