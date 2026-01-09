using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Server;
using ViciOne.ManagedEngine.EngineDeployment;

namespace ViciOne.ManagedEngine;

internal sealed class App : IAsyncDisposable
{
    private readonly IMqttClient _mqttClient;
    private readonly MqttServer? _mqttServer;
    private readonly string _engineHostUniqueIdentifier;
    private readonly DeployParameter[] _deployParameters;
    private readonly IMqttRpcClient _mqttRpcClient;
    private readonly EngineHostCommunication _engineHostCommunication;

    internal static async Task RunAsync(AppSettings appSettings, CancellationToken cancellationToken)
    {
        var mqttFactory = new MqttFactory();
        using var mqttClient = mqttFactory.CreateMqttClient();
        MqttServer? mqttServer = default;

        if (appSettings.MqttBroker)
        {
            mqttServer = mqttFactory.CreateMqttServer(new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(appSettings.MqttPort)
                .Build());
            await mqttServer.StartAsync();
        }
        await mqttClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(appSettings.MqttHost, appSettings.MqttPort).WithProtocolVersion(MqttProtocolVersion.V500).Build(), cancellationToken);

        await using var app = new App(mqttClient, mqttServer, appSettings.EngineHostUniqueIdentifier, appSettings.DeployParameters);

        await app.RunAsync(cancellationToken);
    }

    private App(IMqttClient mqttClient, MqttServer? mqttServer, string engineHostUniqueIdentifier, DeployParameter[] deployParameters)
    {
        _mqttClient = mqttClient;
        _mqttServer = mqttServer;
        _engineHostUniqueIdentifier = engineHostUniqueIdentifier;
        _deployParameters = deployParameters;
        _mqttRpcClient = new MqttNetRpcClient(_mqttClient);
        _engineHostCommunication = new EngineHostCommunication(_mqttRpcClient);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        List<string> deployments = [];

        foreach (var deployParameter in _deployParameters)
        {
            var deploymentIdentifier = await _engineHostCommunication.DeployAsync(_engineHostUniqueIdentifier, deployParameter, cancellationToken)
                .PrintAsync("Deploy");
            deployments.Add(deploymentIdentifier);

            await _engineHostCommunication.GetStateAsync(_engineHostUniqueIdentifier, deploymentIdentifier, cancellationToken)
                .PrintStatusAsync(deploymentIdentifier);
        }

        foreach (var deployment in deployments)
        {
            await _engineHostCommunication.StartAsync(_engineHostUniqueIdentifier, deployment, cancellationToken)
                .PrintAsync("Start", deployment);
            await _engineHostCommunication.GetStateAsync(_engineHostUniqueIdentifier, deployment, cancellationToken)
                .PrintStatusAsync(deployment);
        }

        await UI.WaitAsync(10_000);

        foreach (var deployment in deployments)
        {
            await _engineHostCommunication.GetStateAsync(_engineHostUniqueIdentifier, deployment, cancellationToken)
                .PrintStatusAsync(deployment);
            await _engineHostCommunication.StopAsync(_engineHostUniqueIdentifier, deployment, cancellationToken)
                .PrintAsync("Stop", deployment);
            await _engineHostCommunication.GetStateAsync(_engineHostUniqueIdentifier, deployment, cancellationToken)
                .PrintStatusAsync(deployment);
            await _engineHostCommunication.TearDownAsync(_engineHostUniqueIdentifier, deployment, cancellationToken)
                .PrintAsync("TearDown", deployment);
        }
    }

    public async ValueTask DisposeAsync()
    {
        (_mqttRpcClient as IDisposable)?.Dispose();
        if (_mqttRpcClient is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();

        await _mqttClient.DisconnectAsync();
        _mqttClient.Dispose();
        if (_mqttServer is not null)
        {
            await _mqttServer.StopAsync();
            _mqttServer.Dispose();
        }
    }
}
