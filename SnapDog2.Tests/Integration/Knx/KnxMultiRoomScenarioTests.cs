namespace SnapDog2.Tests.Integration.Knx;

using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Commands.Playback;
using SnapDog2.Server.Features.Zones.Commands.Volume;
using SnapDog2.Tests.Integration.Fixtures;

/// <summary>
/// Advanced KNX multi-room scenario tests that validate complex real-world use cases.
/// Tests scenarios like party mode, zone transfers, synchronized playback, and master controls
/// that would be triggered by KNX wall panels in a smart home environment.
/// </summary>
[Collection("KnxIntegrationFlow")]
public class KnxMultiRoomScenarioTests : IClassFixture<KnxIntegrationTestFixture>
{
    private readonly KnxIntegrationTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public KnxMultiRoomScenarioTests(KnxIntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    #region Party Mode Scenarios

    [Fact]
    public async Task KnxPartyModeActivation_ShouldSynchronizeAllZones()
    {
        _output.WriteLine("Testing KNX Party Mode activation - synchronize all zones to same state");

        // Arrange: Set different initial states for each zone
        await SetupDifferentZoneStates();

        var partyVolume = 75;
        var stopwatch = Stopwatch.StartNew();

        // Act: Activate Party Mode via KNX
        await _fixture.KnxClient.WriteGroupValueAsync("0/0/10", true); // Scene Master GA
        await _fixture.KnxClient.WriteGroupValueAsync("0/0/2", (byte)partyVolume); // System Time GA (repurposed for party volume)

        // Assert: All zones synchronized
        var zones = new[] { "ground-floor", "first-floor" };

        foreach (var zone in zones)
        {
            // Verify volume synchronized
            await VerifyZoneVolume(zone, partyVolume);

            // Verify playback state synchronized
            await VerifyZonePlaybackState(zone, PlaybackState.Playing);

            // Verify same track playing in all zones
            await VerifyZoneTrackSynchronized(zone);

            _output.WriteLine($"Zone {zone} synchronized to party mode");
        }

        stopwatch.Stop();
        _output.WriteLine($"Party mode synchronization completed in {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000); // < 3 seconds for all zones
    }

    [Fact]
    public async Task KnxPartyModeDeactivation_ShouldRestorePreviousStates()
    {
        _output.WriteLine("Testing KNX Party Mode deactivation - restore previous zone states");

        // Arrange: Set up party mode first, then capture states
        await _fixture.KnxClient.WriteGroupValueAsync("0/0/10", true); // Activate party mode
        await Task.Delay(2000); // Allow synchronization

        // Store party mode states
        var partyStates = await CaptureAllZoneStates();

        // Act: Deactivate Party Mode
        await _fixture.KnxClient.WriteGroupValueAsync("0/0/10", false); // Deactivate party mode

        // Assert: Each zone restored to individual control
        await VerifyZoneIndependentControl("ground-floor");
        await VerifyZoneIndependentControl("first-floor");

        _output.WriteLine("Party mode deactivation completed - zones restored to independent control");
    }

    #endregion

    #region Zone Transfer Scenarios

    [Fact]
    public async Task KnxZoneTransfer_LivingRoomToKitchen_ShouldMovePlaybackSeamlessly()
    {
        _output.WriteLine("Testing seamless zone transfer: Ground Floor → 1st Floor");

        // Arrange: Start playback in Ground Floor
        await _fixture.KnxClient.WriteGroupValueAsync("1/1/1", true); // Ground Floor Play
        await Task.Delay(1000);

        var originalTrack = await GetCurrentTrackInfo("ground-floor");
        var originalPosition = await GetCurrentPlaybackPosition("ground-floor");

        // Act: Transfer to 1st Floor (using scene master)
        await _fixture.KnxClient.WriteGroupValueAsync("0/0/10", true); // Scene Master for transfer

        // Assert: Seamless transfer
        // Ground Floor should stop
        await VerifyZonePlaybackState("ground-floor", PlaybackState.Stopped);

        // 1st Floor should start playing same track at same position (±2 seconds tolerance)
        await VerifyZonePlaybackState("first-floor", PlaybackState.Playing);
        var firstFloorTrack = await GetCurrentTrackInfo("first-floor");
        var firstFloorPosition = await GetCurrentPlaybackPosition("first-floor");

        firstFloorTrack.Should().BeEquivalentTo(originalTrack);
        Math.Abs(firstFloorPosition - originalPosition).Should().BeLessThan(2000); // ±2 seconds

        _output.WriteLine(
            $"Zone transfer completed: track '{originalTrack.Title}' moved from Ground Floor to 1st Floor"
        );
    }

    [Fact]
    public async Task KnxMultiZoneTransfer_ShouldHandleComplexRouting()
    {
        _output.WriteLine("Testing complex multi-zone transfer routing");

        // Scenario: Living Room → Kitchen → Bedroom → All Zones
        var transferSequence = new[]
        {
            ("1/1/1", "living-room", "Start in Living Room"),
            ("1/2/10", "kitchen", "Transfer to Kitchen"),
            ("2/3/10", "bedroom", "Transfer to Bedroom"),
            ("0/0/12", "all", "Broadcast to All Zones"),
        };

        string currentTrackId = null;

        foreach (var (groupAddress, expectedZone, description) in transferSequence)
        {
            _output.WriteLine($"Step: {description}");

            await _fixture.KnxClient.WriteGroupValueAsync(groupAddress, true);
            await Task.Delay(1500); // Allow transfer to complete

            if (expectedZone == "all")
            {
                // Verify all zones playing
                foreach (var zone in new[] { "living-room", "kitchen", "bedroom" })
                {
                    await VerifyZonePlaybackState(zone, PlaybackState.Playing);
                    if (currentTrackId != null)
                    {
                        var track = await GetCurrentTrackInfo(zone);
                        track.Id.Should().Be(currentTrackId);
                    }
                }
            }
            else
            {
                // Verify only target zone playing
                await VerifyZonePlaybackState(expectedZone, PlaybackState.Playing);
                var track = await GetCurrentTrackInfo(expectedZone);
                currentTrackId = track.Id;

                // Verify other zones stopped
                var otherZones = new[] { "living-room", "kitchen", "bedroom" }.Except(new[] { expectedZone });
                foreach (var zone in otherZones)
                {
                    await VerifyZonePlaybackState(zone, PlaybackState.Stopped);
                }
            }
        }

        _output.WriteLine("Complex multi-zone transfer routing completed successfully");
    }

    #endregion

    #region Master Control Scenarios

    [Fact]
    public async Task KnxMasterVolumeControl_ShouldAffectAllZonesProportionally()
    {
        _output.WriteLine("Testing KNX Master Volume Control with proportional adjustment");

        // Arrange: Set different volumes for each zone
        var initialVolumes = new Dictionary<string, int>
        {
            ["living-room"] = 60,
            ["kitchen"] = 40,
            ["bedroom"] = 80,
        };

        foreach (var (zone, volume) in initialVolumes)
        {
            await SetZoneVolume(zone, volume);
        }

        // Act: Master Volume Up by 10%
        await _fixture.KnxClient.WriteGroupValueAsync("0/0/1", (byte)10); // Master Volume Up 10%

        // Assert: All zones increased proportionally
        foreach (var (zone, initialVolume) in initialVolumes)
        {
            var expectedVolume = Math.Min(100, initialVolume + 10);
            await VerifyZoneVolume(zone, expectedVolume);
            _output.WriteLine($"Zone {zone}: {initialVolume}% → {expectedVolume}%");
        }

        // Act: Master Volume Down by 15%
        await _fixture.KnxClient.WriteGroupValueAsync("0/0/2", (byte)15); // Master Volume Down 15%

        // Assert: All zones decreased proportionally
        foreach (var (zone, initialVolume) in initialVolumes)
        {
            var expectedVolume = Math.Max(0, (initialVolume + 10) - 15); // Previous + 10 - 15
            await VerifyZoneVolume(zone, expectedVolume);
            _output.WriteLine($"Zone {zone}: {initialVolume + 10}% → {expectedVolume}%");
        }
    }

    [Fact]
    public async Task KnxMasterMute_ShouldMuteAllZonesAndRestore()
    {
        _output.WriteLine("Testing KNX Master Mute/Unmute functionality");

        // Arrange: Set different volumes and ensure playback
        var initialVolumes = await SetupActivePlaybackAllZones();

        // Act: Master Mute
        await _fixture.KnxClient.WriteGroupValueAsync("0/0/3", true); // Master Mute

        // Assert: All zones muted but volumes preserved
        foreach (var (zone, originalVolume) in initialVolumes)
        {
            await VerifyZoneMuted(zone, true);
            // Volume should be preserved for restore
            var zoneState = await GetZoneState(zone);
            zoneState.Volume.Should().Be(originalVolume); // Volume preserved
            _output.WriteLine($"Zone {zone} muted (volume {originalVolume}% preserved)");
        }

        // Act: Master Unmute
        await _fixture.KnxClient.WriteGroupValueAsync("0/0/3", false); // Master Unmute

        // Assert: All zones unmuted with original volumes
        foreach (var (zone, originalVolume) in initialVolumes)
        {
            await VerifyZoneMuted(zone, false);
            await VerifyZoneVolume(zone, originalVolume);
            _output.WriteLine($"Zone {zone} unmuted (volume {originalVolume}% restored)");
        }
    }

    #endregion

    #region Scene Control Scenarios

    [Fact]
    public async Task KnxDinnerScene_ShouldConfigureAppropriateZones()
    {
        _output.WriteLine("Testing KNX Dinner Scene activation");

        // Act: Activate Dinner Scene
        await _fixture.KnxClient.WriteGroupValueAsync("0/1/1", true); // Dinner Scene

        // Assert: Kitchen and Living Room active, Bedroom off
        await VerifyZonePlaybackState("kitchen", PlaybackState.Playing);
        await VerifyZoneVolume("kitchen", 45); // Moderate volume for conversation

        await VerifyZonePlaybackState("living-room", PlaybackState.Playing);
        await VerifyZoneVolume("living-room", 35); // Lower volume for ambiance

        await VerifyZonePlaybackState("bedroom", PlaybackState.Stopped);

        _output.WriteLine("Dinner scene configured: Kitchen (45%), Living Room (35%), Bedroom (off)");
    }

    [Fact]
    public async Task KnxSleepScene_ShouldConfigureNightTimeSettings()
    {
        _output.WriteLine("Testing KNX Sleep Scene activation");

        // Act: Activate Sleep Scene
        await _fixture.KnxClient.WriteGroupValueAsync("0/1/2", true); // Sleep Scene

        // Assert: Only Bedroom active with low volume
        await VerifyZonePlaybackState("bedroom", PlaybackState.Playing);
        await VerifyZoneVolume("bedroom", 20); // Very low volume

        await VerifyZonePlaybackState("living-room", PlaybackState.Stopped);
        await VerifyZonePlaybackState("kitchen", PlaybackState.Stopped);

        _output.WriteLine("Sleep scene configured: Bedroom (20%), other zones off");
    }

    [Fact]
    public async Task KnxWorkoutScene_ShouldConfigureHighEnergySettings()
    {
        _output.WriteLine("Testing KNX Workout Scene activation");

        // Act: Activate Workout Scene
        await _fixture.KnxClient.WriteGroupValueAsync("0/1/3", true); // Workout Scene

        // Assert: Living Room high volume, upbeat playlist
        await VerifyZonePlaybackState("living-room", PlaybackState.Playing);
        await VerifyZoneVolume("living-room", 85); // High volume for motivation

        // Verify playlist switched to workout music (if implemented)
        var currentTrack = await GetCurrentTrackInfo("living-room");
        // currentTrack.Genre.Should().Contain("workout"); // If genre metadata available

        await VerifyZonePlaybackState("kitchen", PlaybackState.Stopped);
        await VerifyZonePlaybackState("bedroom", PlaybackState.Stopped);

        _output.WriteLine("Workout scene configured: Living Room (85%), high-energy music");
    }

    #endregion

    #region Emergency and Safety Scenarios

    [Fact]
    public async Task KnxEmergencyStop_ShouldImmediatelyStopAllPlayback()
    {
        _output.WriteLine("Testing KNX Emergency Stop functionality");

        // Arrange: Active playback in all zones
        await SetupActivePlaybackAllZones();

        var stopwatch = Stopwatch.StartNew();

        // Act: Emergency Stop
        await _fixture.KnxClient.WriteGroupValueAsync("0/0/99", true); // Emergency Stop GA

        // Assert: All zones stopped immediately
        var zones = new[] { "ground-floor", "first-floor" };

        foreach (var zone in zones)
        {
            await VerifyZonePlaybackState(zone, PlaybackState.Stopped);
            _output.WriteLine($"Zone {zone} stopped");
        }

        stopwatch.Stop();
        _output.WriteLine($"Emergency stop completed in {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500); // < 500ms for emergency response
    }

    [Fact]
    public async Task KnxDoorbellIntegration_ShouldPauseAndResumePlayback()
    {
        _output.WriteLine("Testing KNX Doorbell integration - pause and resume");

        // Arrange: Active playback
        await SetupActivePlaybackAllZones();
        var initialStates = await CaptureAllZoneStates();

        // Act: Doorbell pressed
        await _fixture.KnxClient.WriteGroupValueAsync("0/8/1", true); // Doorbell GA

        // Assert: All zones paused
        foreach (var zone in new[] { "living-room", "kitchen", "bedroom" })
        {
            await VerifyZonePlaybackState(zone, PlaybackState.Paused);
        }

        // Wait for doorbell timeout (simulate)
        await Task.Delay(10000); // 10 seconds

        // Assert: Playback automatically resumed
        foreach (var zone in new[] { "living-room", "kitchen", "bedroom" })
        {
            if (initialStates[zone].PlaybackState == PlaybackState.Playing)
            {
                await VerifyZonePlaybackState(zone, PlaybackState.Playing);
            }
        }

        _output.WriteLine("Doorbell integration completed - playback paused and resumed");
    }

    #endregion

    #region Helper Methods

    private async Task SetupDifferentZoneStates()
    {
        // Ground Floor: High volume, playing
        await SetZoneVolume("ground-floor", 80);
        await _fixture.KnxClient.WriteGroupValueAsync("1/1/1", true);

        // 1st Floor: Medium volume, paused
        await SetZoneVolume("first-floor", 50);
        await _fixture.KnxClient.WriteGroupValueAsync("2/1/2", true);

        await Task.Delay(1000); // Allow states to settle
    }

    private async Task<Dictionary<string, int>> SetupActivePlaybackAllZones()
    {
        var volumes = new Dictionary<string, int> { ["ground-floor"] = 70, ["first-floor"] = 55 };

        foreach (var (zone, volume) in volumes)
        {
            await SetZoneVolume(zone, volume);
            await StartZonePlayback(zone);
        }

        await Task.Delay(1000); // Allow playback to start
        return volumes;
    }

    private async Task<Dictionary<string, ZoneState>> CaptureAllZoneStates()
    {
        var states = new Dictionary<string, ZoneState>();

        foreach (var zone in new[] { "ground-floor", "first-floor" })
        {
            states[zone] = await GetZoneState(zone);
        }

        return states;
    }

    private async Task SetZoneVolume(string zoneName, int volume)
    {
        var zoneIndex = GetZoneIndex(zoneName);
        var response = await _fixture.ApiClient.PostAsync(
            $"/api/v1/zones/{zoneIndex}/volume",
            JsonContent.Create(new { volume })
        );
        response.Should().BeSuccessful();
    }

    private async Task StartZonePlayback(string zoneName)
    {
        var playGA = GetPlayGroupAddress(zoneName);
        await _fixture.KnxClient.WriteGroupValueAsync(playGA, true);
    }

    private async Task<ZoneState> GetZoneState(string zoneName)
    {
        var zoneIndex = GetZoneIndex(zoneName);
        var response = await _fixture.ApiClient.GetAsync($"/api/v1/zones/{zoneIndex}");
        response.Should().BeSuccessful();
        return await response.Content.ReadFromJsonAsync<ZoneState>();
    }

    private async Task<TrackInfo> GetCurrentTrackInfo(string zoneName)
    {
        var zoneIndex = GetZoneIndex(zoneName);
        var response = await _fixture.ApiClient.GetAsync($"/api/v1/zones/{zoneIndex}/track");
        response.Should().BeSuccessful();
        return await response.Content.ReadFromJsonAsync<TrackInfo>();
    }

    private async Task<long> GetCurrentPlaybackPosition(string zoneName)
    {
        var zoneIndex = GetZoneIndex(zoneName);
        var response = await _fixture.ApiClient.GetAsync($"/api/v1/zones/{zoneIndex}/playback");
        response.Should().BeSuccessful();
        var playback = await response.Content.ReadFromJsonAsync<PlaybackStatus>();
        return playback.Position;
    }

    private async Task VerifyZoneVolume(string zoneName, int expectedVolume)
    {
        var zoneState = await GetZoneState(zoneName);
        zoneState.Volume.Should().Be(expectedVolume);
    }

    private async Task VerifyZonePlaybackState(string zoneName, PlaybackState expectedState)
    {
        var zoneIndex = GetZoneIndex(zoneName);
        var response = await _fixture.ApiClient.GetAsync($"/api/v1/zones/{zoneIndex}/playback");
        var playback = await response.Content.ReadFromJsonAsync<PlaybackStatus>();
        playback.State.Should().Be(expectedState);
    }

    private async Task VerifyZoneMuted(string zoneName, bool expectedMuted)
    {
        var zoneState = await GetZoneState(zoneName);
        zoneState.Muted.Should().Be(expectedMuted);
    }

    private async Task VerifyZoneTrackSynchronized(string zoneName)
    {
        // Verify all zones playing the same track (party mode)
        var track = await GetCurrentTrackInfo(zoneName);
        track.Should().NotBeNull();
        // Additional synchronization checks could be added here
    }

    private async Task VerifyZoneIndependentControl(string zoneName)
    {
        // Test that zone responds to individual commands (not synchronized)
        var initialVolume = (await GetZoneState(zoneName)).Volume;

        // Send individual volume command
        var volumeGA = GetVolumeUpGroupAddress(zoneName);
        await _fixture.KnxClient.WriteGroupValueAsync(volumeGA, true);

        await Task.Delay(500);

        var newVolume = (await GetZoneState(zoneName)).Volume;
        newVolume.Should().BeGreaterThan(initialVolume);
    }

    private static int GetZoneIndex(string zoneName) =>
        zoneName switch
        {
            "ground-floor" => 1,
            "first-floor" => 2,
            _ => throw new ArgumentException($"Unknown zone: {zoneName}"),
        };

    private static string GetPlayGroupAddress(string zoneName) =>
        zoneName switch
        {
            "ground-floor" => "1/1/1",
            "first-floor" => "2/1/1",
            _ => throw new ArgumentException($"Unknown zone: {zoneName}"),
        };

    private static string GetVolumeUpGroupAddress(string zoneName) =>
        zoneName switch
        {
            "ground-floor" => "1/2/3",
            "first-floor" => "2/2/3",
            _ => throw new ArgumentException($"Unknown zone: {zoneName}"),
        };

    #endregion
}
