using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Benchmarks.Communication;
using Microsoft.Extensions.Logging.Abstractions;
using MQTTnet;
using MQTTnet.Extensions;
using MQTTnet.Server;
using NSubstitute;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.ManagedEngine.Runtime;

namespace Benchmarks.ExternalCommunication;

[WorkloadMethodColumn('_', "Technology", "Type", "Direction")]
[ParamsExpansionColumn<TestCase>(nameof(TestCase))]
[ParamsExpansionColumn<MqttTestEnvironment>(nameof(MqttTestEnvironment))]
[ThroughputColumn(TestDataGenerator.Values)]
[CsvCompliantExporter]
[SuppressMessage("Maintainability", "CA1515:Erwägen Sie, öffentliche Typen intern zu machen.", Justification = "Sonst nicht von BenchmarkDotnet aufrufbar")]
public class MqttExternalOutgoingCommunicationBenchmarks : IDisposable
{
    [ParamsSource(typeof(TestCase), nameof(TestCase.DefaultSet))]
    public TestCase TestCase { get; set; }

    [ParamsSource(typeof(MqttTestEnvironment), nameof(MqttTestEnvironment.DefaultSet))]
    public MqttTestEnvironment MqttTestEnvironment { get; set; }

    private MQTTnet.Server.MqttServer? _broker;
    private MqttCommunication _mqttCommunication;
    private IVirtualMqttClient _mqttClient;
    private TestData<ExternalValue> _data;
    private MqttExternalOutgoingCommunication _outgoing;
    private bool _disposedValue;

    public MqttExternalOutgoingCommunicationBenchmarks()
    {
        MqttTestEnvironment = null!;
        TestCase = null!;
        _mqttCommunication = null!;
        _mqttClient = null!;
        _data = null!;
        _outgoing = null!;
    }

    [GlobalSetup]
    public async Task GlobalSetupAsync()
    {
        if (MqttTestEnvironment.InProcessBroker && MqttTestEnvironment.Port.HasValue)
        {
            _broker = new MqttFactory().CreateMqttServer(
                new MqttServerOptionsBuilder()
                    .WithDefaultEndpoint()
                    .WithDefaultEndpointPort(MqttTestEnvironment.Port.Value)
                    .Build());
            await _broker.StartAsync();
        }

        _mqttCommunication = MqttTestEnvironment.ToMqttCommunication();
        _mqttClient = new VirtualMqttClient(MqttOptimizer.Instance.Register(_mqttCommunication.ToCommunicationInfo(), NullLoggerFactory.Instance));

        _data = TestDataGenerator.CreateExternalValues(TestCase);

        var outgoingOptions = new ExternalOutgoingCommunicationOptions()
        {
            Channels = GetChannels(_data),
            ConnectionUniqueIdentifier = nameof(MqttExternalOutgoingCommunicationBenchmarks),
            EngineUniqueIdentifier = nameof(MqttExternalOutgoingCommunicationBenchmarks),
        };
        _outgoing = new(_mqttCommunication, outgoingOptions, _mqttClient, NullLogger<MqttExternalOutgoingCommunication>.Instance, Substitute.For<INameResolver>(), AssemblyLoadContext.Default);

        await _outgoing.ConnectAsync(CancellationToken.None);
    }

    private static List<string> GetChannels(TestData<ExternalValue> data)
        => [.. data.EngineCycles.SelectMany(p => p.Values.Select(l => l.Channel)).Distinct()];

    [GlobalCleanup]
    public async Task GlobalCleanupAsync()
    {
        await _outgoing.DisconnectAsync(CancellationToken.None);
        _outgoing.Dispose();
        _mqttClient.Dispose();
        if (_broker is not null)
        {
            await _broker.StopAsync(new());
            _broker.Dispose();
            _broker = null;
        }
    }

    [Benchmark]
    [SuppressMessage("Naming", "CA1707:Bezeichner dürfen keine Unterstriche enthalten", Justification = "Definiertes Pattern zur Extrahierung von TestCases")]
    public async Task Mqtt_External_Outgoing_Async()
    {
        TaskCompletionSource tcs = new();
        var packageCount = 0;
        _mqttClient.InnerClient.ApplicationMessageProcessedAsync += InnerClient_ApplicationMessageProcessedAsync;
        foreach (var engineCycle in _data.EngineCycles)
            await _outgoing.SendAsync(engineCycle.Number, engineCycle.Values, CancellationToken.None);
        await tcs.Task;

        Task InnerClient_ApplicationMessageProcessedAsync(MQTTnet.Extensions.ManagedClient.ApplicationMessageProcessedEventArgs arg)
        {
            if (++packageCount == TestDataGenerator.Values)
            {
                _mqttClient.InnerClient.ApplicationMessageProcessedAsync -= InnerClient_ApplicationMessageProcessedAsync;
                tcs.SetResult();
            }
            return Task.CompletedTask;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _outgoing.Dispose();
                _mqttClient.Dispose();
                _broker?.Dispose();
            }
            _disposedValue = true;
        }
    }
}
