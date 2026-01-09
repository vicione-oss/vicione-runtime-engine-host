using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.Runtime;

internal static partial class DeploymentsServiceLog
{
    [LoggerMessage(1, LogLevel.Information, "Recovered {RecoveredDeployments} of {TotalDeployments} deployments. {FailedDeployments} failed.")]
    internal static partial void RecoveredDeployments(this ILogger<DeploymentsService> logger,
        int recoveredDeployments, int failedDeployments, int totalDeployments);

    [LoggerMessage(2, LogLevel.Debug, "Deployment '{Deployment}' recovered at chain index {ChainIndex}.")]
    internal static partial void RecoveredDeployment(this ILogger<DeploymentsService> logger, string deployment, int chainIndex);

    [LoggerMessage(3, LogLevel.Error, "Deployment '{Deployment}' cannot be recovered.")]
    internal static partial void RecoverDeploymentFailed(this ILogger<DeploymentsService> logger, string deployment, Exception exception);

    [LoggerMessage(20, LogLevel.Information, "Stopped {StoppedDeployments} of {TotalDeployments} deployments. {FailedDeployments} failed.")]
    internal static partial void StoppedDeployments(this ILogger<DeploymentsService> logger,
        int stoppedDeployments, int failedDeployments, int totalDeployments);

    [LoggerMessage(21, LogLevel.Debug, "Deployment '{Deployment}' stopped.")]
    internal static partial void StoppedDeployment(this ILogger<DeploymentsService> logger, string deployment);

    [LoggerMessage(22, LogLevel.Error, "Deployment '{Deployment}' cannot be stopped.")]
    internal static partial void StopDeploymentFailed(this ILogger<DeploymentsService> logger, string deployment, Exception exception);
}
