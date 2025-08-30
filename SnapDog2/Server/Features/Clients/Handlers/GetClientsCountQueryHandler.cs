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
namespace SnapDog2.Server.Features.Clients.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Queries;

/// <summary>
/// Handles the GetClientsCountQuery.
/// </summary>
public class GetClientsCountQueryHandler(IOptions<SnapDogConfiguration> config)
    : IQueryHandler<GetClientsCountQuery, Result<int>>
{
    private readonly SnapDogConfiguration _config = config.Value;

    public Task<Result<int>> Handle(GetClientsCountQuery request, CancellationToken cancellationToken)
    {
        var count = _config.Clients.Count;
        return Task.FromResult(Result<int>.Success(count));
    }
}
