namespace SnapDog2.Tests.Integration.Knx;

using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using global::Knx.Falcon;
using global::Knx.Falcon.Configuration;
using global::Knx.Falcon.Sdk;
using MQTTnet;
using MQTTnet.Client;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Commands.Playback;
using SnapDog2.Server.Features.Zones.Commands.Track;
using SnapDog2.Server.Features.Zones.Commands.Volume;
using SnapDog2.Tests.Integration.Fixtures;

/// <summary>
/// Comprehensive KNX integration tests that validate the complete mediator flow:
/// KNX Group Address Write → Mediator Commands → All Integrations (MQTT, Snapcast, API)
///
/// Tests realistic multi-room audio scenarios using actual KNX group address writes
/// to trigger volume, playback, and control commands across all system integrations.
/// </summary>
[Collection("KnxIntegrationFlow")]
public class KnxMediatorFlowTests : IClassFixture<KnxIntegrationTestFixture>
{
    private readonly KnxIntegrationTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public KnxMediatorFlowTests(KnxIntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    #region Volume Control Integration Tests

    [Theory]
    [InlineData("1/2/3", 5, "ground-floor")] // Zone 1 (Ground Floor) Volume Up
    [InlineData("2/2/3", 5, "first-floor")] // Zone 2 (1st Floor) Volume Up
    public async Task KnxVolumeUp_ShouldTriggerCompleteIntegrationFlow(
        string groupAddress,
        int expectedStep,
        string zoneName
    )
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();
        var initialVolume = await GetCurrentZoneVolume(zoneName);
        var expectedVolume = Math.Min(100, initialVolume + expectedStep);

        _output.WriteLine($"Testing KNX Volume Up: GA={groupAddress}, Zone={zoneName}, Initial={initialVolume}");

        // Act: Write to KNX Group Address (simulating wall panel button press)
        await _fixture.KnxClient.WriteGroupValueAsync(groupAddress, true);

        // Assert 1: Mediator Command Processing
        var mediatorCommand = await _fixture.MediatorSpy.WaitForCommand<VolumeUpCommand>(TimeSpan.FromSeconds(2));
        mediatorCommand.Should().NotBeNull();
        mediatorCommand.ZoneIndex.Should().Be(GetZoneIndex(zoneName));
        mediatorCommand.Step.Should().Be(expectedStep);
        mediatorCommand.Source.Should().Be(CommandSource.Knx);

        // Assert 2: MQTT State Update
        var mqttMessage = await _fixture.MqttTestClient.WaitForMessage(
            $"snapdog/zones/{zoneName}/volume/status",
            TimeSpan.FromSeconds(3)
        );
        var mqttState = JsonSerializer.Deserialize<ZoneVolumeState>(mqttMessage);
        mqttState.Volume.Should().Be(expectedVolume);

        // Assert 3: Snapcast Client Update
        var snapcastStatus = await _fixture.SnapcastTestClient.GetClientStatus(GetSnapcastClientId(zoneName));
        snapcastStatus.Volume.Should().Be(expectedVolume);

        // Assert 4: API Endpoint State
        var apiResponse = await _fixture.ApiClient.GetAsync($"/api/v1/zones/{GetZoneIndex(zoneName)}");
        apiResponse.Should().BeSuccessful();
        var zoneState = await apiResponse.Content.ReadFromJsonAsync<ZoneState>();
        zoneState.Volume.Should().Be(expectedVolume);

        // Assert 5: KNX Status Group Address Updated (bi-directional)
        var knxStatusValue = await _fixture.KnxClient.ReadGroupValueAsync(GetVolumeStatusGroupAddress(zoneName));
        knxStatusValue.Should().Be(expectedVolume);

        stopwatch.Stop();
        _output.WriteLine($"Complete flow processed in {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // < 1 second end-to-end
    }

    [Theory]
    [InlineData("1/2/4", 5, "ground-floor")] // Zone 1 (Ground Floor) Volume Down
    [InlineData("2/2/4", 5, "first-floor")] // Zone 2 (1st Floor) Volume Down
    public async Task KnxVolumeDown_ShouldTriggerCompleteIntegrationFlow(
        string groupAddress,
        int expectedStep,
        string zoneName
    )
    {
        // Arrange
        var initialVolume = await GetCurrentZoneVolume(zoneName);
        var expectedVolume = Math.Max(0, initialVolume - expectedStep);

        _output.WriteLine($"Testing KNX Volume Down: GA={groupAddress}, Zone={zoneName}, Initial={initialVolume}");

        // Act: Write to KNX Group Address
        await _fixture.KnxClient.WriteGroupValueAsync(groupAddress, true);

        // Assert: Complete integration flow (similar to VolumeUp)
        var mediatorCommand = await _fixture.MediatorSpy.WaitForCommand<VolumeDownCommand>(TimeSpan.FromSeconds(2));
        mediatorCommand.Should().NotBeNull();
        mediatorCommand.ZoneIndex.Should().Be(GetZoneIndex(zoneName));

        await VerifyAllIntegrationsUpdated(zoneName, expectedVolume);
    }

    [Theory]
    [InlineData("1/2/1", 75, "ground-floor")] // Zone 1 (Ground Floor) Set Volume 75%
    [InlineData("2/2/1", 50, "first-floor")] // Zone 2 (1st Floor) Set Volume 50%
    public async Task KnxSetVolume_ShouldTriggerCompleteIntegrationFlow(
        string groupAddress,
        int targetVolume,
        string zoneName
    )
    {
        _output.WriteLine($"Testing KNX Set Volume: GA={groupAddress}, Zone={zoneName}, Target={targetVolume}%");

        // Act: Write target volume to KNX Group Address
        await _fixture.KnxClient.WriteGroupValueAsync(groupAddress, (byte)targetVolume);

        // Assert: Mediator processes SetZoneVolumeCommand
        var mediatorCommand = await _fixture.MediatorSpy.WaitForCommand<SetZoneVolumeCommand>(TimeSpan.FromSeconds(2));
        mediatorCommand.Should().NotBeNull();
        mediatorCommand.ZoneIndex.Should().Be(GetZoneIndex(zoneName));
        mediatorCommand.Volume.Should().Be(targetVolume);
        mediatorCommand.Source.Should().Be(CommandSource.Knx);

        await VerifyAllIntegrationsUpdated(zoneName, targetVolume);
    }

    #endregion

    #region Playback Control Integration Tests

    [Theory]
    [InlineData("1/1/1", "ground-floor", PlaybackState.Playing)] // Zone 1 (Ground Floor) Play
    [InlineData("2/1/1", "first-floor", PlaybackState.Playing)] // Zone 2 (1st Floor) Play
    public async Task KnxPlayCommand_ShouldTriggerCompleteIntegrationFlow(
        string groupAddress,
        string zoneName,
        PlaybackState expectedState
    )
    {
        _output.WriteLine($"Testing KNX Play: GA={groupAddress}, Zone={zoneName}");

        // Act: Write to KNX Play Group Address
        await _fixture.KnxClient.WriteGroupValueAsync(groupAddress, true);

        // Assert: Mediator processes PlayCommand
        var mediatorCommand = await _fixture.MediatorSpy.WaitForCommand<PlayCommand>(TimeSpan.FromSeconds(2));
        mediatorCommand.Should().NotBeNull();
        mediatorCommand.ZoneIndex.Should().Be(GetZoneIndex(zoneName));
        mediatorCommand.Source.Should().Be(CommandSource.Knx);

        await VerifyPlaybackStateAcrossIntegrations(zoneName, expectedState);
    }

    [Theory]
    [InlineData("1/1/2", "ground-floor", PlaybackState.Paused)] // Zone 1 (Ground Floor) Pause
    [InlineData("2/1/2", "first-floor", PlaybackState.Paused)] // Zone 2 (1st Floor) Pause
    public async Task KnxPauseCommand_ShouldTriggerCompleteIntegrationFlow(
        string groupAddress,
        string zoneName,
        PlaybackState expectedState
    )
    {
        _output.WriteLine($"Testing KNX Pause: GA={groupAddress}, Zone={zoneName}");

        // Act: Write to KNX Pause Group Address
        await _fixture.KnxClient.WriteGroupValueAsync(groupAddress, true);

        // Assert: Mediator processes PauseCommand
        var mediatorCommand = await _fixture.MediatorSpy.WaitForCommand<PauseCommand>(TimeSpan.FromSeconds(2));
        mediatorCommand.Should().NotBeNull();

        await VerifyPlaybackStateAcrossIntegrations(zoneName, expectedState);
    }

    [Theory]
    [InlineData("1/1/3", "ground-floor", PlaybackState.Stopped)] // Zone 1 (Ground Floor) Stop
    [InlineData("2/1/3", "first-floor", PlaybackState.Stopped)] // Zone 2 (1st Floor) Stop
    public async Task KnxStopCommand_ShouldTriggerCompleteIntegrationFlow(
        string groupAddress,
        string zoneName,
        PlaybackState expectedState
    )
    {
        _output.WriteLine($"Testing KNX Stop: GA={groupAddress}, Zone={zoneName}");

        // Act: Write to KNX Stop Group Address
        await _fixture.KnxClient.WriteGroupValueAsync(groupAddress, true);

        // Assert: Mediator processes StopCommand
        var mediatorCommand = await _fixture.MediatorSpy.WaitForCommand<StopCommand>(TimeSpan.FromSeconds(2));
        mediatorCommand.Should().NotBeNull();

        await VerifyPlaybackStateAcrossIntegrations(zoneName, expectedState);
    }

    [Theory]
    [InlineData("1/1/4", "ground-floor")] // Zone 1 (Ground Floor) Next Track
    [InlineData("2/1/4", "first-floor")] // Zone 2 (1st Floor) Next Track
    public async Task KnxNextTrackCommand_ShouldTriggerCompleteIntegrationFlow(string groupAddress, string zoneName)
    {
        _output.WriteLine($"Testing KNX Next Track: GA={groupAddress}, Zone={zoneName}");

        // Arrange: Get current track info
        var initialTrack = await GetCurrentTrackInfo(zoneName);

        // Act: Write to KNX Next Track Group Address
        await _fixture.KnxClient.WriteGroupValueAsync(groupAddress, true);

        // Assert: Mediator processes NextTrackCommand
        var mediatorCommand = await _fixture.MediatorSpy.WaitForCommand<NextTrackCommand>(TimeSpan.FromSeconds(2));
        mediatorCommand.Should().NotBeNull();
        mediatorCommand.ZoneIndex.Should().Be(GetZoneIndex(zoneName));

        // Verify track changed across all integrations
        await VerifyTrackChangedAcrossIntegrations(zoneName, initialTrack);
    }

    #endregion

    #region Multi-Room Scenario Tests

    [Fact]
    public async Task KnxMasterVolumeUp_ShouldAffectAllZones()
    {
        _output.WriteLine("Testing KNX Master Volume Up affecting all zones");

        // Arrange: Get initial volumes for all zones
        var initialVolumes = new Dictionary<string, int>
        {
            ["ground-floor"] = await GetCurrentZoneVolume("ground-floor"),
            ["first-floor"] = await GetCurrentZoneVolume("first-floor"),
        };

        // Act: Write to Master Volume Up Group Address (using system status address)
        await _fixture.KnxClient.WriteGroupValueAsync("0/0/1", true);

        // Assert: All zones volume increased
        foreach (var zone in initialVolumes.Keys)
        {
            var expectedVolume = Math.Min(100, initialVolumes[zone] + 5);
            await VerifyAllIntegrationsUpdated(zone, expectedVolume);
            _output.WriteLine($"Zone {zone}: {initialVolumes[zone]} → {expectedVolume}");
        }
    }

    [Fact]
    public async Task KnxZoneTransfer_ShouldMovePlaybackBetweenRooms()
    {
        _output.WriteLine("Testing KNX Zone Transfer from Ground Floor to 1st Floor");

        // Arrange: Start playback in Ground Floor
        await _fixture.KnxClient.WriteGroupValueAsync("1/1/1", true); // Ground Floor Play
        await Task.Delay(1000); // Allow playback to start

        // Act: Transfer to 1st Floor (using scene master for zone transfer)
        await _fixture.KnxClient.WriteGroupValueAsync("0/0/10", true); // Scene Master GA

        // Assert: Ground Floor stopped, 1st Floor playing
        await VerifyPlaybackStateAcrossIntegrations("ground-floor", PlaybackState.Stopped);
        await VerifyPlaybackStateAcrossIntegrations("first-floor", PlaybackState.Playing);

        _output.WriteLine("Zone transfer completed successfully");
    }

    [Fact]
    public async Task KnxPartyMode_ShouldSynchronizeAllZones()
    {
        _output.WriteLine("Testing KNX Party Mode - synchronize all zones");

        // Act: Activate Party Mode
        await _fixture.KnxClient.WriteGroupValueAsync("0/0/10", true); // Scene Master GA

        // Assert: All zones synchronized to same volume and playback state
        var targetVolume = 70; // Party mode default volume
        var zones = new[] { "ground-floor", "first-floor" };

        foreach (var zone in zones)
        {
            await VerifyAllIntegrationsUpdated(zone, targetVolume);
            await VerifyPlaybackStateAcrossIntegrations(zone, PlaybackState.Playing);
        }

        _output.WriteLine("Party mode synchronization completed");
    }

    #endregion

    #region Bi-Directional Communication Tests

    [Fact]
    public async Task ApiVolumeChange_ShouldTriggerKnxStatusUpdate()
    {
        _output.WriteLine("Testing API → KNX bi-directional communication");

        // Act: Change volume via API
        var response = await _fixture.ApiClient.PostAsync(
            "/api/v1/zones/1/volume",
            JsonContent.Create(new { volume = 80 })
        );
        response.Should().BeSuccessful();

        // Assert: KNX status group address updated
        await Task.Delay(500); // Allow status propagation
        var knxStatusValue = await _fixture.KnxClient.ReadGroupValueAsync("1/0/10"); // Volume Status GA
        knxStatusValue.Should().Be(80);

        _output.WriteLine("API → KNX status update verified");
    }

    [Fact]
    public async Task MqttCommand_ShouldTriggerKnxStatusUpdate()
    {
        _output.WriteLine("Testing MQTT → KNX bi-directional communication");

        // Act: Send MQTT command
        await _fixture.MqttTestClient.PublishAsync("snapdog/zones/living-room/volume/set", "60");

        // Assert: KNX status updated
        await Task.Delay(500); // Allow status propagation
        var knxStatusValue = await _fixture.KnxClient.ReadGroupValueAsync("1/0/10");
        knxStatusValue.Should().Be(60);

        _output.WriteLine("MQTT → KNX status update verified");
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task KnxCommand_ShouldProcessWithinAcceptableTime()
    {
        _output.WriteLine("Testing KNX command processing performance");

        var stopwatch = Stopwatch.StartNew();

        // Act: Send KNX Volume Up command
        await _fixture.KnxClient.WriteGroupValueAsync("1/2/3", true);

        // Wait for complete flow across all integrations
        await _fixture.MqttTestClient.WaitForMessage(
            "snapdog/zones/ground-floor/volume/status",
            TimeSpan.FromSeconds(2)
        );

        stopwatch.Stop();

        _output.WriteLine($"End-to-end processing time: {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500); // < 500ms end-to-end
    }

    [Fact]
    public async Task ConcurrentKnxCommands_ShouldBeProcessedCorrectly()
    {
        _output.WriteLine("Testing concurrent KNX command processing");

        // Act: Send multiple KNX commands simultaneously
        var tasks = new[]
        {
            _fixture.KnxClient.WriteGroupValueAsync("1/2/3", true), // Ground Floor Volume Up
            _fixture.KnxClient.WriteGroupValueAsync("1/1/1", true), // Ground Floor Play
            _fixture.KnxClient.WriteGroupValueAsync("2/2/1", (byte)50), // 1st Floor Set Volume 50%
            _fixture.KnxClient.WriteGroupValueAsync("2/1/2", true), // 1st Floor Pause
        };

        await Task.WhenAll(tasks);

        // Assert: All commands processed correctly
        await Task.Delay(2000); // Allow all processing to complete

        // Verify each command was processed
        _fixture.MediatorSpy.ProcessedCommands.Should().HaveCount(4);
        _fixture.MediatorSpy.ProcessedCommands.Should().Contain(cmd => cmd is VolumeUpCommand);
        _fixture.MediatorSpy.ProcessedCommands.Should().Contain(cmd => cmd is PlayCommand);
        _fixture.MediatorSpy.ProcessedCommands.Should().Contain(cmd => cmd is SetZoneVolumeCommand);
        _fixture.MediatorSpy.ProcessedCommands.Should().Contain(cmd => cmd is PauseCommand);

        _output.WriteLine("All concurrent commands processed successfully");
    }

    #endregion

    #region Helper Methods

    private async Task<int> GetCurrentZoneVolume(string zoneName)
    {
        var response = await _fixture.ApiClient.GetAsync($"/api/v1/zones/{GetZoneIndex(zoneName)}");
        var zoneState = await response.Content.ReadFromJsonAsync<ZoneState>();
        return zoneState.Volume;
    }

    private async Task<TrackInfo> GetCurrentTrackInfo(string zoneName)
    {
        var response = await _fixture.ApiClient.GetAsync($"/api/v1/zones/{GetZoneIndex(zoneName)}/track");
        return await response.Content.ReadFromJsonAsync<TrackInfo>();
    }

    private async Task VerifyAllIntegrationsUpdated(string zoneName, int expectedVolume)
    {
        // MQTT verification
        var mqttMessage = await _fixture.MqttTestClient.WaitForMessage(
            $"snapdog/zones/{zoneName}/volume/status",
            TimeSpan.FromSeconds(3)
        );
        var mqttState = JsonSerializer.Deserialize<ZoneVolumeState>(mqttMessage);
        mqttState.Volume.Should().Be(expectedVolume);

        // Snapcast verification
        var snapcastStatus = await _fixture.SnapcastTestClient.GetClientStatus(GetSnapcastClientId(zoneName));
        snapcastStatus.Volume.Should().Be(expectedVolume);

        // API verification
        var apiResponse = await _fixture.ApiClient.GetAsync($"/api/v1/zones/{GetZoneIndex(zoneName)}");
        var zoneState = await apiResponse.Content.ReadFromJsonAsync<ZoneState>();
        zoneState.Volume.Should().Be(expectedVolume);

        // KNX status verification
        var knxStatusValue = await _fixture.KnxClient.ReadGroupValueAsync(GetVolumeStatusGroupAddress(zoneName));
        knxStatusValue.Should().Be(expectedVolume);
    }

    private async Task VerifyPlaybackStateAcrossIntegrations(string zoneName, PlaybackState expectedState)
    {
        // MQTT verification
        var mqttMessage = await _fixture.MqttTestClient.WaitForMessage(
            $"snapdog/zones/{zoneName}/playback/status",
            TimeSpan.FromSeconds(3)
        );
        var mqttState = JsonSerializer.Deserialize<PlaybackStatus>(mqttMessage);
        mqttState.State.Should().Be(expectedState);

        // Snapcast verification
        var snapcastStatus = await _fixture.SnapcastTestClient.GetServerStatus();
        var clientStatus = snapcastStatus
            .Groups.SelectMany(g => g.Clients)
            .First(c => c.Id == GetSnapcastClientId(zoneName));
        // Note: Snapcast playback state mapping would be implemented based on actual API

        // API verification
        var apiResponse = await _fixture.ApiClient.GetAsync($"/api/v1/zones/{GetZoneIndex(zoneName)}/playback");
        var playbackStatus = await apiResponse.Content.ReadFromJsonAsync<PlaybackStatus>();
        playbackStatus.State.Should().Be(expectedState);
    }

    private async Task VerifyTrackChangedAcrossIntegrations(string zoneName, TrackInfo initialTrack)
    {
        // Wait for track change to propagate
        await Task.Delay(1000);

        var currentTrack = await GetCurrentTrackInfo(zoneName);
        currentTrack.Should().NotBeEquivalentTo(initialTrack);

        // Verify MQTT track update
        var mqttMessage = await _fixture.MqttTestClient.WaitForMessage(
            $"snapdog/zones/{zoneName}/track/current",
            TimeSpan.FromSeconds(3)
        );
        var mqttTrack = JsonSerializer.Deserialize<TrackInfo>(mqttMessage);
        mqttTrack.Should().BeEquivalentTo(currentTrack);
    }

    private static int GetZoneIndex(string zoneName) =>
        zoneName switch
        {
            "ground-floor" => 1,
            "first-floor" => 2,
            _ => throw new ArgumentException($"Unknown zone: {zoneName}"),
        };

    private static string GetSnapcastClientId(string zoneName) =>
        zoneName switch
        {
            "ground-floor" => "living-room-client", // Client 1 maps to Ground Floor
            "first-floor" => "kitchen-client", // Client 2 maps to 1st Floor
            _ => throw new ArgumentException($"Unknown zone: {zoneName}"),
        };

    private static string GetVolumeStatusGroupAddress(string zoneName) =>
        zoneName switch
        {
            "ground-floor" => "1/2/2", // Zone 1 Volume Status
            "first-floor" => "2/2/2", // Zone 2 Volume Status
            _ => throw new ArgumentException($"Unknown zone: {zoneName}"),
        };

    #endregion
}

#region Supporting Types

public record ZoneVolumeState(int Volume, bool Muted);

#endregion
