using SnapDog2.Core.Configuration;

namespace SnapDog2.Tests.Helpers.Builders;

/// <summary>
/// Enterprise-grade builder for creating ClientConfig test data with fluent API
/// </summary>
public class ClientConfigBuilder
{
    private string _name = "Test Client";
    private string _mac = "02:42:ac:11:00:10";
    private int _defaultZone = 1;
    private ClientMqttConfig? _mqtt;
    private ClientKnxConfig? _knx;

    /// <summary>
    /// Sets the client name
    /// </summary>
    public ClientConfigBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the client MAC address
    /// </summary>
    public ClientConfigBuilder WithMac(string mac)
    {
        _mac = mac;
        return this;
    }

    /// <summary>
    /// Sets the default zone
    /// </summary>
    public ClientConfigBuilder WithDefaultZone(int zone)
    {
        _defaultZone = zone;
        return this;
    }

    /// <summary>
    /// Configures MQTT settings
    /// </summary>
    public ClientConfigBuilder WithMqtt(Action<ClientMqttConfigBuilder> configure)
    {
        var builder = new ClientMqttConfigBuilder();
        configure(builder);
        _mqtt = builder.Build();
        return this;
    }

    /// <summary>
    /// Configures KNX settings
    /// </summary>
    public ClientConfigBuilder WithKnx(Action<ClientKnxConfigBuilder> configure)
    {
        var builder = new ClientKnxConfigBuilder();
        configure(builder);
        _knx = builder.Build();
        return this;
    }

    /// <summary>
    /// Creates a living room client configuration
    /// </summary>
    public static ClientConfigBuilder LivingRoom()
    {
        return new ClientConfigBuilder()
            .WithName("Living Room")
            .WithMac("02:42:ac:11:00:10")
            .WithDefaultZone(1)
            .WithMqtt(mqtt => mqtt.WithBaseTopic("snapdog/clients/livingroom"));
    }

    /// <summary>
    /// Creates a kitchen client configuration
    /// </summary>
    public static ClientConfigBuilder Kitchen()
    {
        return new ClientConfigBuilder()
            .WithName("Kitchen")
            .WithMac("02:42:ac:11:00:11")
            .WithDefaultZone(1)
            .WithMqtt(mqtt => mqtt.WithBaseTopic("snapdog/clients/kitchen"));
    }

    /// <summary>
    /// Creates a bedroom client configuration
    /// </summary>
    public static ClientConfigBuilder Bedroom()
    {
        return new ClientConfigBuilder()
            .WithName("Bedroom")
            .WithMac("02:42:ac:11:00:12")
            .WithDefaultZone(2)
            .WithMqtt(mqtt => mqtt.WithBaseTopic("snapdog/clients/bedroom"));
    }

    /// <summary>
    /// Builds the ClientConfig instance
    /// </summary>
    public ClientConfig Build()
    {
        return new ClientConfig
        {
            Name = _name,
            Mac = _mac,
            DefaultZone = _defaultZone,
            Mqtt = _mqtt ?? new ClientMqttConfig(),
            Knx = _knx ?? new ClientKnxConfig(),
        };
    }
}

/// <summary>
/// Builder for ClientMqttConfig
/// </summary>
public class ClientMqttConfigBuilder
{
    private string _baseTopic = "snapdog/clients/test";

    public ClientMqttConfigBuilder WithBaseTopic(string baseTopic)
    {
        _baseTopic = baseTopic;
        return this;
    }

    public ClientMqttConfig Build()
    {
        return new ClientMqttConfig { BaseTopic = _baseTopic };
    }
}

/// <summary>
/// Builder for ClientKnxConfig
/// </summary>
public class ClientKnxConfigBuilder
{
    private string? _volumeGroupAddress;
    private string? _muteGroupAddress;

    public ClientKnxConfigBuilder WithVolumeGroupAddress(string address)
    {
        _volumeGroupAddress = address;
        return this;
    }

    public ClientKnxConfigBuilder WithMuteGroupAddress(string address)
    {
        _muteGroupAddress = address;
        return this;
    }

    public ClientKnxConfig Build()
    {
        return new ClientKnxConfig { Volume = _volumeGroupAddress, Mute = _muteGroupAddress };
    }
}
