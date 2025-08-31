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

/// <summary>
/// High-performance LoggerMessage definitions for SharedLoggingCommandBehavior.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class SharedLoggingCommandBehavior<TCommand, TResponse>
{
    // Command Lifecycle Operations (9701-9703)
    [LoggerMessage(
        EventId = 4600,
        Level = LogLevel.Information,
        Message = "Starting Command {CommandName}"
    )]
    private partial void LogStartingCommand(string commandName);

    [LoggerMessage(
        EventId = 4601,
        Level = LogLevel.Information,
        Message = "Completed Command {CommandName} in {ElapsedMilliseconds}ms"
    )]
    private partial void LogCompletedCommand(string commandName, long elapsedMilliseconds);

    [LoggerMessage(
        EventId = 4602,
        Level = LogLevel.Error,
        Message = "Command {CommandName} failed after {ElapsedMilliseconds}ms"
    )]
    private partial void LogCommandFailed(Exception ex, string commandName, long elapsedMilliseconds);
}

/// <summary>
/// High-performance LoggerMessage definitions for SharedLoggingQueryBehavior.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class SharedLoggingQueryBehavior<TQuery, TResponse>
{
    // Query Lifecycle Operations (9704-9706)
    [LoggerMessage(
        EventId = 4603,
        Level = LogLevel.Information,
        Message = "Starting Query {QueryName}"
    )]
    private partial void LogStartingQuery(string queryName);

    [LoggerMessage(
        EventId = 4604,
        Level = LogLevel.Information,
        Message = "Completed Query {QueryName} in {ElapsedMilliseconds}ms"
    )]
    private partial void LogCompletedQuery(string queryName, long elapsedMilliseconds);

    [LoggerMessage(
        EventId = 4605,
        Level = LogLevel.Error,
        Message = "Query {QueryName} failed after {ElapsedMilliseconds}ms"
    )]
    private partial void LogQueryFailed(Exception ex, string queryName, long elapsedMilliseconds);
}
