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
    public bool HasApi => Protocols.HasFlag(Protocol.Api);
    public bool HasMqtt => Protocols.HasFlag(Protocol.Mqtt);
    public bool HasKnx => Protocols.HasFlag(Protocol.Knx);
    public bool IsRequired => !IsOptional;

    // Exclusion checks
    public bool IsExcludedFrom(Protocol protocol) => Exclusions.Any(e => e.Protocol == protocol);

    public string? GetExclusionReason(Protocol protocol) =>
        Exclusions.FirstOrDefault(e => e.Protocol == protocol)?.Reason;
}

/// <summary>
/// Command specification.
/// </summary>
public record CommandSpec(string Id) : FeatureSpec(Id)
{
    public string? HttpMethod { get; init; }
    public string? ApiPath { get; init; }
    public string? MqttTopic { get; init; }
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
    public Type? ApiReturnType { get; init; }
}

/// <summary>
/// Collection of features with query methods.
/// </summary>
public class FeatureCollection<T> : IEnumerable<T>
    where T : FeatureSpec
{
    private readonly List<T> _features;

    public FeatureCollection(IEnumerable<T> features)
    {
        _features = features.ToList();
    }

    // Protocol filters
    public FeatureCollection<T> WithApi() => Filter(f => f.HasApi && !f.IsExcludedFrom(Protocol.Api));

    public FeatureCollection<T> WithMqtt() => Filter(f => f.HasMqtt && !f.IsExcludedFrom(Protocol.Mqtt));

    public FeatureCollection<T> WithKnx() => Filter(f => f.HasKnx && !f.IsExcludedFrom(Protocol.Knx));

    public FeatureCollection<T> WithoutApi() => Filter(f => !f.HasApi);

    public FeatureCollection<T> WithoutMqtt() => Filter(f => !f.HasMqtt);

    public FeatureCollection<T> WithoutKnx() => Filter(f => !f.HasKnx);

    // Category filters
    public FeatureCollection<T> Zone() => Filter(f => f.Category == FeatureCategory.Zone);

    public FeatureCollection<T> Client() => Filter(f => f.Category == FeatureCategory.Client);

    public FeatureCollection<T> Global() => Filter(f => f.Category == FeatureCategory.Global);

    // Requirement filters
    public FeatureCollection<T> Required() => Filter(f => f.IsRequired);

    public FeatureCollection<T> Optional() => Filter(f => f.IsOptional);

    public FeatureCollection<T> RecentlyAdded() => Filter(f => f.IsRecentlyAdded);

    // Exclusion filters
    public FeatureCollection<T> ExcludedFrom(Protocol protocol) => Filter(f => f.IsExcludedFrom(protocol));

    // HTTP method filters (for commands/status with API endpoints)
    public FeatureCollection<T> WithMethod(string method) =>
        Filter(f =>
            f switch
            {
                CommandSpec c => c.HttpMethod == method,
                StatusSpec s => s.HttpMethod == method,
                _ => false,
            }
        );

    // Utility methods
    private FeatureCollection<T> Filter(Func<T, bool> predicate) => new(_features.Where(predicate));

    public IEnumerator<T> GetEnumerator() => _features.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
