using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Benchmarks.Communication;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.ManagedEngine.Runtime;

namespace Benchmarks.ExternalCommunication;

[WorkloadMethodColumn('_', "Technology", "Type", "Direction")]
[ParamsExpansionColumn<TestCase>(nameof(TestCase))]
[ThroughputColumn(TestDataGenerator.Values)]
[CsvCompliantExporter]
[SuppressMessage("Maintainability", "CA1515:Erwägen Sie, öffentliche Typen intern zu machen.", Justification = "Sonst nicht von BenchmarkDotnet aufrufbar")]
public class DirectExternalOutgoingCommunicationBenchmarks
{
    [ParamsSource(typeof(TestCase), nameof(TestCase.DefaultSet))]
    public TestCase TestCase { get; set; }

    private DirectExternalOutgoingCommunication _communication;
    private TestData<ExternalValue> _data;
    private readonly List<object> _receivedData;
    private SubscriberInfo? _subscriber;

    public DirectExternalOutgoingCommunicationBenchmarks()
    {
        _communication = new(new(), new NullLogger<DirectExternalOutgoingCommunication>(), Substitute.For<INameResolver>(), AssemblyLoadContext.Default);
        TestCase = new();
        _data = new();
        _receivedData = [];
    }

    [GlobalSetup]
    public async Task GlobalSetupAsync()
    {
        _data = TestDataGenerator.CreateExternalValues(TestCase);
        var channels = GetChannels(_data);
        _communication = new(new()
        {
            Channels = channels,
            ConnectionUniqueIdentifier = "Service",
            EngineUniqueIdentifier = nameof(DirectExternalOutgoingCommunicationBenchmarks),
        }, new NullLogger<DirectExternalOutgoingCommunication>(), Substitute.For<INameResolver>(), AssemblyLoadContext.Default);
        _subscriber = new(AssemblyLoadContext.Default.GetHashCode(), _receivedData.Add, _receivedData.Add);
        await DirectExchange.Instance.SubscribeAsync(channels, _subscriber);
    }

    private static List<string> GetChannels(TestData<ExternalValue> data)
        => [.. data.EngineCycles.SelectMany(p => p.Values.Select(l => l.Channel)).Distinct()];

    [GlobalCleanup]
    public async Task GlobalCleanupAsync()
    {
        if (_subscriber is not null)
            await DirectExchange.Instance.UnsubscribeAsync(_subscriber);
    }

    [Benchmark]
    [SuppressMessage("Naming", "CA1707:Bezeichner dürfen keine Unterstriche enthalten", Justification = "Definiertes Pattern zur Extrahierung von TestCases")]
    public async Task Direct_External_Outgoing_Async()
    {
        await _communication.ConnectAsync(CancellationToken.None);
        foreach (var engineCycle in _data.EngineCycles)
            await _communication.SendAsync(engineCycle.Number, engineCycle.Values, CancellationToken.None);
        while (_receivedData.Count != TestDataGenerator.Values)
            await Task.Delay(1);
        await _communication.DisconnectAsync(CancellationToken.None);
        _receivedData.Clear();
    }
}
