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
/// Base builder for features with common fluent methods.
/// </summary>
public abstract class FeatureBuilder<TBuilder>
    where TBuilder : FeatureBuilder<TBuilder>
{
    protected readonly BlueprintBuilder _blueprintBuilder;
    protected readonly string _id;
    protected FeatureCategory _category;
    protected Protocol _protocols = Protocol.None;
    protected string? _description;
    protected bool _isOptional;
    protected bool _isRecentlyAdded;
    protected readonly List<ProtocolExclusion> _exclusions = new();
    protected string? _httpMethod;
    protected string? _apiPath;
    protected string? _mqttTopic;
    protected Type? _apiReturnType;

    protected FeatureBuilder(BlueprintBuilder blueprintBuilder, string id)
    {
        _blueprintBuilder = blueprintBuilder;
        _id = id;
    }

    // Category methods
    public TBuilder Zone()
    {
        _category = FeatureCategory.Zone;
        return (TBuilder)this;
    }

    public TBuilder Client()
    {
        _category = FeatureCategory.Client;
        return (TBuilder)this;
    }

    public TBuilder Global()
    {
        _category = FeatureCategory.Global;
        return (TBuilder)this;
    }

    public TBuilder Media()
    {
        _category = FeatureCategory.Media;
        return (TBuilder)this;
    }

    // Protocol methods
    public TBuilder Api()
    {
        _protocols |= Protocol.Api;
        return (TBuilder)this;
    }

    public TBuilder Mqtt()
    {
        _protocols |= Protocol.Mqtt;
        return (TBuilder)this;
    }

    public TBuilder Mqtt(string topicPattern)
    {
        _protocols |= Protocol.Mqtt;
        _mqttTopic = topicPattern;
        return (TBuilder)this;
    }

    public TBuilder Knx()
    {
        _protocols |= Protocol.Knx;
        return (TBuilder)this;
    }

    // HTTP method methods
    public TBuilder Get(string path)
    {
        _httpMethod = "GET";
        _apiPath = path;
        return Api();
    }

    public TBuilder Post(string path)
    {
        _httpMethod = "POST";
        _apiPath = path;
        return Api();
    }

    public TBuilder Put(string path)
    {
        _httpMethod = "PUT";
        _apiPath = path;
        return Api();
    }

    public TBuilder Delete(string path)
    {
        _httpMethod = "DELETE";
        _apiPath = path;
        return Api();
    }

    // API return type method
    public TBuilder ApiReturns<T>()
    {
        _apiReturnType = typeof(T);
        return (TBuilder)this;
    }

    // Documentation methods
    public TBuilder Description(string description)
    {
        _description = description;
        return (TBuilder)this;
    }

    // Modifier methods
    public TBuilder Exclude(Protocol protocol, string reason)
    {
        _exclusions.Add(new ProtocolExclusion(protocol, reason));
        return (TBuilder)this;
    }

    public TBuilder RecentlyAdded()
    {
        _isRecentlyAdded = true;
        return (TBuilder)this;
    }

    public TBuilder Optional()
    {
        _isOptional = true;
        return (TBuilder)this;
    }

    protected abstract void BuildFeature();
}

/// <summary>
/// Builder for command specifications.
/// </summary>
public class CommandBuilder : FeatureBuilder<CommandBuilder>
{
    public CommandBuilder(BlueprintBuilder blueprintBuilder, string id)
        : base(blueprintBuilder, id) { }

    // Fluent continuation methods
    public CommandBuilder Command(string id)
    {
        BuildFeature();
        return _blueprintBuilder.Command(id);
    }

    public StatusBuilder Status(string id)
    {
        BuildFeature();
        return _blueprintBuilder.Status(id);
    }

    public Blueprint Build()
    {
        BuildFeature();
        return _blueprintBuilder.Build();
    }

    protected override void BuildFeature()
    {
        var command = new CommandSpec(_id)
        {
            Category = _category,
            Protocols = _protocols,
            Description = _description,
            IsOptional = _isOptional,
            IsRecentlyAdded = _isRecentlyAdded,
            Exclusions = _exclusions,
            HttpMethod = _httpMethod,
            ApiPath = _apiPath,
            MqttTopic = _mqttTopic,
            ApiReturnType = _apiReturnType,
        };

        _blueprintBuilder.AddCommand(command);
    }
}

/// <summary>
/// Builder for status specifications.
/// </summary>
public class StatusBuilder : FeatureBuilder<StatusBuilder>
{
    public StatusBuilder(BlueprintBuilder blueprintBuilder, string id)
        : base(blueprintBuilder, id) { }

    // Fluent continuation methods
    public CommandBuilder Command(string id)
    {
        BuildFeature();
        return _blueprintBuilder.Command(id);
    }

    public StatusBuilder Status(string id)
    {
        BuildFeature();
        return _blueprintBuilder.Status(id);
    }

    public Blueprint Build()
    {
        BuildFeature();
        return _blueprintBuilder.Build();
    }

    protected override void BuildFeature()
    {
        var status = new StatusSpec(_id)
        {
            Category = _category,
            Protocols = _protocols,
            Description = _description,
            IsOptional = _isOptional,
            IsRecentlyAdded = _isRecentlyAdded,
            Exclusions = _exclusions,
            HttpMethod = _httpMethod,
            ApiPath = _apiPath,
            MqttTopic = _mqttTopic,
            ApiReturnType = _apiReturnType,
        };

        _blueprintBuilder.AddStatus(status);
    }
}
