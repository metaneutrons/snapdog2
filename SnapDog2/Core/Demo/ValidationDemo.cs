using Microsoft.Extensions.Logging;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;
using SnapDog2.Core.Validation;

namespace SnapDog2.Core.Demo;

/// <summary>
/// Demonstrates validation capabilities across all domain entities and value objects.
/// Shows both successful validation scenarios and various error conditions.
/// </summary>
public class ValidationDemo
{
    private readonly ILogger<ValidationDemo> _logger;

    public ValidationDemo(ILogger<ValidationDemo> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Runs the complete validation demonstration.
    /// </summary>
    public async Task RunDemoAsync()
    {
        _logger.LogInformation("=== Validation Demo ===");

        try
        {
            await DemonstrateValueObjectValidationAsync();
            await DemonstrateEntityValidationAsync();
            await DemonstrateConfigurationValidationAsync();
            await DemonstrateValidationSuccessAsync();

            _logger.LogInformation("Validation demo completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation demo failed");
            throw;
        }
    }

    /// <summary>
    /// Demonstrates value object validation scenarios.
    /// </summary>
    private async Task DemonstrateValueObjectValidationAsync()
    {
        _logger.LogInformation("--- Value Object Validation Demo ---");

        // MacAddress validation
        await DemonstrateMacAddressValidationAsync();

        // IpAddress validation
        await DemonstrateIpAddressValidationAsync();

        // StreamUrl validation
        await DemonstrateStreamUrlValidationAsync();

        await Task.Delay(100);
    }

    /// <summary>
    /// Demonstrates MAC address validation scenarios.
    /// </summary>
    private async Task DemonstrateMacAddressValidationAsync()
    {
        _logger.LogInformation("--- MAC Address Validation ---");

        var validMacFormats = new[]
        {
            "AA:BB:CC:DD:EE:FF",
            "aa:bb:cc:dd:ee:ff",
            "AA-BB-CC-DD-EE-FF",
            "11:22:33:44:55:66",
        };

        var invalidMacFormats = new[]
        {
            "invalid-mac",
            "AA:BB:CC:DD:EE",
            "AA:BB:CC:DD:EE:FF:GG",
            "XX:YY:ZZ:AA:BB:CC",
            "",
            null,
        };

        foreach (var mac in validMacFormats)
        {
            var isValid = MacAddress.IsValid(mac);
            _logger.LogInformation("MAC '{Mac}' is valid: {IsValid}", mac, isValid);

            if (isValid)
            {
                try
                {
                    var macAddress = new MacAddress(mac);
                    _logger.LogInformation("  Normalized: {Normalized}", macAddress.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("  Failed to create MAC address: {Error}", ex.Message);
                }
            }
        }

        foreach (var mac in invalidMacFormats)
        {
            var isValid = MacAddress.IsValid(mac);
            _logger.LogInformation("MAC '{Mac}' is valid: {IsValid}", mac ?? "null", isValid);

            if (!isValid)
            {
                try
                {
                    var macAddress = new MacAddress(mac!);
                    _logger.LogWarning("  Unexpectedly succeeded creating invalid MAC");
                }
                catch (ArgumentException ex)
                {
                    _logger.LogInformation("  Expected validation error: {Error}", ex.Message);
                }
            }
        }

        await Task.Delay(50);
    }

    /// <summary>
    /// Demonstrates IP address validation scenarios.
    /// </summary>
    private async Task DemonstrateIpAddressValidationAsync()
    {
        _logger.LogInformation("--- IP Address Validation ---");

        var validIpAddresses = new[]
        {
            "192.168.1.1",
            "10.0.0.1",
            "127.0.0.1",
            "255.255.255.255",
            "::1",
            "2001:db8::1",
        };

        var invalidIpAddresses = new[] { "invalid-ip", "256.256.256.256", "192.168.1", "192.168.1.1.1", "", null };

        foreach (var ip in validIpAddresses)
        {
            var isValid = IpAddress.IsValid(ip);
            _logger.LogInformation("IP '{Ip}' is valid: {IsValid}", ip, isValid);

            if (isValid)
            {
                try
                {
                    var ipAddress = new IpAddress(ip);
                    _logger.LogInformation(
                        "  Type: IPv4={IsIPv4}, IPv6={IsIPv6}, Loopback={IsLoopback}",
                        ipAddress.IsIPv4,
                        ipAddress.IsIPv6,
                        ipAddress.IsLoopback
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("  Failed to create IP address: {Error}", ex.Message);
                }
            }
        }

        foreach (var ip in invalidIpAddresses)
        {
            var isValid = IpAddress.IsValid(ip);
            _logger.LogInformation("IP '{Ip}' is valid: {IsValid}", ip ?? "null", isValid);

            if (!isValid)
            {
                try
                {
                    var ipAddress = new IpAddress(ip!);
                    _logger.LogWarning("  Unexpectedly succeeded creating invalid IP");
                }
                catch (ArgumentException ex)
                {
                    _logger.LogInformation("  Expected validation error: {Error}", ex.Message);
                }
            }
        }

        await Task.Delay(50);
    }

    /// <summary>
    /// Demonstrates stream URL validation scenarios.
    /// </summary>
    private async Task DemonstrateStreamUrlValidationAsync()
    {
        _logger.LogInformation("--- Stream URL Validation ---");

        var validUrls = new[]
        {
            "http://example.com/stream",
            "https://secure.example.com/stream",
            "file:///path/to/file.mp3",
            "ftp://ftp.example.com/music.mp3",
            "rtsp://rtsp.example.com/stream",
            "rtmp://rtmp.example.com/stream",
        };

        var invalidUrls = new[]
        {
            "invalid-url",
            "not-a-url",
            "smtp://mail.example.com", // Unsupported scheme
            "",
            null,
        };

        foreach (var url in validUrls)
        {
            var isValid = StreamUrl.IsValid(url);
            _logger.LogInformation("URL '{Url}' is valid: {IsValid}", url, isValid);

            if (isValid)
            {
                try
                {
                    var streamUrl = new StreamUrl(url);
                    _logger.LogInformation(
                        "  Scheme: {Scheme}, Host: {Host}, IsSecure: {IsSecure}",
                        streamUrl.Scheme,
                        streamUrl.Host,
                        streamUrl.IsSecure
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("  Failed to create stream URL: {Error}", ex.Message);
                }
            }
        }

        foreach (var url in invalidUrls)
        {
            var isValid = StreamUrl.IsValid(url);
            _logger.LogInformation("URL '{Url}' is valid: {IsValid}", url ?? "null", isValid);

            if (!isValid)
            {
                try
                {
                    var streamUrl = new StreamUrl(url!);
                    _logger.LogWarning("  Unexpectedly succeeded creating invalid URL");
                }
                catch (ArgumentException ex)
                {
                    _logger.LogInformation("  Expected validation error: {Error}", ex.Message);
                }
            }
        }

        await Task.Delay(50);
    }

    /// <summary>
    /// Demonstrates entity validation scenarios.
    /// </summary>
    private async Task DemonstrateEntityValidationAsync()
    {
        _logger.LogInformation("--- Entity Validation Demo ---");

        await DemonstrateZoneValidationAsync();
        await DemonstrateClientValidationAsync();
        await DemonstrateAudioStreamValidationAsync();
        await DemonstratePlaylistValidationAsync();
        await DemonstrateTrackValidationAsync();
        await DemonstrateRadioStationValidationAsync();

        await Task.Delay(100);
    }

    /// <summary>
    /// Demonstrates zone validation scenarios.
    /// </summary>
    private async Task DemonstrateZoneValidationAsync()
    {
        _logger.LogInformation("--- Zone Validation ---");

        // Valid zone creation
        try
        {
            var validZone = Zone.Create("valid-zone", "Valid Zone", "A properly configured zone");
            _logger.LogInformation("Created valid zone: {ZoneName}", validZone.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to create valid zone: {Error}", ex.Message);
        }

        // Invalid zone scenarios
        var invalidZoneScenarios = new List<(string Scenario, Func<Zone> CreateAction)>
        {
            ("Empty ID", static () => Zone.Create("", "Valid Name")),
            ("Null ID", static () => Zone.Create(null!, "Valid Name")),
            ("Empty Name", static () => Zone.Create("valid-id", "")),
            ("Null Name", static () => Zone.Create("valid-id", null!)),
        };

        foreach (var (scenario, createAction) in invalidZoneScenarios)
        {
            try
            {
                var zone = createAction();
                _logger.LogWarning("Zone validation scenario '{Scenario}' unexpectedly succeeded", scenario);
            }
            catch (ArgumentException ex)
            {
                _logger.LogInformation("Zone validation scenario '{Scenario}': {Error}", scenario, ex.Message);
            }
        }

        // Volume validation
        try
        {
            var zone = Zone.Create("test-zone", "Test Zone");
            var zoneWithInvalidVolume = zone.WithVolumeSettings(150, 0, 100); // Invalid default > max
            _logger.LogWarning("Volume validation unexpectedly succeeded");
        }
        catch (ArgumentException ex)
        {
            _logger.LogInformation("Volume validation error (expected): {Error}", ex.Message);
        }

        await Task.Delay(50);
    }

    /// <summary>
    /// Demonstrates client validation scenarios.
    /// </summary>
    private async Task DemonstrateClientValidationAsync()
    {
        _logger.LogInformation("--- Client Validation ---");

        // Valid client creation
        try
        {
            var validClient = Client.Create(
                "valid-client",
                "Valid Client",
                new MacAddress("AA:BB:CC:DD:EE:FF"),
                new IpAddress("192.168.1.100"),
                ClientStatus.Connected,
                75
            );
            _logger.LogInformation("Created valid client: {ClientName}", validClient.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to create valid client: {Error}", ex.Message);
        }

        // Invalid client scenarios
        var invalidClientScenarios = new List<(string Scenario, Func<Client> CreateAction)>
        {
            (
                "Empty ID",
                static () =>
                    Client.Create("", "Valid Name", new MacAddress("AA:BB:CC:DD:EE:FF"), new IpAddress("192.168.1.1"))
            ),
            (
                "Empty Name",
                static () =>
                    Client.Create("valid-id", "", new MacAddress("AA:BB:CC:DD:EE:FF"), new IpAddress("192.168.1.1"))
            ),
            (
                "Invalid Volume (-1)",
                static () =>
                    Client.Create(
                        "valid-id",
                        "Valid Name",
                        new MacAddress("AA:BB:CC:DD:EE:FF"),
                        new IpAddress("192.168.1.1"),
                        volume: -1
                    )
            ),
            (
                "Invalid Volume (101)",
                static () =>
                    Client.Create(
                        "valid-id",
                        "Valid Name",
                        new MacAddress("AA:BB:CC:DD:EE:FF"),
                        new IpAddress("192.168.1.1"),
                        volume: 101
                    )
            ),
        };

        foreach (var (scenario, createAction) in invalidClientScenarios)
        {
            try
            {
                var client = createAction();
                _logger.LogWarning("Client validation scenario '{Scenario}' unexpectedly succeeded", scenario);
            }
            catch (ArgumentException ex)
            {
                _logger.LogInformation("Client validation scenario '{Scenario}': {Error}", scenario, ex.Message);
            }
        }

        await Task.Delay(50);
    }

    /// <summary>
    /// Demonstrates audio stream validation scenarios.
    /// </summary>
    private async Task DemonstrateAudioStreamValidationAsync()
    {
        _logger.LogInformation("--- Audio Stream Validation ---");

        // Valid stream creation
        try
        {
            var validStream = AudioStream.Create(
                "valid-stream",
                "Valid Stream",
                new StreamUrl("http://example.com/stream"),
                AudioCodec.MP3,
                320
            );
            _logger.LogInformation("Created valid audio stream: {StreamName}", validStream.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to create valid audio stream: {Error}", ex.Message);
        }

        // Invalid stream scenarios
        var invalidStreamScenarios = new List<(string Scenario, Func<AudioStream> CreateAction)>
        {
            (
                "Empty ID",
                static () =>
                    AudioStream.Create("", "Valid Name", new StreamUrl("http://example.com"), AudioCodec.MP3, 320)
            ),
            (
                "Empty Name",
                static () =>
                    AudioStream.Create("valid-id", "", new StreamUrl("http://example.com"), AudioCodec.MP3, 320)
            ),
            (
                "Invalid Bitrate (0)",
                static () =>
                    AudioStream.Create("valid-id", "Valid Name", new StreamUrl("http://example.com"), AudioCodec.MP3, 0)
            ),
            (
                "Invalid Bitrate (-1)",
                static () =>
                    AudioStream.Create(
                        "valid-id",
                        "Valid Name",
                        new StreamUrl("http://example.com"),
                        AudioCodec.MP3,
                        -1
                    )
            ),
        };

        foreach (var (scenario, createAction) in invalidStreamScenarios)
        {
            try
            {
                var stream = createAction();
                _logger.LogWarning("Stream validation scenario '{Scenario}' unexpectedly succeeded", scenario);
            }
            catch (ArgumentException ex)
            {
                _logger.LogInformation("Stream validation scenario '{Scenario}': {Error}", scenario, ex.Message);
            }
        }

        await Task.Delay(50);
    }

    /// <summary>
    /// Demonstrates playlist validation scenarios.
    /// </summary>
    private async Task DemonstratePlaylistValidationAsync()
    {
        _logger.LogInformation("--- Playlist Validation ---");

        // Valid playlist creation
        try
        {
            var validPlaylist = Playlist.Create("valid-playlist", "Valid Playlist", "A test playlist", "user");
            _logger.LogInformation("Created valid playlist: {PlaylistName}", validPlaylist.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to create valid playlist: {Error}", ex.Message);
        }

        // Invalid playlist scenarios
        var invalidPlaylistScenarios = new List<(string Scenario, Func<Playlist> CreateAction)>
        {
            ("Empty ID", static () => Playlist.Create("", "Valid Name")),
            ("Empty Name", static () => Playlist.Create("valid-id", "")),
            ("Null ID", static () => Playlist.Create(null!, "Valid Name")),
            ("Null Name", static () => Playlist.Create("valid-id", null!)),
        };

        foreach (var (scenario, createAction) in invalidPlaylistScenarios)
        {
            try
            {
                var playlist = createAction();
                _logger.LogWarning("Playlist validation scenario '{Scenario}' unexpectedly succeeded", scenario);
            }
            catch (ArgumentException ex)
            {
                _logger.LogInformation("Playlist validation scenario '{Scenario}': {Error}", scenario, ex.Message);
            }
        }

        await Task.Delay(50);
    }

    /// <summary>
    /// Demonstrates track validation scenarios.
    /// </summary>
    private async Task DemonstrateTrackValidationAsync()
    {
        _logger.LogInformation("--- Track Validation ---");

        // Valid track creation
        try
        {
            var validTrack = Track.Create("valid-track", "Valid Track", "Artist", "Album");
            _logger.LogInformation("Created valid track: {TrackTitle}", validTrack.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to create valid track: {Error}", ex.Message);
        }

        // Invalid track scenarios
        var invalidTrackScenarios = new List<(string Scenario, Func<Track> CreateAction)>
        {
            ("Empty ID", static () => Track.Create("", "Valid Title")),
            ("Empty Title", static () => Track.Create("valid-id", "")),
            ("Null ID", static () => Track.Create(null!, "Valid Title")),
            ("Null Title", static () => Track.Create("valid-id", null!)),
        };

        foreach (var (scenario, createAction) in invalidTrackScenarios)
        {
            try
            {
                var track = createAction();
                _logger.LogWarning("Track validation scenario '{Scenario}' unexpectedly succeeded", scenario);
            }
            catch (ArgumentException ex)
            {
                _logger.LogInformation("Track validation scenario '{Scenario}': {Error}", scenario, ex.Message);
            }
        }

        await Task.Delay(50);
    }

    /// <summary>
    /// Demonstrates radio station validation scenarios.
    /// </summary>
    private async Task DemonstrateRadioStationValidationAsync()
    {
        _logger.LogInformation("--- Radio Station Validation ---");

        // Valid radio station creation
        try
        {
            var validStation = RadioStation.Create(
                "valid-station",
                "Valid Station",
                new StreamUrl("http://radio.example.com"),
                AudioCodec.MP3,
                "A test radio station"
            );
            _logger.LogInformation("Created valid radio station: {StationName}", validStation.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to create valid radio station: {Error}", ex.Message);
        }

        // Invalid radio station scenarios
        var invalidStationScenarios = new List<(string Scenario, Func<RadioStation> CreateAction)>
        {
            (
                "Empty ID",
                static () => RadioStation.Create("", "Valid Name", new StreamUrl("http://example.com"), AudioCodec.MP3)
            ),
            (
                "Empty Name",
                static () => RadioStation.Create("valid-id", "", new StreamUrl("http://example.com"), AudioCodec.MP3)
            ),
            (
                "Null ID",
                static () =>
                    RadioStation.Create(null!, "Valid Name", new StreamUrl("http://example.com"), AudioCodec.MP3)
            ),
            (
                "Null Name",
                static () => RadioStation.Create("valid-id", null!, new StreamUrl("http://example.com"), AudioCodec.MP3)
            ),
        };

        foreach (var (scenario, createAction) in invalidStationScenarios)
        {
            try
            {
                var station = createAction();
                _logger.LogWarning("Radio station validation scenario '{Scenario}' unexpectedly succeeded", scenario);
            }
            catch (ArgumentException ex)
            {
                _logger.LogInformation("Radio station validation scenario '{Scenario}': {Error}", scenario, ex.Message);
            }
        }

        await Task.Delay(50);
    }

    /// <summary>
    /// Demonstrates configuration validation scenarios.
    /// </summary>
    private async Task DemonstrateConfigurationValidationAsync()
    {
        _logger.LogInformation("--- Configuration Validation Demo ---");

        // Test basic configuration validation
        var config = new SnapDogConfiguration
        {
            System = new SystemConfiguration { Environment = "Demo", LogLevel = "Information" },
            Api = new ApiConfiguration { Port = 5000 },
            Telemetry = new TelemetryConfiguration { Enabled = true, ServiceName = "snapdog2-demo" },
        };

        try
        {
            var validator = new SnapDogConfigurationValidator();
            var validationResult = validator.Validate(config);

            if (validationResult.IsValid)
            {
                _logger.LogInformation("Configuration validation passed");
            }
            else
            {
                _logger.LogWarning("Configuration validation failed:");
                foreach (var error in validationResult.Errors)
                {
                    _logger.LogWarning("  - {PropertyName}: {ErrorMessage}", error.PropertyName, error.ErrorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Configuration validation error: {Error}", ex.Message);
        }

        await Task.Delay(100);
    }

    /// <summary>
    /// Demonstrates successful validation scenarios.
    /// </summary>
    private async Task DemonstrateValidationSuccessAsync()
    {
        _logger.LogInformation("--- Validation Success Demo ---");

        // Create a complete, valid entity hierarchy
        var zone = Zone.Create("success-zone", "Success Zone", "A fully valid zone")
            .WithVolumeSettings(60, 10, 90)
            .WithEnabled(true);

        var client = Client
            .Create(
                "success-client",
                "Success Client",
                new MacAddress("AA:BB:CC:DD:EE:FF"),
                new IpAddress("192.168.1.100"),
                ClientStatus.Connected,
                60
            )
            .WithZone("success-zone");

        var stream = AudioStream
            .Create(
                "success-stream",
                "Success Stream",
                new StreamUrl("https://stream.example.com/music"),
                AudioCodec.FLAC,
                1411
            )
            .WithStatus(StreamStatus.Playing);

        var playlist = Playlist
            .Create("success-playlist", "Success Playlist", "A valid playlist", "demo-user")
            .WithAddedTrack("track-1")
            .WithAddedTrack("track-2");

        var track = Track
            .Create("track-1", "Success Track", "Demo Artist", "Demo Album")
            .WithMetadata(genre: "Demo", year: 2024)
            .WithTechnicalInfo(durationSeconds: 180, bitrateKbps: 320, sampleRateHz: 44100, channels: 2);

        var radioStation = RadioStation
            .Create(
                "success-radio",
                "Success Radio",
                new StreamUrl("https://radio.example.com/stream"),
                AudioCodec.MP3,
                "A successful radio station"
            )
            .WithMetadata(genre: "Variety", country: "Demo", language: "English")
            .WithTechnicalInfo(bitrateKbps: 192, sampleRateHz: 44100, channels: 2);

        _logger.LogInformation("Successfully created all entity types:");
        _logger.LogInformation("  Zone: {ZoneName} (Active: {IsActive})", zone.Name, zone.IsActive);
        _logger.LogInformation("  Client: {ClientName} (Connected: {IsConnected})", client.Name, client.IsConnected);
        _logger.LogInformation("  Stream: {StreamName} (Playing: {IsPlaying})", stream.Name, stream.IsPlaying);
        _logger.LogInformation("  Playlist: {PlaylistName} (Tracks: {TrackCount})", playlist.Name, playlist.TrackCount);
        _logger.LogInformation("  Track: {TrackTitle} (Duration: {Duration})", track.Title, track.FormattedDuration);
        _logger.LogInformation(
            "  Radio: {RadioName} (Available: {IsAvailable})",
            radioStation.Name,
            radioStation.IsAvailable
        );

        await Task.Delay(100);
    }

    /// <summary>
    /// Demonstrates validation error scenarios for testing error handling.
    /// </summary>
    public async Task DemonstrateValidationErrorsAsync()
    {
        _logger.LogInformation("--- Validation Errors Demo ---");

        // Intentionally create invalid scenarios for testing
        try
        {
            var invalidZone = Zone.Create("", ""); // Both ID and name empty
        }
        catch (ArgumentException ex)
        {
            _logger.LogInformation("Caught expected validation error: {Error}", ex.Message);
            throw; // Re-throw for demo error handling
        }

        await Task.Delay(50);
    }
}
