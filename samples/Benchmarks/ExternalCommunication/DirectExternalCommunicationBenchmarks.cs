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
using NSubstitute;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.ManagedEngine.Runtime;

namespace Benchmarks.ExternalCommunication;

[WorkloadMethodColumn('_', "Technology", "Type", "Direction")]
[ParamsExpansionColumn<TestCase>(nameof(TestCase))]
[ThroughputColumn(TestDataGenerator.Values)]
[CsvCompliantExporter]
[SuppressMessage("Maintainability", "CA1515:Erwägen Sie, öffentliche Typen intern zu machen.", Justification = "Sonst nicht von BenchmarkDotnet aufrufbar")]
public class DirectExternalCommunicationBenchmarks : IDisposable
{
    [ParamsSource(typeof(TestCase), nameof(TestCase.DefaultSet))]
    public TestCase TestCase { get; set; }

    private DirectExternalIncomingCommunication _incoming;
    private DirectExternalOutgoingCommunication _outgoing;
    private TestData<ExternalValue> _data;
    private readonly List<ExternalValue> _receivedValues = [];
    private CancellationTokenSource _receivingTokenSource;
    private bool _disposedValue;

    public DirectExternalCommunicationBenchmarks()
    {
        _incoming = null!;
        _outgoing = null!;
        TestCase = null!;
        _data = null!;
        _receivingTokenSource = null!;
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _data = TestDataGenerator.CreateExternalValues(TestCase);
        var channels = GetChannels(_data);
        _incoming = new(new NullLogger<DirectExternalIncomingCommunication>(),
            new()
            {
                Channels = channels,
                ConnectionUniqueIdentifier = "Service",
                EngineUniqueIdentifier = nameof(DirectExternalCommunicationBenchmarks),
            }, Substitute.For<INameResolver>(), AssemblyLoadContext.Default);
        _incoming.Received += Incoming_Received;
        _outgoing = new(
            new()
            {
                Channels = channels,
                ConnectionUniqueIdentifier = "Service",
                EngineUniqueIdentifier = nameof(DirectExternalCommunicationBenchmarks),
            }, new NullLogger<DirectExternalOutgoingCommunication>(), Substitute.For<INameResolver>(), AssemblyLoadContext.Default);
    }

    private static List<string> GetChannels(TestData<ExternalValue> data) => [.. data.EngineCycles.SelectMany(p => p.Values.Select(l => l.Channel)).Distinct()];

    private void Incoming_Received(IReadOnlyCollection<ExternalValue> externalValues)
    {
        _receivedValues.AddRange(externalValues);
        if (_receivedValues.Count == TestDataGenerator.Values)
            _receivingTokenSource.Cancel();
    }

    [GlobalCleanup]
    public void GlobalCleanup() => _incoming.Received -= Incoming_Received;

    [Benchmark]
    [SuppressMessage("Naming", "CA1707:Bezeichner dürfen keine Unterstriche enthalten", Justification = "Definiertes Pattern zur Extrahierung von TestCases")]
    public async Task Direct_External_IncomingAndOutgoing_Async()
    {
        _receivingTokenSource = new();

        await _outgoing.ConnectAsync(CancellationToken.None);
        await _incoming.ConnectAsync(CancellationToken.None);

        foreach (var engineCycle in _data.EngineCycles)
            await _outgoing.SendAsync(engineCycle.Number, engineCycle.Values, CancellationToken.None);

        try
        {
            await Task.Delay(TestDataGenerator.Values * 10, _receivingTokenSource.Token); // Assume a timeout of 10ms for each value
        }
        catch
        {
        }

        await _incoming.DisconnectAsync(CancellationToken.None);
        await _outgoing.DisconnectAsync(CancellationToken.None);
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
                _receivingTokenSource.Dispose();

            _disposedValue = true;
        }
    }
}
