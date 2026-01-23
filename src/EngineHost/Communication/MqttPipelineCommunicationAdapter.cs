using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Extensions;

namespace ViciOne.ManagedEngine.Communication;

internal sealed class MqttPipelineCommunicationAdapter : IHostedLifecycleService
{
    private readonly IOptions<HostConfig> _config;
    private readonly MqttRpcRegistrations _rpcRegistrations;
    private readonly ILogger<MqttPipelineCommunicationAdapter> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly MqttOptimizer _mqttOptimizer;
    private IVirtualMqttClient? _mqttClient;
    private readonly TimeProvider _timeProvider;
    private MqttRpcServer? _server;

    public MqttPipelineCommunicationAdapter(IOptions<HostConfig> config, CommunicationMethods methods,
        ILogger<MqttPipelineCommunicationAdapter> logger, ILoggerFactory loggerFactory, MqttOptimizer mqttOptimizer)
    {
        _config = config;
        _rpcRegistrations = methods.MqttRpcRegistrations;
        _logger = logger;
        _timeProvider = TimeProvider.System;
        _loggerFactory = loggerFactory;
        _mqttOptimizer = mqttOptimizer;
    }

    internal MqttPipelineCommunicationAdapter(IOptions<HostConfig> config, CommunicationMethods methods,
        ILogger<MqttPipelineCommunicationAdapter> logger, IVirtualMqttClient client, TimeProvider timeProvider)
    {
        _config = config;
        _rpcRegistrations = methods.MqttRpcRegistrations;
        _logger = logger;
        _mqttClient = client;
        _timeProvider = timeProvider;
        _loggerFactory = null!;
        _mqttOptimizer = null!;
    }

    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _mqttClient ??= _mqttOptimizer.Register(_config.Value.CommandBus, _loggerFactory);
        if (_server is null)
        {
            var requestTopic = $"{_config.Value.Id}/request";
            using PipelineSubscriptionWatcher subscriptionWatcher = new(requestTopic, _mqttClient);
            using CancellationTokenSource timeoutSource = new(TimeSpan.FromSeconds(10), _timeProvider);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

            _server = MqttSetup.CreateRpcServer(_mqttClient, requestTopic, $"{_config.Value.Id}/response", _logger, _rpcRegistrations);

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
            _server = null;
        }
        if (_mqttClient is not null)
        {
            _mqttClient.Dispose();
            _mqttClient = null;
        }
    }

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
