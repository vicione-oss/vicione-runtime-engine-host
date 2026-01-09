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

internal sealed class LifetimeService : IHostedService, IDisposable
{
    private const string UpAndRunningPayload = "upandrunning";
    private const string InterruptionPayload = "interruption";

    private readonly IVirtualMqttClient _client;
    private readonly string _topic;
    private readonly ILogger<LifetimeService> _logger;

    public LifetimeService(IOptions<HostConfig> options, ILoggerFactory loggerFactory, ILogger<LifetimeService> logger, IHostApplicationLifetime applicationLifetime, MqttOptimizer mqttOptimizer)
        : this(options, mqttOptimizer.Register(options.Value.CommandBus, loggerFactory), logger, applicationLifetime)
    {
    }

    internal LifetimeService(IOptions<HostConfig> options, IVirtualMqttClient mqttClient, ILogger<LifetimeService> logger, IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _topic = GetTopic(options.Value);
        _client = mqttClient;

        applicationLifetime.ApplicationStarted.Register(SendUpAndRunning);
    }

    internal static void ConfigureLastWill(HostConfig hostConfig, CommunicationInfo communication)
    {
        communication.WillTopic = GetTopic(hostConfig);
        communication.WillMessage = InterruptionPayload;
        communication.WillContentType = MediaTypeNames.Text.Plain;
    }

    private static string GetTopic(HostConfig hostConfig) => $"{hostConfig.Id}/state";

    private void SendUpAndRunning()
    {
        _client.Publish(new MqttApplicationMessageBuilder()
            .WithTopic(_topic)
            .WithPayload(UpAndRunningPayload)
            .WithPayloadFormatIndicator(MqttPayloadFormatIndicator.CharacterData)
            .WithContentType(MediaTypeNames.Text.Plain)
            .WithUserProperty(nameof(EngineHost.Version), EngineHost.InformationalVersion)
            .Build()).GetAwaiter().GetResult();

        _logger.UpAndRunning(EngineHost.InformationalVersion);
    }

    public Task StartAsync(CancellationToken cancellationToken) => _client.Connect();
    public Task StopAsync(CancellationToken cancellationToken) => _client.Disconnect();
    public void Dispose() => _client.Dispose();
}
