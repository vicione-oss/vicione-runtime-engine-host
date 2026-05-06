using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.Runtime;

internal static partial class ContextPoolLog
{
    [LoggerMessage(1, LogLevel.Information, "Created new context with id '{ContextId}' for deployment '{DeploymentId}'.")]
    internal static partial void CreatedContext(this ILogger<ContextPool> logger, string contextId, string deploymentId);

    [LoggerMessage(2, LogLevel.Debug, "Used existing context with id '{ContextId}' for deployment '{DeploymentId}'.")]
    internal static partial void UsedExistingContext(this ILogger<ContextPool> logger, string contextId, string deploymentId);

    [LoggerMessage(3, LogLevel.Debug, "Unload of context '{ContextId}' initiated. Last used by deployment '{DeploymentId}'.")]
    internal static partial void ContextUnloadInitiated(this ILogger<ContextPool> logger, string contextId, string deploymentId);

    [LoggerMessage(EventId = 4, Message = "Context '{ContextId}' is still alive. Last used by deployment '{DeploymentId}'.")]
    internal static partial void ContextIsStillAlive(this ILogger<ContextPool> logger, LogLevel level, string contextId, string deploymentId);

    [LoggerMessage(5, LogLevel.Information, "Context '{ContextId}' is fully unloaded. Last used by deployment '{DeploymentId}'.")]
    internal static partial void ContextIsUnloaded(this ILogger<ContextPool> logger, string contextId, string deploymentId);

    [LoggerMessage(8, LogLevel.Debug, "Remove deployment '{DeploymentId}' from context '{ContextId}'.")]
    internal static partial void RemoveConsumer(this ILogger<ContextPool> logger, string contextId, string deploymentId);

    [LoggerMessage(9, LogLevel.Warning, "Context '{ContextId}' leaked: native handles will not be freed.")]
    internal static partial void ContextLeakedNativeHandlesNotFreed(this ILogger<ContextPool> logger, string contextId);

    [LoggerMessage(10, LogLevel.Warning, "Context '{ContextId}': Cannot free a native library.")]
    internal static partial void FreeNativeLibraryFailed(this ILogger<ContextPool> logger, Exception exception, string contextId);
}
