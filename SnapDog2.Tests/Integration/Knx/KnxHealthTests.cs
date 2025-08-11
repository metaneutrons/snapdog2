namespace SnapDog2.Tests.Integration.Knx;

using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SnapDog2.Core.Configuration;
using SnapDog2.Tests.Integration.Fixtures;

[Collection("KnxdContainer")]
public class KnxHealthTests : IClassFixture<KnxIntegrationWebApplicationFactory>
{
    private readonly KnxIntegrationWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public KnxHealthTests(KnxIntegrationWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ReadyEndpoint_ShouldIncludeKnxHealthCheck()
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<SnapDogConfiguration>();
        config.Api.Enabled.Should().BeTrue();

        var response = await _client.GetAsync("/api/health/ready");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();

        using var doc = JsonDocument.Parse(content);
        if (doc.RootElement.TryGetProperty("Checks", out var checks))
        {
            var hasKnx = checks
                .EnumerateArray()
                .Any(e =>
                    e.TryGetProperty("Name", out var name)
                    && string.Equals(name.GetString(), "knx", StringComparison.OrdinalIgnoreCase)
                );
            hasKnx.Should().BeTrue("KNX health check should be present when KNX is enabled");
        }
    }

    [Fact]
    public async Task KnxTcpPort_ShouldBeReachable()
    {
        // Verify raw TCP connectivity to knxd control port from the host
        var host = Environment.GetEnvironmentVariable("SNAPDOG_TEST_KNX_HOST") ?? "localhost";
        var portStr = Environment.GetEnvironmentVariable("SNAPDOG_TEST_KNX_TCP_PORT") ?? "0";
        portStr.Should().NotBe("0");
        var port = int.Parse(portStr);
        using var client = new TcpClient();
        var connectTask = client.ConnectAsync(host, port);
        var completed = await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(5)));
        completed.Should().Be(connectTask);
        client.Connected.Should().BeTrue();
    }
}

public class KnxIntegrationWebApplicationFactory : CustomWebApplicationFactory
{
    private readonly Dictionary<string, string?> _originalValues = new();
    private static readonly object _lock = new object();

    public KnxIntegrationWebApplicationFactory()
    {
        var knxHost = Environment.GetEnvironmentVariable("SNAPDOG_TEST_KNX_HOST") ?? "localhost";
        var knxTcpPort = Environment.GetEnvironmentVariable("SNAPDOG_TEST_KNX_TCP_PORT") ?? "6720";

        // Enable API and health
        SetEnv("SNAPDOG_SYSTEM_HEALTH_CHECKS_ENABLED", "true");
        SetEnv("SNAPDOG_API_ENABLED", "true");
        SetEnv("SNAPDOG_API_AUTH_ENABLED", "false");
        SetEnv("SNAPDOG_API_PORT", "0");

        // Point KNX to knxd container; use TCP port 6720 so the TCP health check passes
        SetEnv("SNAPDOG_SERVICES_KNX_ENABLED", "true");
        SetEnv("SNAPDOG_SERVICES_KNX_GATEWAY", knxHost);
        SetEnv("SNAPDOG_SERVICES_KNX_PORT", knxTcpPort);
        SetEnv("SNAPDOG_SERVICES_KNX_TIMEOUT", "10");
        SetEnv("SNAPDOG_SERVICES_KNX_AUTO_RECONNECT", "true");

        // Other integrations off to reduce noise
        SetEnv("SNAPDOG_SERVICES_MQTT_ENABLED", "false");
        SetEnv("SNAPDOG_SERVICES_SUBSONIC_ENABLED", "false");

        // Snapcast target for health (may be unhealthy)
        SetEnv("SNAPDOG_SERVICES_SNAPCAST_ADDRESS", "localhost");
        SetEnv("SNAPDOG_SERVICES_SNAPCAST_JSONRPC_PORT", "1704");
    }

    private void SetEnv(string name, string value)
    {
        lock (_lock)
        {
            _originalValues[name] = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_lock)
            {
                foreach (var kvp in _originalValues)
                {
                    Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
                }
                _originalValues.Clear();
            }
        }
        base.Dispose(disposing);
    }
}
