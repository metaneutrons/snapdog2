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
public abstract class FeatureBuilder<TBuilder>(BlueprintBuilder blueprintBuilder, string id)
    where TBuilder : FeatureBuilder<TBuilder>
{
    protected readonly BlueprintBuilder _blueprintBuilder = blueprintBuilder;
    protected readonly string _id = id;
    protected FeatureCategory _category;
    protected Protocol _protocols = Protocol.None;
    protected string? _description;
    protected bool _isOptional;
    protected bool _isRecentlyAdded;
    protected readonly List<ProtocolExclusion> _exclusions = new();
    protected string? _httpMethod;
    protected string? _apiPath;
    protected string? _mqttTopic;
    protected string? _signalREvent;
    protected Type? _apiReturnType;

    // Category methods
    public TBuilder Zone()
    {
        this._category = FeatureCategory.Zone;
        return (TBuilder)this;
    }

    public TBuilder Client()
    {
        this._category = FeatureCategory.Client;
        return (TBuilder)this;
    }

    public TBuilder Global()
    {
        this._category = FeatureCategory.Global;
        return (TBuilder)this;
    }

    public TBuilder Media()
    {
        this._category = FeatureCategory.Media;
        return (TBuilder)this;
    }

    // Protocol methods
    public TBuilder Api()
    {
        this._protocols |= Protocol.Api;
        return (TBuilder)this;
    }

    public TBuilder Mqtt()
    {
        this._protocols |= Protocol.Mqtt;
        return (TBuilder)this;
    }

    public TBuilder Mqtt(string topicPattern)
    {
        this._protocols |= Protocol.Mqtt;
        this._mqttTopic = topicPattern;
        return (TBuilder)this;
    }

    public TBuilder Knx()
    {
        this._protocols |= Protocol.Knx;
        return (TBuilder)this;
    }

    public TBuilder SignalR(string eventName)
    {
        this._protocols |= Protocol.SignalR;
        this._signalREvent = eventName;
        return (TBuilder)this;
    }

    // HTTP method methods
    public TBuilder Get(string path)
    {
        this._httpMethod = "GET";
        this._apiPath = path;
        return this.Api();
    }

    public TBuilder Post(string path)
    {
        this._httpMethod = "POST";
        this._apiPath = path;
        return this.Api();
    }

    public TBuilder Put(string path)
    {
        this._httpMethod = "PUT";
        this._apiPath = path;
        return this.Api();
    }

    public TBuilder Delete(string path)
    {
        this._httpMethod = "DELETE";
        this._apiPath = path;
        return this.Api();
    }

    // API return type method
    public TBuilder ApiReturns<T>()
    {
        this._apiReturnType = typeof(T);
        return (TBuilder)this;
    }

    // Documentation methods
    public TBuilder Description(string description)
    {
        this._description = description;
        return (TBuilder)this;
    }

    // Modifier methods
    public TBuilder Exclude(Protocol protocol, string reason)
    {
        this._exclusions.Add(new ProtocolExclusion(protocol, reason));
        return (TBuilder)this;
    }

    public TBuilder RecentlyAdded()
    {
        this._isRecentlyAdded = true;
        return (TBuilder)this;
    }

    public TBuilder Optional()
    {
        this._isOptional = true;
        return (TBuilder)this;
    }

    protected abstract void BuildFeature();
}

/// <summary>
/// Builder for command specifications.
/// </summary>
public class CommandBuilder(BlueprintBuilder blueprintBuilder, string id)
    : FeatureBuilder<CommandBuilder>(blueprintBuilder, id)
{
    // Fluent continuation methods
    public CommandBuilder Command(string id)
    {
        this.BuildFeature();
        return this._blueprintBuilder.Command(id);
    }

    public StatusBuilder Status(string id)
    {
        this.BuildFeature();
        return this._blueprintBuilder.Status(id);
    }

    public Blueprint Build()
    {
        this.BuildFeature();
        return this._blueprintBuilder.Build();
    }

    protected override void BuildFeature()
    {
        var command = new CommandSpec(this._id)
        {
            Category = this._category,
            Protocols = this._protocols,
            Description = this._description,
            IsOptional = this._isOptional,
            IsRecentlyAdded = this._isRecentlyAdded,
            Exclusions = this._exclusions,
            HttpMethod = this._httpMethod,
            ApiPath = this._apiPath,
            MqttTopic = this._mqttTopic,
            SignalREvent = this._signalREvent,
            ApiReturnType = this._apiReturnType,
        };

        this._blueprintBuilder.AddCommand(command);
    }
}

/// <summary>
/// Builder for status specifications.
/// </summary>
public class StatusBuilder(BlueprintBuilder blueprintBuilder, string id)
    : FeatureBuilder<StatusBuilder>(blueprintBuilder, id)
{
    // Fluent continuation methods
    public CommandBuilder Command(string id)
    {
        this.BuildFeature();
        return this._blueprintBuilder.Command(id);
    }

    public StatusBuilder Status(string id)
    {
        this.BuildFeature();
        return this._blueprintBuilder.Status(id);
    }

    public Blueprint Build()
    {
        this.BuildFeature();
        return this._blueprintBuilder.Build();
    }

    protected override void BuildFeature()
    {
        var status = new StatusSpec(this._id)
        {
            Category = this._category,
            Protocols = this._protocols,
            Description = this._description,
            IsOptional = this._isOptional,
            IsRecentlyAdded = this._isRecentlyAdded,
            Exclusions = this._exclusions,
            HttpMethod = this._httpMethod,
            ApiPath = this._apiPath,
            MqttTopic = this._mqttTopic,
            SignalREvent = this._signalREvent,
            ApiReturnType = this._apiReturnType,
        };

        this._blueprintBuilder.AddStatus(status);
    }
}
