namespace SnapDog2.Tests.Testing;

using System;
using System.Threading;
using System.Threading.Tasks;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;

public class TestMqttServiceFailing : IMqttService
{
    public bool IsConnected => true;

    public Task<Result> InitializeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure("Simulated MQTT failure"));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public Task<Result> PublishAsync(
        string topic,
        string payload,
        bool retain = false,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(Result.Failure("Simulated MQTT failure"));

    public Task<Result> PublishZoneStateAsync(
        int zoneIndex,
        ZoneState state,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(Result.Failure("Simulated MQTT failure"));

    public Task<Result> PublishClientStateAsync(
        string clientIndex,
        ClientState state,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(Result.Failure("Simulated MQTT failure"));

    public Task<Result> SubscribeAsync(IEnumerable<string> topics, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure("Simulated MQTT failure"));

    public Task<Result> UnsubscribeAsync(IEnumerable<string> topics, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure("Simulated MQTT failure"));

    public event EventHandler? Connected;
    public event EventHandler<string>? Disconnected;
    public event EventHandler<MqttMessageReceivedEventArgs>? MessageReceived;

    public Task<Result> PublishClientStatusAsync<T>(
        string clientIndex,
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(Result.Failure("Simulated MQTT failure"));

    public Task<Result> PublishZoneStatusAsync<T>(
        int zoneIndex,
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(Result.Failure("Simulated MQTT failure"));

    public Task<Result> PublishGlobalStatusAsync<T>(
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(Result.Failure("Simulated MQTT failure"));
}
