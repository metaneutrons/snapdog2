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
using SnapDog2.Core.Enums;
using SnapDog2.Core.Mappers;
using SnapDog2.Core.Models;
using SnapDog2.Core.Models.Mqtt;
using Xunit;

namespace SnapDog2.Tests.Core.Mappers;

public class MqttStateMapperTests
{
    [Fact]
    public void ToMqttZoneState_ShouldMapAllProperties_WhenZoneStateIsComplete()
    {
        // Arrange
        var zoneState = new ZoneState
        {
            Name = "Living Room",
            PlaybackState = PlaybackState.Playing,
            Volume = 75,
            Mute = false,
            TrackRepeat = true,
            PlaylistRepeat = false,
            PlaylistShuffle = true,
            SnapcastGroupId = "group-123",
            SnapcastStreamId = "/stream/1",
            Clients = [1, 2],
            Playlist = new PlaylistInfo
            {
                Name = "Test Playlist",
                Index = 2,
                TrackCount = 10,
                TotalDurationSec = 3600,
                CoverArtUrl = "cover.jpg",
                Source = "subsonic"
            },
            Track = new TrackInfo
            {
                Index = 3,
                Title = "Test Track",
                Artist = "Test Artist",
                Album = "Test Album",
                CoverArtUrl = "track-cover.jpg",
                Source = "subsonic",
                Url = "http://example.com/track.mp3"
            }
        };

        // Act
        var mqttState = MqttStateMapper.ToMqttZoneState(zoneState);

        // Assert
        Assert.Equal("Living Room", mqttState.Name);
        Assert.True(mqttState.PlaybackState); // Playing = true
        Assert.Equal(75, mqttState.Volume);
        Assert.False(mqttState.Mute);
        Assert.True(mqttState.RepeatTrack);
        Assert.False(mqttState.RepeatPlaylist);
        Assert.True(mqttState.Shuffle);

        // Verify playlist mapping
        Assert.NotNull(mqttState.Playlist);
        Assert.Equal(2, mqttState.Playlist.Index);
        Assert.Equal("Test Playlist", mqttState.Playlist.Name);
        Assert.Equal(10, mqttState.Playlist.TrackCount);
        Assert.Equal(3600, mqttState.Playlist.TotalDurationSec);
        Assert.Equal("cover.jpg", mqttState.Playlist.CoverArtUrl);
        Assert.Equal("subsonic", mqttState.Playlist.Source);

        // Verify track mapping
        Assert.NotNull(mqttState.Track);
        Assert.Equal(3, mqttState.Track.Index);
        Assert.Equal("Test Track", mqttState.Track.Title);
        Assert.Equal("Test Artist", mqttState.Track.Artist);
        Assert.Equal("Test Album", mqttState.Track.Album);
        Assert.Equal("track-cover.jpg", mqttState.Track.CoverArtUrl);
        Assert.Equal("subsonic", mqttState.Track.Source);
    }

    [Fact]
    public void ToMqttZoneState_ShouldMapPlaybackStateCorrectly_WhenPaused()
    {
        // Arrange
        var zoneState = new ZoneState
        {
            Name = "Test Zone",
            PlaybackState = PlaybackState.Paused,
            Volume = 50,
            Mute = false,
            TrackRepeat = false,
            PlaylistRepeat = false,
            PlaylistShuffle = false,
            SnapcastGroupId = "group-123",
            SnapcastStreamId = "/stream/1",
            Clients = []
        };

        // Act
        var mqttState = MqttStateMapper.ToMqttZoneState(zoneState);

        // Assert
        Assert.False(mqttState.PlaybackState); // Paused = false
    }

    [Fact]
    public void ToMqttZoneState_ShouldHandleNullablePlaylistValues()
    {
        // Arrange
        var zoneState = new ZoneState
        {
            Name = "Test Zone",
            PlaybackState = PlaybackState.Stopped,
            Volume = 50,
            Mute = false,
            TrackRepeat = false,
            PlaylistRepeat = false,
            PlaylistShuffle = false,
            SnapcastGroupId = "group-123",
            SnapcastStreamId = "/stream/1",
            Clients = [],
            Playlist = new PlaylistInfo
            {
                Name = "Radio Playlist",
                Index = null, // Nullable
                TrackCount = 5,
                TotalDurationSec = null, // Nullable
                Source = "radio"
            }
        };

        // Act
        var mqttState = MqttStateMapper.ToMqttZoneState(zoneState);

        // Assert
        Assert.NotNull(mqttState.Playlist);
        Assert.Equal(0, mqttState.Playlist.Index); // null -> 0
        Assert.Equal(0, mqttState.Playlist.TotalDurationSec); // null -> 0
    }

    [Fact]
    public void HasMeaningfulChange_ShouldReturnTrue_WhenPreviousIsNull()
    {
        // Arrange
        var current = new MqttZoneState
        {
            Name = "Test",
            PlaybackState = true,
            Volume = 50,
            Mute = false,
            RepeatTrack = false,
            RepeatPlaylist = false,
            Shuffle = false,
            Playlist = null,
            Track = null
        };

        // Act
        var hasChange = MqttStateMapper.HasMeaningfulChange(null, current);

        // Assert
        Assert.True(hasChange);
    }

    [Fact]
    public void HasMeaningfulChange_ShouldReturnFalse_WhenStatesAreIdentical()
    {
        // Arrange
        var state = new MqttZoneState
        {
            Name = "Test",
            PlaybackState = true,
            Volume = 50,
            Mute = false,
            RepeatTrack = false,
            RepeatPlaylist = false,
            Shuffle = false,
            Playlist = null,
            Track = null
        };

        // Act
        var hasChange = MqttStateMapper.HasMeaningfulChange(state, state);

        // Assert
        Assert.False(hasChange);
    }

    [Fact]
    public void HasMeaningfulChange_ShouldReturnTrue_WhenVolumeChanges()
    {
        // Arrange
        var previous = new MqttZoneState
        {
            Name = "Test",
            PlaybackState = true,
            Volume = 50,
            Mute = false,
            RepeatTrack = false,
            RepeatPlaylist = false,
            Shuffle = false,
            Playlist = null,
            Track = null
        };

        var current = previous with { Volume = 75 };

        // Act
        var hasChange = MqttStateMapper.HasMeaningfulChange(previous, current);

        // Assert
        Assert.True(hasChange);
    }

    [Fact]
    public void HasMeaningfulChange_ShouldReturnTrue_WhenTrackChanges()
    {
        // Arrange
        var track1 = new MqttTrackInfo
        {
            Index = 1,
            Title = "Track 1",
            Artist = "Artist",
            Album = "Album",
            Source = "subsonic"
        };

        var track2 = new MqttTrackInfo
        {
            Index = 2,
            Title = "Track 2",
            Artist = "Artist",
            Album = "Album",
            Source = "subsonic"
        };

        var previous = new MqttZoneState
        {
            Name = "Test",
            PlaybackState = true,
            Volume = 50,
            Mute = false,
            RepeatTrack = false,
            RepeatPlaylist = false,
            Shuffle = false,
            Playlist = null,
            Track = track1
        };

        var current = previous with { Track = track2 };

        // Act
        var hasChange = MqttStateMapper.HasMeaningfulChange(previous, current);

        // Assert
        Assert.True(hasChange);
    }
}
