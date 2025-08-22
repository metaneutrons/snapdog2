using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Models;

namespace SnapDog2.Core.Commands;

/// <summary>
/// Command to set zone name.
/// </summary>
[CommandId("ZONE_NAME")]
public class ZoneNameCommand : ICommand<Result>
{
    /// <summary>
    /// Gets or sets the zone index.
    /// </summary>
    public int ZoneIndex { get; set; }

    /// <summary>
    /// Gets or sets the zone name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
