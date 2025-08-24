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
namespace SnapDog2.Tests.Unit.Core.Models;

using FluentAssertions;
using SnapDog2.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for TrackInfo model to verify the new structure without Id field.
/// </summary>
public class TrackInfoTests
{
    [Fact]
    public void TrackInfo_Should_HaveRequiredFields()
    {
        // Arrange & Act
        var trackInfo = new TrackInfo
        {
            Index = 1,
            Title = "Test Song",
            Artist = "Test Artist",
            Source = "radio",
            Url = "https://example.com/stream.mp3",
        };

        // Assert
        trackInfo.Index.Should().Be(1);
        trackInfo.Title.Should().Be("Test Song");
        trackInfo.Artist.Should().Be("Test Artist");
        trackInfo.Source.Should().Be("radio");
        trackInfo.Url.Should().Be("https://example.com/stream.mp3");
    }

    [Fact]
    public void TrackInfo_Should_NotHaveIdField()
    {
        // Arrange
        var trackInfoType = typeof(TrackInfo);

        // Act
        var idProperty = trackInfoType.GetProperty("Id");

        // Assert
        idProperty.Should().BeNull("TrackInfo should not have an Id property");
    }

    [Fact]
    public void TrackInfo_Should_HaveUrlField()
    {
        // Arrange
        var trackInfoType = typeof(TrackInfo);

        // Act
        var urlProperty = trackInfoType.GetProperty("Url");

        // Assert
        urlProperty.Should().NotBeNull("TrackInfo should have a Url property");
        urlProperty!.PropertyType.Should().Be<string>("Url should be a string");
    }

    [Fact]
    public void TrackInfo_RadioStation_Should_UseUrlForPlayback()
    {
        // Arrange
        const string streamUrl = "https://st02.sslstream.dlf.de/dlf/02/high/aac/stream.aac";

        var radioTrack = new TrackInfo
        {
            Index = 1,
            Title = "DLF Kultur",
            Artist = "Radio",
            Album = "Radio Stations",
            Source = "radio",
            Url = streamUrl,
        };

        // Assert
        radioTrack.Url.Should().Be(streamUrl);
        radioTrack.Source.Should().Be("radio");
        radioTrack.Index.Should().Be(1);
    }

    [Fact]
    public void TrackInfo_WithOptionalFields_Should_WorkCorrectly()
    {
        // Arrange & Act
        var trackInfo = new TrackInfo
        {
            Index = 2,
            Title = "Complex Song",
            Artist = "Test Artist",
            Source = "subsonic",
            Url = "subsonic://track/123",
            Album = "Test Album",
            DurationMs = 240000, // 4 minutes
            Genre = "Rock",
            Year = 2023,
            TrackNumber = 5,
            Rating = 0.8f,
        };

        // Assert
        trackInfo.Index.Should().Be(2);
        trackInfo.Album.Should().Be("Test Album");
        trackInfo.DurationMs.Should().Be(240000);
        trackInfo.Genre.Should().Be("Rock");
        trackInfo.Year.Should().Be(2023);
        trackInfo.TrackNumber.Should().Be(5);
        trackInfo.Rating.Should().Be(0.8f);
    }
}
