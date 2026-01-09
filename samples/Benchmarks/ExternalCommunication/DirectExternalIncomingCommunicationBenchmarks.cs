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
public class DirectExternalIncomingCommunicationBenchmarks
{
    [ParamsSource(typeof(TestCase), nameof(TestCase.DefaultSet))]
    public TestCase TestCase { get; set; }

    private DirectExternalIncomingCommunication _communication;
    private TestData<ExternalValue> _data = new();
    private readonly List<ExternalValue> _receivedValues = [];

    public DirectExternalIncomingCommunicationBenchmarks()
    {
        _communication = new(NullLogger<DirectExternalIncomingCommunication>.Instance, new(), Substitute.For<INameResolver>(), AssemblyLoadContext.Default);
        TestCase = new();
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _data = TestDataGenerator.CreateExternalValues(TestCase);
        _communication = new(NullLogger<DirectExternalIncomingCommunication>.Instance,
            new()
            {
                Channels = GetChannels(_data),
                ConnectionUniqueIdentifier = "Service",
                EngineUniqueIdentifier = nameof(DirectExternalIncomingCommunicationBenchmarks),
            }, Substitute.For<INameResolver>(), AssemblyLoadContext.Default);
        _communication.Received += _receivedValues.AddRange;
    }

    private static List<string> GetChannels(TestData<ExternalValue> data)
        => [.. data.EngineCycles.SelectMany(p => p.Values.Select(l => l.Channel)).Distinct()];

    [GlobalCleanup]
    public void GlobalCleanup() => _communication.Received -= _receivedValues.AddRange;

    [Benchmark]
    [SuppressMessage("Naming", "CA1707:Bezeichner dürfen keine Unterstriche enthalten", Justification = "Definiertes Pattern zur Extrahierung von TestCases")]
    public async Task Direct_External_Incoming_Async()
    {
        await _communication.ConnectAsync(CancellationToken.None);
        foreach (var engineCycle in _data.EngineCycles)
        {
            foreach (var value in engineCycle.Values)
                _communication.HandleValueDirectly(value);
        }
        while (_receivedValues.Count != TestDataGenerator.Values)
            await Task.Delay(1);
        await _communication.DisconnectAsync(CancellationToken.None);
        _receivedValues.Clear();
    }
}
