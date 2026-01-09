using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ViciOne.ManagedEngine.Runtime;

internal static class EngineChainTelemetry
{
    private const string Prefix = "engine-chain";
    private const string ActivityNameCycle = $"{ActivitySourceName}.{nameof(EngineChain.ProcessChainLinks)}";
    private const string ActivityNameChainLink = $"{ActivitySourceName}.ProcessChainLink";

    internal const string ActivitySourceName = "ViciOne.ManagedEngine.Runtime.EngineChain";
    internal const string MeterName = ActivitySourceName;

    internal const string TagChainLinkCount = $"{Prefix}.link.count";
    internal const string TagChainLinkId = $"{Prefix}.link.id";
    internal const string TagChainLinkIndex = $"{Prefix}.link.index";

    internal static ActivitySource ActivitySource { get; } = new(ActivitySourceName);
    internal static Meter Meter { get; } = new(MeterName);

    internal static Histogram<double> ChainLinkDuration { get; } = Meter.CreateHistogram<double>(
        name: $"{Prefix}.link.duration",
        unit: "ms",
        description: "Execution duration of an engine chain link.");

    internal static Histogram<double> CycleDuration { get; } = Meter.CreateHistogram<double>(
        name: $"{Prefix}.cycle.duration",
        unit: "ms",
        description: "Duration of an engine chain execution cycle.");

    internal static Counter<long> CycleSkippedCount { get; } = Meter.CreateCounter<long>(
        name: $"{Prefix}.cycle.skipped",
        description: "Number of engine chain execution cycles that were skipped due to previous cycle overruns.");

    internal static Counter<long> ChainLinkCrashCount { get; } = Meter.CreateCounter<long>(
        name: $"{Prefix}.link.crashes",
        description: "Number of engine chain link executions that resulted in an exception.");

    internal static Activity? StartCycleActivity(int linkCount)
    {
        var activity = ActivitySource.StartActivity(ActivityNameCycle);
        activity?.SetTag(TagChainLinkCount, linkCount);
        return activity;
    }

    internal static Activity? StartChainLinkActivity(string id, int index)
    {
        var activity = ActivitySource.StartActivity(ActivityNameChainLink);
        activity?.SetTag(TagChainLinkId, id);
        activity?.SetTag(TagChainLinkIndex, index);
        return activity;
    }

    internal static void RecordChainLinkDuration(string id, double duration)
        => ChainLinkDuration.Record(duration, [new(TagChainLinkId, id)]);

    internal static void RecordCycleDuration(double duration)
        => CycleDuration.Record(duration);

    internal static void IncreaseCycleSkippedCount(int count)
        => CycleSkippedCount.Add(count);

    internal static void IncreaseChainLinkCrashCount(string id)
        => ChainLinkCrashCount.Add(1, [new(TagChainLinkId, id)]);
}
