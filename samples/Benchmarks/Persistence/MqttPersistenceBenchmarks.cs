using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Benchmarks.Communication;
using Microsoft.Extensions.Logging.Abstractions;
using MQTTnet.Extensions;
using MQTTnet.Server;
using NSubstitute;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.Persistence;
using ViciOne.ManagedEngine.Runtime;

namespace Benchmarks.Persistence;

[WorkloadMethodColumn('_', "Technology", "Type", "Direction")]
[ParamsExpansionColumn<TestCase>(nameof(TestCase))]
[ParamsExpansionColumn<MqttTestEnvironment>(nameof(MqttTestEnvironment))]
[ThroughputColumn(TestDataGenerator.Values)]
[CsvCompliantExporter]
[SuppressMessage("Maintainability", "CA1515:Erwägen Sie, öffentliche Typen intern zu machen.", Justification = "Sonst nicht von BenchmarkDotnet aufrufbar")]
public class MqttPersistenceBenchmarks : IDisposable
{
    [ParamsSource(typeof(TestCase), nameof(TestCase.DefaultSet))]
    public TestCase TestCase { get; set; }

    [ParamsSource(typeof(MqttTestEnvironment), nameof(MqttTestEnvironment.DefaultSet))]
    public MqttTestEnvironment MqttTestEnvironment { get; set; }

    private MQTTnet.Server.MqttServer? _broker;
    private MqttCommunication _mqttCommunication;
    private VirtualMqttClient _mqttClient;
    private TestData<PersistenceEntry> _data;
    private MqttPersistence _persistence;
    private bool _disposedValue;

    public MqttPersistenceBenchmarks()
    {
        MqttTestEnvironment = null!;
        TestCase = null!;
        _mqttCommunication = null!;
        _mqttClient = null!;
        _data = null!;
        _persistence = null!;
    }

    [GlobalSetup]
    public async Task GlobalSetupAsync()
    {
        if (MqttTestEnvironment.InProcessBroker && MqttTestEnvironment.Port.HasValue)
        {
            _broker = new MqttServerFactory().CreateMqttServer(
                new MqttServerOptionsBuilder()
                    .WithDefaultEndpoint()
                    .WithDefaultEndpointPort(MqttTestEnvironment.Port.Value)
                    .Build());
            await _broker.StartAsync();
        }

        _mqttCommunication = MqttTestEnvironment.ToMqttCommunication();
        _mqttClient = new(MqttOptimizer.Instance.Register(_mqttCommunication.ToCommunicationInfo(), NullLoggerFactory.Instance));

        _data = TestDataGenerator.CreatePersistenceEntries(TestCase);
        var entryIds = _data.EngineCycles.SelectMany(e => e.Values).Select(e => e.UniqueIdentifier).Distinct().ToList();
        _persistence = new MqttPersistence(new() { QualityOfService = 0 }, _mqttClient, NullLogger<MqttPersistence>.Instance, Substitute.For<INameResolver>(), AssemblyLoadContext.Default);
        await _persistence.ConnectAsync(CancellationToken.None);
    }

    [GlobalCleanup]
    public async Task GlobalCleanupAsync()
    {
        await _persistence.DisconnectAsync(CancellationToken.None);
        _persistence.Dispose();
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
    public async Task Mqtt_Persistence_Outgoing_Async()
    {
        foreach (var engineCycle in _data.EngineCycles)
            await _persistence.SaveAsync(engineCycle.Values, CancellationToken.None);
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
                _persistence.Dispose();
                _mqttClient.Dispose();
                _broker?.Dispose();
            }
            _disposedValue = true;
        }
    }
}
