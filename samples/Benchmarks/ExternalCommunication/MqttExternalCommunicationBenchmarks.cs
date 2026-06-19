using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Benchmarks.Communication;
using Microsoft.Extensions.Logging.Abstractions;
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
public class MqttExternalCommunicationBenchmarks : IDisposable
{
    [ParamsSource(typeof(TestCase), nameof(TestCase.DefaultSet))]
    public TestCase TestCase { get; set; }

    [ParamsSource(typeof(MqttTestEnvironment), nameof(MqttTestEnvironment.DefaultSet))]
    public MqttTestEnvironment MqttTestEnvironment { get; set; }

    private MQTTnet.Server.MqttServer? _broker;
    private MqttCommunication _mqttCommunication;
    private VirtualMqttClient _mqttClient;
    private TestData<ExternalValue> _data;
    private MqttExternalIncomingCommunication _incoming;
    private MqttExternalOutgoingCommunication _outgoing;
    private CancellationTokenSource _receivingTokenSource;
    private readonly List<ExternalValue> _receivedValues = [];
    private bool _disposedValue;

    public MqttExternalCommunicationBenchmarks()
    {
        MqttTestEnvironment = null!;
        TestCase = null!;
        _mqttCommunication = null!;
        _mqttClient = null!;
        _data = null!;
        _incoming = null!;
        _outgoing = null!;
        _receivingTokenSource = null!;
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

        _data = TestDataGenerator.CreateExternalValues(TestCase);
        var channels = GetChannels(_data);
        var incomingOptions = new ExternalIncomingCommunicationOptions()
        {
            Channels = channels,
            ConnectionUniqueIdentifier = nameof(MqttExternalCommunicationBenchmarks),
            EngineUniqueIdentifier = nameof(MqttExternalCommunicationBenchmarks),
        };
        _incoming = new(_mqttCommunication, NullLogger<MqttExternalIncomingCommunication>.Instance, incomingOptions, _mqttClient, Substitute.For<INameResolver>(), AssemblyLoadContext.Default);
        _incoming.Received += Incoming_Received;
        var outgoingOptions = new ExternalOutgoingCommunicationOptions()
        {
            Channels = channels,
            ConnectionUniqueIdentifier = nameof(MqttExternalCommunicationBenchmarks),
            EngineUniqueIdentifier = nameof(MqttExternalCommunicationBenchmarks),
        };
        _outgoing = new(_mqttCommunication, outgoingOptions, _mqttClient, NullLogger<MqttExternalOutgoingCommunication>.Instance, Substitute.For<INameResolver>(), AssemblyLoadContext.Default);

        await _outgoing.ConnectAsync(CancellationToken.None);
        await _incoming.ConnectAsync(CancellationToken.None);
    }

    private static List<string> GetChannels(TestData<ExternalValue> data)
        => [.. data.EngineCycles.SelectMany(p => p.Values.Select(l => l.Channel)).Distinct()];

    private void Incoming_Received(IReadOnlyCollection<ExternalValue> externalValues)
    {
        _receivedValues.AddRange(externalValues);
        if (_receivedValues.Count == TestDataGenerator.Values)
            _receivingTokenSource.Cancel();
    }

    [GlobalCleanup]
    public async Task GlobalCleanupAsync()
    {
        await _incoming.DisconnectAsync(CancellationToken.None);
        await _outgoing.DisconnectAsync(CancellationToken.None);
        _outgoing.Dispose();
        _incoming.Received -= Incoming_Received;
        _incoming.Dispose();
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
    public async Task Mqtt_External_IncomingAndOutgoing_Async()
    {
        _receivingTokenSource = new();

        foreach (var engineCycle in _data.EngineCycles)
            await _outgoing.SendAsync(engineCycle.Number, engineCycle.Values, CancellationToken.None);

        try
        {
            await Task.Delay(TestDataGenerator.Values * 15, _receivingTokenSource.Token); // Assume a timeout of 15ms for each value
        }
        catch
        {
        }

        if (_receivedValues.Count != TestDataGenerator.Values)
        {
            StringBuilder builder = new();
            var sendChannels = _data.EngineCycles.SelectMany(p => p.Values).GroupBy(l => l.Channel).ToDictionary(g => g.Key, g => g.Count());
            var receivedChannels = _receivedValues.GroupBy(l => l.Channel).ToDictionary(g => g.Key, g => g.Count());
            foreach (var (sendId, sendCount) in sendChannels)
                receivedChannels.TryAdd(sendId, 0);
            foreach (var (receivedId, receivedCount) in receivedChannels)
            {
                sendChannels.TryGetValue(receivedId, out var sendCount);
                if (sendCount != receivedCount)
                    builder.AppendLine(CultureInfo.InvariantCulture, $"Channel '{receivedId}': expected {sendCount} values but received {receivedCount}");
            }
            _receivedValues.Clear();
            throw new ArgumentException(builder.ToString());
        }
        _receivedValues.Clear();
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
                _incoming.Dispose();
                _outgoing.Dispose();
                _mqttClient.Dispose();
                _receivingTokenSource.Dispose();
                _broker?.Dispose();
            }
            _disposedValue = true;
        }
    }
}
