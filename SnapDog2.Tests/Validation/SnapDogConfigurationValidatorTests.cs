using FluentValidation.TestHelper;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Validation;
using Xunit;

namespace SnapDog2.Tests.Validation;

/// <summary>
/// Unit tests for the SnapDogConfigurationValidator class.
/// Tests validation rules for the main configuration structure and cross-dependencies.
/// </summary>
public class SnapDogConfigurationValidatorTests
{
    private readonly SnapDogConfigurationValidator _validator;

    public SnapDogConfigurationValidatorTests()
    {
        _validator = new SnapDogConfigurationValidator();
    }

    [Fact]
    public void Validate_WithValidConfiguration_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var configuration = CreateValidConfiguration();

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNullSystem_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.System = null!;

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.System).WithErrorMessage("System configuration is required.");
    }

    [Fact]
    public void Validate_WithNullApi_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.Api = null!;

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Api).WithErrorMessage("API configuration is required.");
    }

    [Fact]
    public void Validate_WithNullTelemetry_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.Telemetry = null!;

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Telemetry).WithErrorMessage("Telemetry configuration is required.");
    }

    [Fact]
    public void Validate_WithNullServices_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.Services = null!;

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Services).WithErrorMessage("Services configuration is required.");
    }

    [Fact]
    public void Validate_WithNullZones_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.Zones = null!;

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Zones).WithErrorMessage("Zones collection cannot be null.");
    }

    [Fact]
    public void Validate_WithEmptyZones_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.Zones = new List<ZoneConfiguration>();

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Zones).WithErrorMessage("At least one zone must be configured.");
    }

    [Fact]
    public void Validate_WithDuplicateZoneIds_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.Zones = new List<ZoneConfiguration>
        {
            new() { Id = 1, Name = "Zone 1" },
            new() { Id = 1, Name = "Zone 2" }, // Duplicate ID
        };

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Zones)
            .WithErrorMessage("Zone IDs must be unique across all zones.");
    }

    [Fact]
    public void Validate_WithNullClients_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.Clients = null!;

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Clients).WithErrorMessage("Clients collection cannot be null.");
    }

    [Fact]
    public void Validate_WithDuplicateClientNames_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.Clients = new List<ClientConfiguration>
        {
            new()
            {
                Name = "Client 1",
                Mac = "00:11:22:33:44:55",
                DefaultZone = 1,
            },
            new()
            {
                Name = "Client 1",
                Mac = "00:11:22:33:44:66",
                DefaultZone = 1,
            }, // Duplicate name
        };

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Clients)
            .WithErrorMessage("Client names must be unique across all clients.");
    }

    [Fact]
    public void Validate_WithDuplicateMacAddresses_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.Clients = new List<ClientConfiguration>
        {
            new()
            {
                Name = "Client 1",
                Mac = "00:11:22:33:44:55",
                DefaultZone = 1,
            },
            new()
            {
                Name = "Client 2",
                Mac = "00:11:22:33:44:55",
                DefaultZone = 1,
            }, // Duplicate MAC
        };

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Clients)
            .WithErrorMessage("MAC addresses must be unique across all clients.");
    }

    [Fact]
    public void Validate_WithNullRadioStations_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.RadioStations = null!;

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.RadioStations)
            .WithErrorMessage("Radio stations collection cannot be null.");
    }

    [Fact]
    public void Validate_WithDuplicateRadioStationNames_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.RadioStations = new List<RadioStationConfiguration>
        {
            new() { Name = "Station 1", Url = "http://stream1.example.com" },
            new() { Name = "Station 1", Url = "http://stream2.example.com" }, // Duplicate name
        };

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.RadioStations)
            .WithErrorMessage("Radio station names must be unique across all stations.");
    }

    [Fact]
    public void Validate_WithDuplicateRadioStationUrls_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.RadioStations = new List<RadioStationConfiguration>
        {
            new() { Name = "Station 1", Url = "http://stream.example.com" },
            new() { Name = "Station 2", Url = "http://stream.example.com" }, // Duplicate URL
        };

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.RadioStations)
            .WithErrorMessage("Radio station URLs should be unique to avoid conflicts.");
    }

    [Fact]
    public void Validate_WithInvalidClientZoneAssignment_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.Clients = new List<ClientConfiguration>
        {
            new()
            {
                Name = "Client 1",
                Mac = "00:11:22:33:44:55",
                DefaultZone = 999,
            }, // Non-existent zone
        };

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("All client zone assignments must reference existing zones.");
    }

    [Fact]
    public void Validate_WithConflictingPorts_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.Api = new ApiConfiguration
        {
            Port = 8080,
            HttpsEnabled = true,
            HttpsPort = 8080, // Same as HTTP port
            AuthEnabled = true,
        };

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Port configuration has conflicts or unreasonable values.");
    }

    [Fact]
    public void Validate_WithReservedPorts_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.Api = new ApiConfiguration
        {
            Port = 80, // Reserved port
            HttpsEnabled = false,
            AuthEnabled = true,
        };

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Port configuration has conflicts or unreasonable values.");
    }

    [Fact]
    public void Validate_WithInvalidPortRange_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.Api = new ApiConfiguration
        {
            Port = 500, // Below 1024
            HttpsEnabled = false,
            AuthEnabled = true,
        };

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Port configuration has conflicts or unreasonable values.");
    }

    [Fact]
    public void Validate_ProductionWithoutAuth_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.System = new SystemConfiguration
        {
            Environment = "Production",
            LogLevel = "Information",
            ApplicationName = "SnapDog2",
            Version = "1.0.0",
            DebugEnabled = false,
        };
        configuration.Api = new ApiConfiguration
        {
            Port = 8080,
            HttpsEnabled = true,
            HttpsPort = 8443,
            AuthEnabled = false, // Should be enabled in production
        };

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Environment settings are inconsistent across configurations.");
    }

    [Fact]
    public void Validate_ProductionWithDebugEnabled_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.System = new SystemConfiguration
        {
            Environment = "Production",
            LogLevel = "Information",
            ApplicationName = "SnapDog2",
            Version = "1.0.0",
            DebugEnabled = true, // Should be disabled in production
        };

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Environment settings are inconsistent across configurations.");
    }

    [Fact]
    public void Validate_ProductionWithoutHttps_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.System = new SystemConfiguration
        {
            Environment = "Production",
            LogLevel = "Information",
            ApplicationName = "SnapDog2",
            Version = "1.0.0",
            DebugEnabled = false,
        };
        configuration.Api = new ApiConfiguration
        {
            Port = 8080,
            HttpsEnabled = false, // Should be enabled in production
            AuthEnabled = true,
        };

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Environment settings are inconsistent across configurations.");
    }

    [Fact]
    public void Validate_DevelopmentConfiguration_ShouldBeValid()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.System = new SystemConfiguration
        {
            Environment = "Development",
            LogLevel = "Debug",
            ApplicationName = "SnapDog2",
            Version = "1.0.0",
            DebugEnabled = true,
        };
        configuration.Api = new ApiConfiguration
        {
            Port = 8080,
            HttpsEnabled = false,
            AuthEnabled = false,
        };

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithCaseInsensitiveMacAddresses_ShouldDetectDuplicates()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.Clients = new List<ClientConfiguration>
        {
            new()
            {
                Name = "Client 1",
                Mac = "00:11:22:33:44:55",
                DefaultZone = 1,
            },
            new()
            {
                Name = "Client 2",
                Mac = "00:11:22:33:44:55".ToUpperInvariant(),
                DefaultZone = 1,
            },
        };

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Clients)
            .WithErrorMessage("MAC addresses must be unique across all clients.");
    }

    [Fact]
    public void Validate_WithCaseInsensitiveRadioStationUrls_ShouldDetectDuplicates()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.RadioStations = new List<RadioStationConfiguration>
        {
            new() { Name = "Station 1", Url = "http://stream.example.com" },
            new() { Name = "Station 2", Url = "HTTP://STREAM.EXAMPLE.COM" },
        };

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.RadioStations)
            .WithErrorMessage("Radio station URLs should be unique to avoid conflicts.");
    }

    [Fact]
    public void Validate_WithEmptyClientNames_ShouldNotCauseUniqueNameValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.Clients = new List<ClientConfiguration>
        {
            new()
            {
                Name = "",
                Mac = "00:11:22:33:44:55",
                DefaultZone = 1,
            },
            new()
            {
                Name = "",
                Mac = "00:11:22:33:44:66",
                DefaultZone = 1,
            },
        };

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert - Should not have unique name error since empty names are filtered out
        result.ShouldNotHaveValidationErrorFor(x => x.Clients);
    }

    [Fact]
    public void Validate_WithEmptyRadioStationNames_ShouldNotCauseUniqueNameValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.RadioStations = new List<RadioStationConfiguration>
        {
            new() { Name = "", Url = "http://stream1.example.com" },
            new() { Name = "", Url = "http://stream2.example.com" },
        };

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert - Should not have unique name error since empty names are filtered out
        result.ShouldNotHaveValidationErrorFor(x => x.RadioStations);
    }

    private static SnapDogConfiguration CreateValidConfiguration()
    {
        return new SnapDogConfiguration
        {
            System = new SystemConfiguration
            {
                Environment = "Development",
                LogLevel = "Information",
                ApplicationName = "SnapDog2",
                Version = "1.0.0",
                DebugEnabled = false,
            },
            Api = new ApiConfiguration
            {
                Port = 8080,
                HttpsEnabled = true,
                HttpsPort = 8443,
                AuthEnabled = true,
            },
            Telemetry = new TelemetryConfiguration(),
            Services = new ServicesConfiguration(),
            Zones = new List<ZoneConfiguration>
            {
                new() { Id = 1, Name = "Living Room" },
                new() { Id = 2, Name = "Kitchen" },
            },
            Clients = new List<ClientConfiguration>
            {
                new()
                {
                    Name = "Client 1",
                    Mac = "00:11:22:33:44:55",
                    DefaultZone = 1,
                },
                new()
                {
                    Name = "Client 2",
                    Mac = "00:11:22:33:44:66",
                    DefaultZone = 2,
                },
            },
            RadioStations = new List<RadioStationConfiguration>
            {
                new() { Name = "Jazz FM", Url = "http://jazz.example.com" },
                new() { Name = "Rock Radio", Url = "http://rock.example.com" },
            },
        };
    }
}
