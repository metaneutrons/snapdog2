using SnapDog2.Core.Configuration;

namespace SnapDog2.Tests.Helpers.Builders;

/// <summary>
/// Enterprise-grade builder for creating ZoneConfig test data with fluent API
/// </summary>
public class ZoneConfigBuilder
{
    private string _name = "Test Zone";
    private string _sink = "/snapsinks/test";
    private ZoneMqttConfig? _mqtt;
    private ZoneKnxConfig? _knx;

    /// <summary>
    /// Sets the zone name
    /// </summary>
    public ZoneConfigBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the Snapcast sink path
    /// </summary>
    public ZoneConfigBuilder WithSink(string sink)
    {
        _sink = sink;
        return this;
    }

    /// <summary>
    /// Configures MQTT settings
    /// </summary>
    public ZoneConfigBuilder WithMqtt(Action<ZoneMqttConfigBuilder> configure)
    {
        var builder = new ZoneMqttConfigBuilder();
        configure(builder);
        _mqtt = builder.Build();
        return this;
    }

    /// <summary>
    /// Configures KNX settings
    /// </summary>
    public ZoneConfigBuilder WithKnx(Action<ZoneKnxConfigBuilder> configure)
    {
        var builder = new ZoneKnxConfigBuilder();
        configure(builder);
        _knx = builder.Build();
        return this;
    }

    /// <summary>
    /// Creates a ground floor zone configuration
    /// </summary>
    public static ZoneConfigBuilder GroundFloor()
    {
        return new ZoneConfigBuilder()
            .WithName("Ground Floor")
            .WithSink("/snapsinks/zone1")
            .WithMqtt(mqtt => mqtt.WithBaseTopic("snapdog/zones/1"));
    }

    /// <summary>
    /// Creates a first floor zone configuration
    /// </summary>
    public static ZoneConfigBuilder FirstFloor()
    {
        return new ZoneConfigBuilder()
            .WithName("1st Floor")
            .WithSink("/snapsinks/zone2")
            .WithMqtt(mqtt => mqtt.WithBaseTopic("snapdog/zones/2"));
    }

    /// <summary>
    /// Creates an outdoor zone configuration
    /// </summary>
    public static ZoneConfigBuilder Outdoor()
    {
        return new ZoneConfigBuilder()
            .WithName("Outdoor")
            .WithSink("/snapsinks/outdoor")
            .WithMqtt(mqtt => mqtt.WithBaseTopic("snapdog/zones/outdoor"));
    }

    /// <summary>
    /// Builds the ZoneConfig instance
    /// </summary>
    public ZoneConfig Build()
    {
        return new ZoneConfig
        {
            Name = _name,
            Sink = _sink,
            Mqtt = _mqtt ?? new ZoneMqttConfig(),
            Knx = _knx ?? new ZoneKnxConfig(),
        };
    }
}

/// <summary>
/// Builder for ZoneMqttConfig
/// </summary>
public class ZoneMqttConfigBuilder
{
    private string _baseTopic = "snapdog/zones/test";

    public ZoneMqttConfigBuilder WithBaseTopic(string baseTopic)
    {
        _baseTopic = baseTopic;
        return this;
    }

    public ZoneMqttConfig Build()
    {
        return new ZoneMqttConfig { BaseTopic = _baseTopic };
    }
}

/// <summary>
/// Builder for ZoneKnxConfig
/// </summary>
public class ZoneKnxConfigBuilder
{
    private string? _volumeGroupAddress;
    private string? _muteGroupAddress;
    private string? _playPauseGroupAddress;

    public ZoneKnxConfigBuilder WithVolumeGroupAddress(string address)
    {
        _volumeGroupAddress = address;
        return this;
    }

    public ZoneKnxConfigBuilder WithMuteGroupAddress(string address)
    {
        _muteGroupAddress = address;
        return this;
    }

    public ZoneKnxConfigBuilder WithPlayPauseGroupAddress(string address)
    {
        _playPauseGroupAddress = address;
        return this;
    }

    public ZoneKnxConfig Build()
    {
        return new ZoneKnxConfig
        {
            Volume = _volumeGroupAddress,
            Mute = _muteGroupAddress,
            Play = _playPauseGroupAddress,
        };
    }
}
