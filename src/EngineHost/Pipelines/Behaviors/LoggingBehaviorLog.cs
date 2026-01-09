using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.Pipelines.Behaviors;

internal static partial class LoggingBehaviorLog
{
    [LoggerMessage(0, LogLevel.Debug, "Handling '{RequestTypeName}' '{RequestContent}'...")]
    internal static partial void RequestHandling(this ILogger logger, string requestTypeName, string requestContent);

    [LoggerMessage(1, LogLevel.Debug, "Handled '{RequestTypeName}' in {DurationMilliseconds:0.00}ms with response '{ResponseTypeName}' '{ResponseContent}'")]
    internal static partial void RequestHandled(this ILogger logger, string requestTypeName, double durationMilliseconds, string responseTypeName, string responseContent);

    [LoggerMessage(2, LogLevel.Error, "Handling '{RequestTypeName}' failed in {DurationMilliseconds:0.00}ms")]
    internal static partial void RequestFailed(this ILogger logger, Exception exception, string requestTypeName, double durationMilliseconds);
}
