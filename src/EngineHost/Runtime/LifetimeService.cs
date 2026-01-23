using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Extensions;
using MQTTnet.Protocol;

namespace ViciOne.ManagedEngine.Runtime;

internal sealed class LifetimeService : IHostedLifecycleService, IDisposable
{
    private const string UpAndRunningPayload = "upandrunning";
    private const string InterruptionPayload = "interruption";

    private readonly IVirtualMqttClient _client;
    private readonly string _topic;
    private readonly ILogger<LifetimeService> _logger;

    public LifetimeService(IOptions<HostConfig> options, ILoggerFactory loggerFactory, ILogger<LifetimeService> logger, MqttOptimizer mqttOptimizer)
        : this(options, mqttOptimizer.Register(options.Value.CommandBus, loggerFactory), logger)
    {
    }

    internal LifetimeService(IOptions<HostConfig> options, IVirtualMqttClient mqttClient, ILogger<LifetimeService> logger)
    {
        _logger = logger;
        _topic = GetTopic(options.Value);
        _client = mqttClient;
    }

    internal static void ConfigureLastWill(HostConfig hostConfig, CommunicationInfo communication)
    {
        communication.WillTopic = GetTopic(hostConfig);
        communication.WillMessage = InterruptionPayload;
        communication.WillContentType = MediaTypeNames.Text.Plain;
    }

    private static string GetTopic(HostConfig hostConfig) => $"{hostConfig.Id}/state";

    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartAsync(CancellationToken cancellationToken) => _client.Connect();
    public Task StartedAsync(CancellationToken cancellationToken) => SendUpAndRunning();

    private async Task SendUpAndRunning()
    {
        _logger.UpAndRunning(EngineHost.InformationalVersion);
        await _client.Publish(new MqttApplicationMessageBuilder()
            .WithTopic(_topic)
            .WithPayload(UpAndRunningPayload)
            .WithPayloadFormatIndicator(MqttPayloadFormatIndicator.CharacterData)
            .WithContentType(MediaTypeNames.Text.Plain)
            .WithUserProperty(nameof(EngineHost.Version), EngineHost.InformationalVersion)
            .Build());
    }

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => _client.Disconnect();
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public void Dispose() => _client.Dispose();
}
