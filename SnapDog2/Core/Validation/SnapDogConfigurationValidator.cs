using FluentValidation;
using SnapDog2.Core.Configuration;

namespace SnapDog2.Core.Validation;

/// <summary>
/// FluentValidation validator for SnapDogConfiguration.
/// Validates the main configuration structure and cross-dependencies between subsystems.
/// </summary>
public sealed class SnapDogConfigurationValidator : AbstractValidator<SnapDogConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnapDogConfigurationValidator"/> class.
    /// </summary>
    public SnapDogConfigurationValidator()
    {
        // System configuration validation
        RuleFor(x => x.System)
            .NotNull()
            .WithMessage("System configuration is required.")
            .SetValidator(new SystemConfigurationValidator());

        // API configuration validation
        RuleFor(x => x.Api)
            .NotNull()
            .WithMessage("API configuration is required.")
            .SetValidator(new ApiConfigurationValidator());

        // Telemetry configuration validation
        RuleFor(x => x.Telemetry)
            .NotNull()
            .WithMessage("Telemetry configuration is required.")
            .SetValidator(new TelemetryConfigurationValidator());

        // Services configuration validation
        RuleFor(x => x.Services)
            .NotNull()
            .WithMessage("Services configuration is required.")
            .SetValidator(new ServicesConfigurationValidator());

        // Zones validation
        RuleFor(x => x.Zones)
            .NotNull()
            .WithMessage("Zones collection cannot be null.")
            .Must(HaveUniqueZoneIds)
            .WithMessage("Zone IDs must be unique across all zones.")
            .Must(HaveAtLeastOneZone)
            .WithMessage("At least one zone must be configured.");

        RuleForEach(x => x.Zones).SetValidator(new ZoneConfigurationValidator());

        // Clients validation
        RuleFor(x => x.Clients)
            .NotNull()
            .WithMessage("Clients collection cannot be null.")
            .Must(HaveUniqueClientNames)
            .WithMessage("Client names must be unique across all clients.")
            .Must(HaveUniqueMacAddresses)
            .WithMessage("MAC addresses must be unique across all clients.");

        RuleForEach(x => x.Clients).SetValidator(new ClientConfigurationValidator());

        // Radio stations validation
        RuleFor(x => x.RadioStations)
            .NotNull()
            .WithMessage("Radio stations collection cannot be null.")
            .Must(HaveUniqueRadioStationNames)
            .WithMessage("Radio station names must be unique across all stations.")
            .Must(HaveUniqueRadioStationUrls)
            .WithMessage("Radio station URLs should be unique to avoid conflicts.");

        RuleForEach(x => x.RadioStations).SetValidator(new RadioStationConfigurationValidator());

        // Cross-configuration business rules
        RuleFor(x => x)
            .Must(HaveValidClientZoneAssignments)
            .WithMessage("All client zone assignments must reference existing zones.")
            .Must(HaveReasonablePortConfiguration)
            .WithMessage("Port configuration has conflicts or unreasonable values.")
            .Must(HaveConsistentEnvironmentSettings)
            .WithMessage("Environment settings are inconsistent across configurations.");
    }

    /// <summary>
    /// Validates that all zone IDs are unique.
    /// </summary>
    /// <param name="zones">The zones to validate.</param>
    /// <returns>True if all zone IDs are unique; otherwise, false.</returns>
    private static bool HaveUniqueZoneIds(List<ZoneConfiguration> zones)
    {
        if (zones == null)
        {
            return true;
        }

        var zoneIds = zones.Select(z => z.Id).ToList();
        return zoneIds.Count == zoneIds.Distinct().Count();
    }

    /// <summary>
    /// Validates that at least one zone is configured.
    /// </summary>
    /// <param name="zones">The zones to validate.</param>
    /// <returns>True if at least one zone exists; otherwise, false.</returns>
    private static bool HaveAtLeastOneZone(List<ZoneConfiguration> zones)
    {
        return zones?.Count > 0;
    }

    /// <summary>
    /// Validates that all client names are unique.
    /// </summary>
    /// <param name="clients">The clients to validate.</param>
    /// <returns>True if all client names are unique; otherwise, false.</returns>
    private static bool HaveUniqueClientNames(List<ClientConfiguration> clients)
    {
        if (clients == null)
        {
            return true;
        }

        var clientNames = clients.Where(c => !string.IsNullOrEmpty(c.Name)).Select(c => c.Name).ToList();
        return clientNames.Count == clientNames.Distinct().Count();
    }

    /// <summary>
    /// Validates that all MAC addresses are unique.
    /// </summary>
    /// <param name="clients">The clients to validate.</param>
    /// <returns>True if all MAC addresses are unique; otherwise, false.</returns>
    private static bool HaveUniqueMacAddresses(List<ClientConfiguration> clients)
    {
        if (clients == null)
        {
            return true;
        }

        var macAddresses = clients
            .Where(c => !string.IsNullOrEmpty(c.Mac))
            .Select(c => c.Mac.ToUpperInvariant())
            .ToList();
        return macAddresses.Count == macAddresses.Distinct().Count();
    }

    /// <summary>
    /// Validates that all radio station names are unique.
    /// </summary>
    /// <param name="radioStations">The radio stations to validate.</param>
    /// <returns>True if all radio station names are unique; otherwise, false.</returns>
    private static bool HaveUniqueRadioStationNames(List<RadioStationConfiguration> radioStations)
    {
        if (radioStations == null)
        {
            return true;
        }

        var stationNames = radioStations.Where(r => !string.IsNullOrEmpty(r.Name)).Select(r => r.Name).ToList();
        return stationNames.Count == stationNames.Distinct().Count();
    }

    /// <summary>
    /// Validates that all radio station URLs are unique.
    /// </summary>
    /// <param name="radioStations">The radio stations to validate.</param>
    /// <returns>True if all radio station URLs are unique; otherwise, false.</returns>
    private static bool HaveUniqueRadioStationUrls(List<RadioStationConfiguration> radioStations)
    {
        if (radioStations == null)
        {
            return true;
        }

        var urls = radioStations
            .Where(r => !string.IsNullOrEmpty(r.Url))
            .Select(r => r.Url.ToLowerInvariant())
            .ToList();
        return urls.Count == urls.Distinct().Count();
    }

    /// <summary>
    /// Validates that client zone assignments reference existing zones.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <returns>True if all zone assignments are valid; otherwise, false.</returns>
    private static bool HaveValidClientZoneAssignments(SnapDogConfiguration configuration)
    {
        if (configuration.Clients == null || configuration.Zones == null)
        {
            return true;
        }

        var validZoneIds = configuration.Zones.Select(z => z.Id).ToHashSet();

        foreach (var client in configuration.Clients)
        {
            if (!validZoneIds.Contains(client.DefaultZone))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Validates that port configuration is reasonable and doesn't have conflicts.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <returns>True if port configuration is reasonable; otherwise, false.</returns>
    private static bool HaveReasonablePortConfiguration(SnapDogConfiguration configuration)
    {
        if (configuration.Api == null)
        {
            return true;
        }

        var api = configuration.Api;

        // Check that HTTP and HTTPS ports are different
        if (api.HttpsEnabled && api.Port == api.HttpsPort)
        {
            return false;
        }

        // Check for reasonable port ranges
        if (api.Port < 1024 || api.Port > 65535)
        {
            return false; // Avoid well-known ports and invalid ranges
        }

        if (api.HttpsEnabled && (api.HttpsPort < 1024 || api.HttpsPort > 65535))
        {
            return false;
        }

        // Check that ports are not commonly reserved
        var reservedPorts = new[] { 22, 23, 25, 53, 80, 110, 143, 443, 993, 995 };
        if (reservedPorts.Contains(api.Port) || (api.HttpsEnabled && reservedPorts.Contains(api.HttpsPort)))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that environment settings are consistent across configurations.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <returns>True if environment settings are consistent; otherwise, false.</returns>
    private static bool HaveConsistentEnvironmentSettings(SnapDogConfiguration configuration)
    {
        if (configuration.System == null)
        {
            return true;
        }

        var systemEnvironment = configuration.System.Environment?.ToLowerInvariant();

        // In production, certain settings should be more restrictive
        if (systemEnvironment == "production")
        {
            // API should have authentication enabled in production
            if (configuration.Api != null && !configuration.Api.AuthEnabled)
            {
                return false;
            }

            // Debug should be disabled in production
            if (configuration.System.DebugEnabled)
            {
                return false;
            }

            // HTTPS should be enabled in production
            if (configuration.Api != null && !configuration.Api.HttpsEnabled)
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Placeholder validators for configuration classes that don't exist yet.
/// These should be replaced with actual validators when the configuration classes are implemented.
/// </summary>
public sealed class SystemConfigurationValidator : AbstractValidator<SystemConfiguration>
{
    public SystemConfigurationValidator()
    {
        RuleFor(x => x.Environment)
            .NotEmpty()
            .WithMessage("Environment is required.")
            .Must(BeValidEnvironment)
            .WithMessage("Environment must be Development, Staging, or Production.");

        RuleFor(x => x.LogLevel)
            .NotEmpty()
            .WithMessage("Log level is required.")
            .Must(BeValidLogLevel)
            .WithMessage("Log level must be a valid Serilog level.");

        RuleFor(x => x.ApplicationName)
            .NotEmpty()
            .WithMessage("Application name is required.")
            .MaximumLength(100)
            .WithMessage("Application name cannot exceed 100 characters.");

        RuleFor(x => x.Version)
            .NotEmpty()
            .WithMessage("Version is required.")
            .Matches(@"^\d+\.\d+\.\d+")
            .WithMessage("Version must follow semantic versioning (x.y.z).");
    }

    private static bool BeValidEnvironment(string environment)
    {
        var validEnvironments = new[] { "Development", "Staging", "Production" };
        return validEnvironments.Contains(environment);
    }

    private static bool BeValidLogLevel(string logLevel)
    {
        var validLevels = new[] { "Verbose", "Debug", "Information", "Warning", "Error", "Fatal" };
        return validLevels.Contains(logLevel);
    }
}

public sealed class ApiConfigurationValidator : AbstractValidator<ApiConfiguration>
{
    public ApiConfigurationValidator()
    {
        RuleFor(x => x.Port).InclusiveBetween(1024, 65535).WithMessage("Port must be between 1024 and 65535.");

        RuleFor(x => x.HttpsPort)
            .InclusiveBetween(1024, 65535)
            .WithMessage("HTTPS port must be between 1024 and 65535.");
    }
}

public sealed class TelemetryConfigurationValidator : AbstractValidator<TelemetryConfiguration> { }

public sealed class ServicesConfigurationValidator : AbstractValidator<ServicesConfiguration> { }

public sealed class ZoneConfigurationValidator : AbstractValidator<ZoneConfiguration> { }

public sealed class ClientConfigurationValidator : AbstractValidator<ClientConfiguration> { }

public sealed class RadioStationConfigurationValidator : AbstractValidator<RadioStationConfiguration> { }
