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
namespace SnapDog2.Server.Zones.Notifications;

using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Models;

/// <summary>
/// Notification published when track playback starts in a zone.
/// </summary>
/// <param name="ZoneIndex">The zone where playback started</param>
/// <param name="Track">The track that started playing</param>
public record TrackPlaybackStartedNotification(int ZoneIndex, TrackInfo Track) : INotification;

/// <summary>
/// Notification published when track playback stops in a zone.
/// </summary>
/// <param name="ZoneIndex">The zone where playback stopped</param>
public record TrackPlaybackStoppedNotification(int ZoneIndex) : INotification;

/// <summary>
/// Notification published when track playback is paused in a zone.
/// </summary>
/// <param name="ZoneIndex">The zone where playback was paused</param>
public record TrackPlaybackPausedNotification(int ZoneIndex) : INotification;

/// <summary>
/// Notification published when a track ends naturally (not stopped by user).
/// </summary>
/// <param name="ZoneIndex">The zone where the track ended</param>
/// <param name="Track">The track that ended</param>
public record TrackEndedNotification(int ZoneIndex, TrackInfo Track) : INotification;

/// <summary>
/// Notification published when a playback error occurs.
/// </summary>
/// <param name="ZoneIndex">The zone where the error occurred</param>
/// <param name="Track">The track that was playing when the error occurred</param>
/// <param name="Error">The error that occurred</param>
public record PlaybackErrorNotification(int ZoneIndex, TrackInfo? Track, string Error) : INotification;

/// <summary>
/// Notification published when streaming buffer underrun occurs.
/// </summary>
/// <param name="ZoneIndex">The zone where the underrun occurred</param>
/// <param name="Track">The track that was playing</param>
public record StreamingBufferUnderrunNotification(int ZoneIndex, TrackInfo Track) : INotification;

/// <summary>
/// Notification published when streaming connection is lost.
/// </summary>
/// <param name="ZoneIndex">The zone where the connection was lost</param>
/// <param name="Track">The track that was playing</param>
/// <param name="Reason">The reason for connection loss</param>
public record StreamingConnectionLostNotification(int ZoneIndex, TrackInfo Track, string Reason) : INotification;
