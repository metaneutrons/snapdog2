using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;

namespace SnapDog2.Core.State;

/// <summary>
/// Example usage of the StateManager for demonstration and testing purposes.
/// </summary>
public static class StateManagerExample
{
    /// <summary>
    /// Demonstrates basic state management operations.
    /// </summary>
    /// <param name="logger">Logger instance for the StateManager.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task DemonstrateStateManagement(ILogger<StateManager> logger)
    {
        // Create a state manager with initial state
        using var stateManager = StateManagerFactory.Create(logger);

        // Subscribe to state change events
        stateManager.StateUpdated += (sender, args) =>
        {
            logger.LogInformation(
                "State updated from version {PreviousVersion} to {NewVersion}",
                args.PreviousState.Version,
                args.NewState.Version
            );
        };

        stateManager.StateValidationFailed += (sender, args) =>
        {
            logger.LogError("State validation failed: {ErrorMessage}", args.ErrorMessage);
        };

        // Set system status to starting
        stateManager.UpdateState(state => state.WithSystemStatus(SystemStatus.Starting));

        // Add some sample entities
        await AddSampleEntities(stateManager, logger);

        // Demonstrate queries
        DemonstrateQueries(stateManager, logger);

        // Set system status to running
        stateManager.UpdateState(state => state.WithSystemStatus(SystemStatus.Running));

        // Get final summary
        var currentState = stateManager.GetCurrentState();
        var summary = currentState.GetSummary();

        logger.LogInformation("Final state summary: {Summary}", summary);
    }

    /// <summary>
    /// Adds sample entities to demonstrate state operations.
    /// </summary>
    /// <param name="stateManager">The state manager instance.</param>
    /// <param name="logger">Logger instance.</param>
    private static async Task AddSampleEntities(IStateManager stateManager, ILogger logger)
    {
        // Add audio stream
        var audioStream = AudioStream.Create(
            id: "stream-1",
            name: "Classical Music Stream",
            url: new StreamUrl("http://example.com/classical"),
            codec: AudioCodec.MP3,
            bitrateKbps: 320,
            status: StreamStatus.Stopped
        );

        stateManager.UpdateState(state => state.WithAudioStream(audioStream));

        // Add clients
        var client1 = Client.Create(
            id: "client-1",
            name: "Living Room Speaker",
            macAddress: new MacAddress("00:11:22:33:44:55"),
            ipAddress: new IpAddress("192.168.1.100"),
            status: ClientStatus.Connected,
            volume: 75
        );

        var client2 = Client.Create(
            id: "client-2",
            name: "Kitchen Speaker",
            macAddress: new MacAddress("00:11:22:33:44:56"),
            ipAddress: new IpAddress("192.168.1.101"),
            status: ClientStatus.Connected,
            volume: 50
        );

        stateManager.UpdateState(state => state.WithClient(client1).WithClient(client2));

        // Add zone and assign clients
        var zone = Zone.Create("zone-1", "Main Floor", "Multi-room audio zone");
        zone = zone.WithAddedClient("client-1").WithAddedClient("client-2");

        stateManager.UpdateState(state => state.WithZone(zone));

        // Update clients with zone assignment
        var updatedClient1 = client1.WithZone("zone-1");
        var updatedClient2 = client2.WithZone("zone-1");

        stateManager.UpdateState(state => state.WithClient(updatedClient1).WithClient(updatedClient2));

        // Add radio station
        var radioStation = RadioStation.Create(
            id: "radio-1",
            name: "BBC Radio 4",
            url: new StreamUrl("http://stream.live.vc.bbcmedia.co.uk/bbc_radio_fourfm"),
            codec: AudioCodec.AAC,
            description: "BBC Radio 4 Live Stream"
        );

        stateManager.UpdateState(state => state.WithRadioStation(radioStation));

        // Add sample tracks
        var track1 = Track.Create("track-1", "Symphony No. 9", "Ludwig van Beethoven", "Classical Collection");
        var track2 = Track.Create("track-2", "Canon in D", "Johann Pachelbel", "Classical Collection");

        stateManager.UpdateState(state => state.WithTrack(track1).WithTrack(track2));

        // Add playlist with tracks
        var playlist = Playlist.Create("playlist-1", "Classical Favorites", "Best classical music tracks");
        playlist = playlist.WithAddedTrack("track-1").WithAddedTrack("track-2");

        stateManager.UpdateState(state => state.WithPlaylist(playlist));

        logger.LogInformation("Added sample entities to state");

        // Simulate async operation
        await Task.Delay(100);
    }

    /// <summary>
    /// Demonstrates various state queries using extensions.
    /// </summary>
    /// <param name="stateManager">The state manager instance.</param>
    /// <param name="logger">Logger instance.</param>
    private static void DemonstrateQueries(IStateManager stateManager, ILogger logger)
    {
        var currentState = stateManager.GetCurrentState();

        // Query individual entities
        var zone = currentState.GetZone("zone-1");
        logger.LogInformation("Found zone: {ZoneName} with {ClientCount} clients", zone?.Name, zone?.ClientCount);

        // Query related entities
        var clientsInZone = currentState.GetClientsInZone("zone-1").ToList();
        logger.LogInformation(
            "Clients in zone: {ClientNames}",
            string.Join(", ", clientsInZone.Select(static c => c.Name))
        );

        var tracksInPlaylist = currentState.GetTracksInPlaylist("playlist-1").ToList();
        logger.LogInformation(
            "Tracks in playlist: {TrackTitles}",
            string.Join(", ", tracksInPlaylist.Select(static t => t.Title))
        );

        // Query by status
        var connectedClients = currentState.GetConnectedClients().ToList();
        logger.LogInformation("Connected clients: {Count}", connectedClients.Count);

        var activeZones = currentState.GetActiveZones().ToList();
        logger.LogInformation("Active zones: {Count}", activeZones.Count);

        // Validation
        var isValid = currentState.IsValid();
        logger.LogInformation("State is valid: {IsValid}", isValid);

        // Summary
        var summary = currentState.GetSummary();
        logger.LogInformation(
            "State summary - Clients: {ConnectedClients}/{TotalClients}, "
                + "Zones: {ActiveZones}/{TotalZones}, "
                + "Status: {SystemStatus}",
            summary.ConnectedClients,
            summary.TotalClients,
            summary.ActiveZones,
            summary.TotalZones,
            summary.SystemStatus
        );
    }
}
