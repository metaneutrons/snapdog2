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
namespace SnapDog2.Tests.Blueprint;

using System.Collections;

/// <summary>
/// Base class for all feature specifications.
/// </summary>
public abstract record FeatureSpec(string Id)
{
    public required FeatureCategory Category { get; init; }
    public required Protocol Protocols { get; init; }
    public string? Description { get; init; }
    public bool IsOptional { get; init; }
    public bool IsRecentlyAdded { get; init; }
    public List<ProtocolExclusion> Exclusions { get; init; } = new();

    // Protocol checks
    public bool HasApi => this.Protocols.HasFlag(Protocol.Api);
    public bool HasMqtt => this.Protocols.HasFlag(Protocol.Mqtt);
    public bool HasKnx => this.Protocols.HasFlag(Protocol.Knx);
    public bool HasSignalR => this.Protocols.HasFlag(Protocol.SignalR);
    public bool IsRequired => !this.IsOptional;

    // Exclusion checks
    public bool IsExcludedFrom(Protocol protocol) => this.Exclusions.Any(e => e.Protocol == protocol);

    public string? GetExclusionReason(Protocol protocol) => this.Exclusions.FirstOrDefault(e => e.Protocol == protocol)?.Reason;
}

/// <summary>
/// Command specification.
/// </summary>
public record CommandSpec(string Id) : FeatureSpec(Id)
{
    public string? HttpMethod { get; init; }
    public string? ApiPath { get; init; }
    public string? MqttTopic { get; init; }
    public string? SignalREvent { get; init; }
    public Type? ApiReturnType { get; init; }
}

/// <summary>
/// Status specification.
/// </summary>
public record StatusSpec(string Id) : FeatureSpec(Id)
{
    public string? HttpMethod { get; init; }
    public string? ApiPath { get; init; }
    public string? MqttTopic { get; init; }
    public string? SignalREvent { get; init; }
    public Type? ApiReturnType { get; init; }
}

/// <summary>
/// Collection of features with query methods.
/// </summary>
public class FeatureCollection<T>(IEnumerable<T> features) : IEnumerable<T>
    where T : FeatureSpec
{
    private readonly List<T> _features = features.ToList();

    // Protocol filters
    public FeatureCollection<T> WithApi() => this.Filter(f => f.HasApi && !f.IsExcludedFrom(Protocol.Api));

    public FeatureCollection<T> WithMqtt() => this.Filter(f => f.HasMqtt && !f.IsExcludedFrom(Protocol.Mqtt));

    public FeatureCollection<T> WithKnx() => this.Filter(f => f.HasKnx && !f.IsExcludedFrom(Protocol.Knx));

    public FeatureCollection<T> WithSignalR() => this.Filter(f => f.HasSignalR && !f.IsExcludedFrom(Protocol.SignalR));

    public FeatureCollection<T> WithoutApi() => this.Filter(f => !f.HasApi);

    public FeatureCollection<T> WithoutMqtt() => this.Filter(f => !f.HasMqtt);

    public FeatureCollection<T> WithoutKnx() => this.Filter(f => !f.HasKnx);

    public FeatureCollection<T> WithoutSignalR() => this.Filter(f => !f.HasSignalR);

    // Category filters
    public FeatureCollection<T> Zone() => this.Filter(f => f.Category == FeatureCategory.Zone);

    public FeatureCollection<T> Client() => this.Filter(f => f.Category == FeatureCategory.Client);

    public FeatureCollection<T> Global() => this.Filter(f => f.Category == FeatureCategory.Global);

    // Requirement filters
    public FeatureCollection<T> Required() => this.Filter(f => f.IsRequired);

    public FeatureCollection<T> Optional() => this.Filter(f => f.IsOptional);

    public FeatureCollection<T> RecentlyAdded() => this.Filter(f => f.IsRecentlyAdded);

    // Exclusion filters
    public FeatureCollection<T> ExcludedFrom(Protocol protocol) => this.Filter(f => f.IsExcludedFrom(protocol));

    // HTTP method filters (for commands/status with API endpoints)
    public FeatureCollection<T> WithMethod(string method) =>
        this.Filter(f =>
            f switch
            {
                CommandSpec c => c.HttpMethod == method,
                StatusSpec s => s.HttpMethod == method,
                _ => false,
            }
        );

    // Utility methods
    private FeatureCollection<T> Filter(Func<T, bool> predicate) => new(this._features.Where(predicate));

    public IEnumerator<T> GetEnumerator() => this._features.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
