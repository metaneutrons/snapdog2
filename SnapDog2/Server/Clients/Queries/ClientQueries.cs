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
namespace SnapDog2.Server.Clients.Queries;

using System.Collections.Generic;
using Cortex.Mediator.Queries;
using SnapDog2.Shared.Models;

/// <summary>
/// Query to retrieve the state of all known clients.
/// </summary>
public record GetAllClientsQuery : IQuery<Result<List<ClientState>>>;

/// <summary>
/// Query to retrieve the state of a specific client.
/// </summary>
public record GetClientQuery : IQuery<Result<ClientState>>
{
    /// <summary>
    /// Gets the index of the client to retrieve (1-based).
    /// </summary>
    public required int ClientIndex { get; init; }
}

/// <summary>
/// Query to retrieve clients assigned to a specific zone.
/// </summary>
public record GetClientsByZoneQuery : IQuery<Result<List<ClientState>>>
{
    /// <summary>
    /// Gets the index of the zone (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }
}
