using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.Runtime;

internal static partial class CycleInfoServiceLog
{
    [LoggerMessage(1, LogLevel.Warning, "Skipped sending cycle report because the previous report is still being sent.")]
    internal static partial void CycleReportSkipped(this ILogger<CycleInfoService> logger);

    [LoggerMessage(2, LogLevel.Warning, "Skipped sending crash report because the previous report is still being sent.")]
    internal static partial void CrashReportSkipped(this ILogger<CycleInfoService> logger);
}
