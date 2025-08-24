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
namespace SnapDog2.Server.Features.Global.Notifications;

using Cortex.Mediator.Notifications;
using SnapDog2.Core.Attributes;

/// <summary>
/// Notification published when a command status changes (acknowledgments).
/// </summary>
[StatusId("COMMAND_STATUS")]
public record CommandStatusNotification : INotification
{
    /// <summary>
    /// Gets the command ID that this status relates to.
    /// </summary>
    public required string CommandId { get; init; }

    /// <summary>
    /// Gets the status of the command ("ok", "processing", "done").
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the zone index if this is a zone-specific command (1-based).
    /// </summary>
    public int? ZoneIndex { get; init; }

    /// <summary>
    /// Gets the client index if this is a client-specific command (1-based).
    /// </summary>
    public int? ClientIndex { get; init; }

    /// <summary>
    /// Gets additional context or metadata about the command execution.
    /// </summary>
    public string? Context { get; init; }
}

/// <summary>
/// Notification published when a command encounters an error.
/// </summary>
[StatusId("COMMAND_ERROR")]
public record CommandErrorNotification : INotification
{
    /// <summary>
    /// Gets the command ID that encountered the error.
    /// </summary>
    public required string CommandId { get; init; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Gets the error code (HTTP-style or custom error code).
    /// </summary>
    public required int ErrorCode { get; init; }

    /// <summary>
    /// Gets the zone index if this is a zone-specific command (1-based).
    /// </summary>
    public int? ZoneIndex { get; init; }

    /// <summary>
    /// Gets the client index if this is a client-specific command (1-based).
    /// </summary>
    public int? ClientIndex { get; init; }

    /// <summary>
    /// Gets additional error details or stack trace information.
    /// </summary>
    public string? ErrorDetails { get; init; }
}
