using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Extensions;
using MQTTnet.Protocol;
using ViciOne.ManagedEngine.Communication;

namespace ViciOne.ManagedEngine.Runtime;

internal sealed class LifetimeService(IOptions<HostConfig> options, [FromKeyedServices(MqttConnectionKeys.CommandBus)] IVirtualMqttClient mqttClient,
    ILogger<LifetimeService> logger) : IHostedLifecycleService
{
    private const string UpAndRunningPayload = "upandrunning";
    private const string InterruptionPayload = "interruption";

    internal static void ConfigureLastWill(HostConfig hostConfig, CommunicationInfo communication)
    {
        communication.WillTopic = GetTopic(hostConfig);
        communication.WillMessage = InterruptionPayload;
        communication.WillContentType = MediaTypeNames.Text.Plain;
    }

    private static string GetTopic(HostConfig hostConfig) => $"{hostConfig.Id}/state";

    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartAsync(CancellationToken cancellationToken) => mqttClient.Connect();
    public Task StartedAsync(CancellationToken cancellationToken) => SendUpAndRunning();

    private async Task SendUpAndRunning()
    {
        logger.UpAndRunning(EngineHost.InformationalVersion);
        await mqttClient.Publish(new MqttApplicationMessageBuilder()
            .WithTopic(GetTopic(options.Value))
            .WithPayload(UpAndRunningPayload)
            .WithPayloadFormatIndicator(MqttPayloadFormatIndicator.CharacterData)
            .WithContentType(MediaTypeNames.Text.Plain)
            .WithUserProperty(nameof(EngineHost.Version), Encoding.UTF8.GetBytes(EngineHost.InformationalVersion))
            .Build());
    }

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => mqttClient.Disconnect();
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
