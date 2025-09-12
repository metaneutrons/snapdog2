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

using SnapDog2.Domain.Abstractions;
using SnapDog2.Infrastructure.Integrations.Mqtt;
using SnapDog2.Shared.Configuration;

/// <summary>
/// Dependency injection configuration for enterprise MQTT services.
/// </summary>
public static class MqttServiceConfiguration
{
    /// <summary>
    /// Adds MQTT services to the dependency injection container.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddMqttServices(this IServiceCollection services)
    {
        // Register MqttConfig from SnapDogConfiguration
        services.AddSingleton<MqttConfig>(provider =>
        {
            var config = provider.GetRequiredService<SnapDogConfiguration>();
            return config.Services.Mqtt;
        });

        // Register the MQTT service as singleton with proper DI lifetime management
        services.AddSingleton<IMqttService, MqttService>();

        return services;
    }

    /// <summary>
    /// Validates MQTT configuration and dependencies.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection ValidateMqttConfiguration(this IServiceCollection services)
    {
        // Add configuration validation
        services
            .AddOptions<SnapDogConfiguration>()
            .Validate(
                config =>
                {
                    if (!config.Services.Mqtt.Enabled)
                    {
                        return true; // Skip validation if MQTT is disabled
                    }

                    if (string.IsNullOrWhiteSpace(config.Services.Mqtt.BrokerAddress))
                    {
                        return false;
                    }

                    if (config.Services.Mqtt.Port <= 0 || config.Services.Mqtt.Port > 65535)
                    {
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(config.Services.Mqtt.ClientIndex))
                    {
                        return false;
                    }

                    return true;
                },
                "Invalid MQTT configuration"
            );

        return services;
    }
}
