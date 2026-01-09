using System;
using System.Buffers;
using System.Globalization;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.ManagedEngine.Persistence;

namespace Benchmarks.Communication;

internal static class TestDataGenerator
{
    /// <summary>
    /// The number of values for one invocation of BenchmarkDotNet.
    /// </summary>
    /// <remarks>
    /// BenchmarkDotNet controls the number of invocations itself to obtain good measurements.
    /// </remarks>
    internal const int Values = 10;

    internal static TestData<ExternalValue> CreateExternalValues(TestCase testCase)
        => Create(testCase, (timestamp, id, validity, value) => new ExternalValue
        {
            Timestamp = timestamp,
            Channel = id,
            Validity = validity,
            Value = value,
        });

    internal static TestData<PersistenceEntry> CreatePersistenceEntries(TestCase testCase)
        => Create(testCase, (time, id, _, value) => new PersistenceEntry
        {
            Time = time,
            UniqueIdentifier = id,
            Value = value,
        });

    private static TestData<T> Create<T>(TestCase testCase, Func<DateTime, string, int, object, T> createDataElement)
    {
        var links = (int)(Values * testCase.LinkUsage);
        var linkUIds = ArrayPool<string>.Shared.Rent(links);
        for (var i = 0; i < links; i++)
            linkUIds[i] = i.ToString(CultureInfo.InvariantCulture);

        var partitions = (int)Math.Ceiling((double)Values / links);
        TestData<T> data = new();
        for (var p = 0; p < partitions; p++)
            data.EngineCycles.Add(new((ulong)p));

        var startTimestamp = DateTime.Now;
        for (var v = 0; v < Values; v++)
        {
            var partition = v / links;
            data.EngineCycles[partition].Values.Add(createDataElement(startTimestamp.AddSeconds(partition), linkUIds[v % links], v % 2 == 0 ? 0 : 1, v));
        }

        ArrayPool<string>.Shared.Return(linkUIds);

        return data;
    }
}
