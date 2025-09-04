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
namespace SnapDog2.Application.Extensions.DependencyInjection;

using Microsoft.Extensions.Options;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Infrastructure.Integrations.Snapcast;
using SnapDog2.Shared.Configuration;

/// <summary>
/// Extension methods for configuring Snapcast services.
/// </summary>
public static class SnapcastServiceConfiguration
{
    /// <summary>
    /// Adds Snapcast services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSnapcastServices(this IServiceCollection services)
    {
        // Register the state repository as singleton since it holds shared state
        services.AddSingleton<ISnapcastStateRepository, SnapcastStateRepository>();

        // Register our custom Snapcast service as singleton
        services.AddSingleton<ISnapcastService, CustomSnapcastService>();

        return services;
    }

    /// <summary>
    /// Validates Snapcast configuration.
    /// </summary>
    /// <param name="config">The Snapcast configuration to validate.</param>
    /// <returns>True if configuration is valid, false otherwise.</returns>
    public static bool ValidateSnapcastConfiguration(SnapcastConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Address))
        {
            return false;
        }

        if (config.JsonRpcPort <= 0 || config.JsonRpcPort > 65535)
        {
            return false;
        }

        if (config.Timeout <= 0)
        {
            return false;
        }

        if (config.ReconnectInterval <= 0)
        {
            return false;
        }

        return true;
    }
}
