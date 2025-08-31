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
using Microsoft.Extensions.Options;
using SnapDog2.Shared.Configuration;

namespace SnapDog2.Application.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to simplify configuration registration.
/// </summary>
public static class ServiceCollectionConfigurationExtensions
{
    /// <summary>
    /// Registers a configuration object directly as IOptions&lt;T&gt; without manual property copying.
    /// </summary>
    /// <typeparam name="T">The configuration type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationInstance">The configuration instance to register.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureFromInstance<T>(this IServiceCollection services, T configurationInstance)
        where T : class
    {
        return services.Configure<T>(options =>
        {
            // Use reflection to copy all properties from the instance to the options
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                if (property.CanRead && property.CanWrite)
                {
                    var value = property.GetValue(configurationInstance);
                    property.SetValue(options, value);
                }
            }
        });
    }

    /// <summary>
    /// Registers a list configuration by clearing and adding all items from the source list.
    /// </summary>
    /// <typeparam name="T">The list item type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="sourceList">The source list to copy from.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureList<T>(this IServiceCollection services, IEnumerable<T> sourceList)
    {
        return services.Configure<List<T>>(options =>
        {
            options.Clear();
            options.AddRange(sourceList);
        });
    }

    /// <summary>
    /// Registers notification processing options with sensible defaults.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action to override defaults.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureNotificationProcessing(
        this IServiceCollection services,
        Action<NotificationProcessingOptions>? configure = null
    )
    {
        return services.Configure<NotificationProcessingOptions>(options =>
        {
            // Set sensible defaults
            options.MaxQueueCapacity = 2048;
            options.MaxConcurrency = 2;
            options.MaxRetryAttempts = 3;
            options.RetryBaseDelayMs = 250;
            options.RetryMaxDelayMs = 5000;
            options.ShutdownTimeoutSeconds = 10;

            // Allow overrides
            configure?.Invoke(options);
        });
    }

    /// <summary>
    /// Registers all SnapDog configuration sections using the elegant pattern.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="snapDogConfig">The main configuration object.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureSnapDog(
        this IServiceCollection services,
        SnapDogConfiguration snapDogConfig
    )
    {
        return services
            // Register the main configuration and its sections as singletons (direct instances)
            .AddSingleton(snapDogConfig)
            .AddSingleton(snapDogConfig.System)
            .AddSingleton(snapDogConfig.Telemetry)
            .AddSingleton(snapDogConfig.Http)
            .AddSingleton(snapDogConfig.Services)
            .AddSingleton(snapDogConfig.SnapcastServer)
            // Register for IOptions pattern using elegant methods
            .ConfigureFromInstance(snapDogConfig.Services.Audio)
            .ConfigureFromInstance(snapDogConfig)
            .ConfigureList(snapDogConfig.Zones)
            .ConfigureList(snapDogConfig.Clients)
            // Register IEnumerable<ZoneConfig> for MediaPlayerService
            .AddSingleton<IEnumerable<ZoneConfig>>(provider =>
            {
                var zoneOptions = provider.GetRequiredService<IOptions<List<ZoneConfig>>>();
                return zoneOptions.Value;
            });
    }
}
