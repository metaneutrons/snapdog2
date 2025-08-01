using FluentValidation.TestHelper;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Validation;
using Xunit;

namespace SnapDog2.Tests.Unit.Validation;

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
        result.ShouldHaveValidationErrorFor(static x => x.System);
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
        result.ShouldHaveValidationErrorFor(static x => x.Api);
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
        result.ShouldHaveValidationErrorFor(static x => x.Telemetry);
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
        result.ShouldHaveValidationErrorFor(static x => x.Services);
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
        result.ShouldHaveValidationErrorFor(static x => x.Zones).WithErrorMessage("Zones collection cannot be null.");
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
        result
            .ShouldHaveValidationErrorFor(static x => x.Zones)
            .WithErrorMessage("At least one zone must be configured.");
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
            .ShouldHaveValidationErrorFor(static x => x.Zones)
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
        result
            .ShouldHaveValidationErrorFor(static x => x.Clients)
            .WithErrorMessage("Clients collection cannot be null.");
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
            .ShouldHaveValidationErrorFor(static x => x.Clients)
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
            .ShouldHaveValidationErrorFor(static x => x.Clients)
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
            .ShouldHaveValidationErrorFor(static x => x.RadioStations)
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
            .ShouldHaveValidationErrorFor(static x => x.RadioStations)
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
            .ShouldHaveValidationErrorFor(static x => x.RadioStations)
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
            .ShouldHaveValidationErrorFor(static x => x)
            .WithErrorMessage("All client default zone assignments must reference existing zone IDs.");
    }

    [Fact]
    public void Validate_ProductionWithoutAuth_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.System = new SystemConfiguration { Environment = "Production" };
        configuration.Api = new ApiConfiguration
        {
            AuthEnabled = false, // Should be enabled in production
        };

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result
            .ShouldHaveValidationErrorFor(static x => x)
            .WithErrorMessage("API authentication must be enabled in Production environment.");
    }

    [Fact]
    public void Validate_ProductionWithDebugEnabled_ShouldHaveValidationError()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        configuration.System = new SystemConfiguration
        {
            Environment = "Production",
            DebugEnabled = true, // Should be disabled in production
        };

        // Act
        var result = _validator.TestValidate(configuration);

        // Assert
        result
            .ShouldHaveValidationErrorFor(static x => x)
            .WithErrorMessage("Debug mode must be disabled in Production environment.");
    }

    [Fact]
    private static SnapDogConfiguration CreateValidConfiguration()
    {
        return new SnapDogConfiguration
        {
            System = new SystemConfiguration
            {
                Environment = "Development",
                LogLevel = "Information",
                DebugEnabled = false,
            },
            Api = new ApiConfiguration { Port = 8080, AuthEnabled = true },
            Telemetry = new TelemetryConfiguration(),
            Services = new ServicesConfiguration
            {
                Snapcast = new SnapcastConfiguration { Enabled = true, Host = "localhost" },
                Mqtt = new ServicesMqttConfiguration { Enabled = true, Broker = "localhost" },
                Knx = new KnxConfiguration { Enabled = false },
                Subsonic = new SubsonicConfiguration { Enabled = false },
                Resilience = new ResilienceConfiguration(),
            },
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
