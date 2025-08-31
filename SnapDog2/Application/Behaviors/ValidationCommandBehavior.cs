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

using Cortex.Mediator.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SnapDog2.Shared.Models;

/// <summary>
/// Pipeline behavior that validates commands using FluentValidation before processing.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ValidationCommandBehavior{TCommand, TResponse}"/> class.
/// </remarks>
/// <param name="validators">The validators for the command type.</param>
/// <param name="logger">The logger instance.</param>
public partial class ValidationCommandBehavior<TCommand, TResponse>(
    IEnumerable<IValidator<TCommand>> validators,
    ILogger<ValidationCommandBehavior<TCommand, TResponse>> logger
) : ICommandPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
    where TResponse : IResult
{
    private readonly IEnumerable<IValidator<TCommand>> _validators = validators;
    private readonly ILogger<ValidationCommandBehavior<TCommand, TResponse>> _logger = logger;

    /// <inheritdoc/>
    public async Task<TResponse> Handle(
        TCommand command,
        CommandHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var commandName = typeof(TCommand).Name;

        if (this._validators.Any())
        {
            this.LogValidatingCommand(commandName);

            var context = new ValidationContext<TCommand>(command);
            var validationResults = await Task.WhenAll(
                    this._validators.Select(v => v.ValidateAsync(context, cancellationToken))
                )
                .ConfigureAwait(false);

            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

            if (failures.Count != 0)
            {
                this.LogValidationFailed(commandName, failures.Count);
                throw new ValidationException(failures);
            }
        }

        return await next().ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 1100,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Validating command {CommandName}"
    )]
    private partial void LogValidatingCommand(string commandName);

    [LoggerMessage(
        EventId = 1101,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Validation failed for command {CommandName} with {FailureCount} errors"
    )]
    private partial void LogValidationFailed(string commandName, int failureCount);
}
