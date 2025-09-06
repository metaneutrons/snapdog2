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
using SnapDog2.Infrastructure.Integrations.Knx;
using SnapDog2.Server.Shared.Handlers;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Enums;

/// <summary>
/// Dependency injection configuration for KNX service.
/// </summary>
public static partial class KnxServiceConfiguration
{
    /// <summary>
    /// Adds KNX service to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The SnapDog configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKnxService(this IServiceCollection services, SnapDogConfiguration configuration)
    {
        var knxConfig = configuration.Services.Knx;
        var logger = services
            .BuildServiceProvider()
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("KnxServiceConfiguration");

        // Check if KNX service is enabled
        if (!knxConfig.Enabled)
        {
            logger.LogInformation("KnxServiceDisabled");
            return services;
        }

        // Validate configuration
        var validationResult = ValidateKnxConfiguration(knxConfig, configuration.Zones, configuration.Clients);
        if (!validationResult.IsValid)
        {
            logger.LogInformation("KnxConfigurationValidationFailed: {Details}", string.Join(", ", validationResult.Errors));
        }

        var connectionTypeText = knxConfig.ConnectionType switch
        {
            KnxConnectionType.Tunnel => "IP Tunneling",
            KnxConnectionType.Router => "IP Routing",
            KnxConnectionType.Usb => "USB",
            _ => "Unknown",
        };

        logger.LogInformation("KnxServiceRegistering: {Details}", connectionTypeText);

        if (!string.IsNullOrEmpty(knxConfig.Gateway))
        {
            var ipConnectionTypeText = knxConfig.ConnectionType switch
            {
                KnxConnectionType.Tunnel => "IP Tunneling",
                KnxConnectionType.Router => "IP Routing",
                _ => "IP Connection",
            };
            logger.LogInformation("KnxConnectionDetails: {Details}", ipConnectionTypeText, knxConfig.Gateway, knxConfig.Port);
        }

        // Count configured KNX zones and clients
        var knxZoneCount = configuration.Zones.Count(z => z.Knx.Enabled);
        var knxClientCount = configuration.Clients.Count(c => c.Knx.Enabled);

        logger.LogInformation("KnxIntegrationConfigured: {Details}", knxZoneCount, knxClientCount);

        // Register the actual KNX service
        services.AddSingleton<IKnxService, KnxService>();

        // Register KNX integration handler as singleton to match KnxService lifecycle
        services.AddSingleton<KnxIntegrationHandler>();

        return services;
    }

    private static ValidationResult ValidateKnxConfiguration(
        KnxConfig knxConfig,
        List<ZoneConfig> zones,
        List<ClientConfig> clients
    )
    {
        var errors = new List<string>();

        // Validate connection configuration
        if (string.IsNullOrEmpty(knxConfig.Gateway))
        {
            // USB connection - no additional validation needed
            // The service will check for available USB devices at runtime
        }
        else
        {
            // IP connection validation
            if (knxConfig.Port <= 0 || knxConfig.Port > 65535)
            {
                errors.Add($"Invalid KNX port: {knxConfig.Port}. Must be between 1 and 65535.");
            }
        }

        // Validate timeout
        if (knxConfig.Timeout <= 0)
        {
            errors.Add($"Invalid KNX timeout: {knxConfig.Timeout}. Must be greater than 0.");
        }

        // Validate zone KNX configurations
        for (var i = 0; i < zones.Count; i++)
        {
            var zone = zones[i];
            if (zone.Knx.Enabled)
            {
                ValidateGroupAddresses(zone.Knx, $"Zone {i + 1}", errors);
            }
        }

        // Validate client KNX configurations
        for (var i = 0; i < clients.Count; i++)
        {
            var client = clients[i];
            if (client.Knx.Enabled)
            {
                ValidateGroupAddresses(client.Knx, $"Client {i + 1}", errors);
            }
        }

        return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
    }

    private static void ValidateGroupAddresses(ZoneKnxConfig knxConfig, string context, List<string> errors)
    {
        ValidateGroupAddress(knxConfig.Volume, $"{context} Volume", errors);
        ValidateGroupAddress(knxConfig.VolumeStatus, $"{context} VolumeStatus", errors);
        ValidateGroupAddress(knxConfig.Mute, $"{context} Mute", errors);
        ValidateGroupAddress(knxConfig.MuteStatus, $"{context} MuteStatus", errors);
        ValidateGroupAddress(knxConfig.Play, $"{context} Play", errors);
        ValidateGroupAddress(knxConfig.Pause, $"{context} Pause", errors);
        ValidateGroupAddress(knxConfig.Stop, $"{context} Stop", errors);
        ValidateGroupAddress(knxConfig.TrackNext, $"{context} TrackNext", errors);
        ValidateGroupAddress(knxConfig.TrackPrevious, $"{context} TrackPrevious", errors);

        // Validate new track metadata and playback status addresses
        ValidateGroupAddress(knxConfig.TrackTitleStatus, $"{context} TrackTitleStatus", errors);
        ValidateGroupAddress(knxConfig.TrackArtistStatus, $"{context} TrackArtistStatus", errors);
        ValidateGroupAddress(knxConfig.TrackAlbumStatus, $"{context} TrackAlbumStatus", errors);
        ValidateGroupAddress(knxConfig.TrackProgressStatus, $"{context} TrackProgressStatus", errors);
        ValidateGroupAddress(knxConfig.TrackPlayingStatus, $"{context} TrackPlayingStatus", errors);
    }

    private static void ValidateGroupAddresses(ClientKnxConfig knxConfig, string context, List<string> errors)
    {
        ValidateGroupAddress(knxConfig.Volume, $"{context} Volume", errors);
        ValidateGroupAddress(knxConfig.VolumeStatus, $"{context} VolumeStatus", errors);
        ValidateGroupAddress(knxConfig.Mute, $"{context} Mute", errors);
        ValidateGroupAddress(knxConfig.MuteStatus, $"{context} MuteStatus", errors);
        ValidateGroupAddress(knxConfig.ConnectedStatus, $"{context} ConnectedStatus", errors);
    }

    private static void ValidateGroupAddress(string? groupAddress, string context, List<string> errors)
    {
        if (string.IsNullOrEmpty(groupAddress))
        {
            return; // Optional addresses are allowed to be empty
        }

        // Basic KNX group address format validation (x/y/z where x,y,z are numbers)
        var parts = groupAddress.Split('/');
        if (parts.Length != 3)
        {
            errors.Add($"Invalid group address format for {context}: '{groupAddress}'. Expected format: 'x/y/z'");
            return;
        }

        for (var i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out var value) || value < 0)
            {
                errors.Add(
                    $"Invalid group address part {i + 1} for {context}: '{parts[i]}'. Must be a non-negative integer."
                );
                return;
            }

            // KNX group address range validation
            var maxValue = i switch
            {
                0 => 31, // Main group: 0-31
                1 => 7, // Middle group: 0-7
                2 => 255, // Sub group: 0-255
                _ => 0,
            };

            if (value > maxValue)
            {
                errors.Add(
                    $"Group address part {i + 1} out of range for {context}: {value}. Maximum allowed: {maxValue}"
                );
                return;
            }
        }
    }

    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
