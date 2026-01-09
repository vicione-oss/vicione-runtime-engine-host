using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.Runtime;

internal static partial class LifetimeServiceLog
{
    [LoggerMessage(0, LogLevel.Information, "Engine host {EngineHostVersion} is up and running")]
    internal static partial void UpAndRunning(this ILogger<LifetimeService> logger, string engineHostVersion);
}
