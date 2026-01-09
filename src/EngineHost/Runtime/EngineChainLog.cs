using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.Runtime;

internal static partial class EngineChainLog
{
    [LoggerMessage(1, LogLevel.Warning, "Reporting of engine chain results failed.")]
    internal static partial void ReportChainResultsFailed(this ILogger logger, Exception exception);

    [LoggerMessage(2, LogLevel.Error, "Execution of engine chain link failed at position {Index}.")]
    internal static partial void ChainLinkExecutionFailure(this ILogger logger, int index, Exception exception);

    [LoggerMessage(3, LogLevel.Warning, "The cycle took {DurationMilliseconds:0.00}ms and skipped {SkippedCycles} cycles.{ChainLinkDurations}")]
    internal static partial void CycleTimeExceeded(this ILogger logger, double durationMilliseconds, int skippedCycles, string chainLinkDurations);

    [LoggerMessage(4, LogLevel.Debug, "Added chain link '{ChainLinkId}' at position {Position}.")]
    internal static partial void AddedChainLink(this ILogger logger, string chainLinkId, int position);

    [LoggerMessage(5, LogLevel.Debug, "Removed chain link '{ChainLinkId}'.")]
    internal static partial void RemovedChainLink(this ILogger logger, string chainLinkId);

    [LoggerMessage(6, LogLevel.Debug, "Enabled chain link '{ChainLinkId}'.")]
    internal static partial void EnabledChainLink(this ILogger logger, string chainLinkId);

    [LoggerMessage(7, LogLevel.Debug, "Disabled chain link '{ChainLinkId}'.")]
    internal static partial void DisabledChainLink(this ILogger logger, string chainLinkId);
}
