using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.Runtime;

internal static partial class UnixTimerLog
{
    [LoggerMessage(0, LogLevel.Critical, "Waiting for next timer expiry failed.")]
    public static partial void LogWaitForNextExpiryFailed(this ILogger logger, Exception exception);
}
