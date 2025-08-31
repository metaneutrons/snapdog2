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

using SnapDog2.Infrastructure.Integrations.Mqtt;

namespace SnapDog2.Application.Extensions.DependencyInjection;

/// <summary>
/// Service collection extensions for smart MQTT publishing configuration.
/// </summary>
public static class SmartMqttServiceConfiguration
{
    /// <summary>
    /// Adds smart MQTT publishing services with hybrid direct/queue approach.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSmartMqttPublishing(this IServiceCollection services)
    {
        // Register the smart MQTT publisher
        services.AddSingleton<ISmartMqttPublisher, SmartMqttPublisher>();

        // Register the unified notification handlers
        services.AddScoped<SnapDog2.Server.Shared.Handlers.IntegrationPublishingHandlers>();

        return services;
    }
}
