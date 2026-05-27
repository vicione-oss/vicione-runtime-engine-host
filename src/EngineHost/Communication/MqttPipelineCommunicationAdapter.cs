using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Extensions;

namespace ViciOne.ManagedEngine.Communication;

internal sealed class MqttPipelineCommunicationAdapter(IOptions<HostConfig> config, CommunicationMethods methods,
    [FromKeyedServices(MqttConnectionKeys.CommandBus)] IVirtualMqttClient mqttClient,
    ILogger<MqttPipelineCommunicationAdapter> logger,
    TimeProvider? timeProvider = default) : IHostedLifecycleService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private MqttRpcServer? _server;

    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_server is null)
        {
            var requestTopic = $"{config.Value.Id}/request";
            using PipelineSubscriptionWatcher subscriptionWatcher = new(requestTopic, mqttClient);
            using CancellationTokenSource timeoutSource = new(TimeSpan.FromSeconds(10), _timeProvider);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

            _server = MqttSetup.CreateRpcServer(mqttClient, requestTopic, $"{config.Value.Id}/response", logger,
                methods.MqttRpcRegistrations);

            await _server.Start().ConfigureAwait(false);
            await subscriptionWatcher.Completion.WaitAsync(linkedCts.Token).ConfigureAwait(false);
        }
    }

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_server is not null)
        {
            await _server.Stop().ConfigureAwait(false);
            _server.Dispose();
            _server = null;
        }
    }

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
