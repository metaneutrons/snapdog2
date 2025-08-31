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
/// Main blueprint containing all system specifications.
/// </summary>
public class Blueprint
{
    public FeatureCollection<CommandSpec> Commands { get; }
    public FeatureCollection<StatusSpec> Status { get; }
    public FeatureCollection<FeatureSpec> All { get; }

    internal Blueprint(IEnumerable<CommandSpec> commands, IEnumerable<StatusSpec> status)
    {
        this.Commands = new FeatureCollection<CommandSpec>(commands);
        this.Status = new FeatureCollection<StatusSpec>(status);

        // Combine commands and status into a single collection
        var allFeatures = commands.Concat(status.Cast<FeatureSpec>());
        this.All = new FeatureCollection<FeatureSpec>(allFeatures);
    }

    /// <summary>
    /// Start defining a new blueprint.
    /// </summary>
    public static BlueprintBuilder Define() => new();
}

/// <summary>
/// Builder for creating blueprint specifications.
/// </summary>
public class BlueprintBuilder
{
    private readonly List<CommandSpec> _commands = new();
    private readonly List<StatusSpec> _status = new();

    /// <summary>
    /// Define a command specification.
    /// </summary>
    public CommandBuilder Command(string id) => new(this, id);

    /// <summary>
    /// Define a status specification.
    /// </summary>
    public StatusBuilder Status(string id) => new(this, id);

    /// <summary>
    /// Build the final blueprint.
    /// </summary>
    public Blueprint Build() => new(this._commands, this._status);

    internal void AddCommand(CommandSpec command) => this._commands.Add(command);

    internal void AddStatus(StatusSpec status) => this._status.Add(status);
}

/// <summary>
/// Supported protocols.
/// </summary>
[Flags]
public enum Protocol
{
    None = 0,
    Api = 1,
    Mqtt = 2,
    Knx = 4,
    SignalR = 8,
}

/// <summary>
/// Feature categories.
/// </summary>
public enum FeatureCategory
{
    Global,
    Zone,
    Client,
    Media,
}

/// <summary>
/// Protocol exclusion with reason.
/// </summary>
public record ProtocolExclusion(Protocol Protocol, string Reason);
