using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.ExternalCommunication;

internal static partial class DirectExternalCommunicationLog
{
    [LoggerMessage(1, LogLevel.Error,
        "Could not process message from '{ExternalConnectionId}' ({ExternalConnectionName}) for '{EngineId}' ({EngineName}).")]
    internal static partial void CannotReadLinkValue(this ILogger<DirectExternalIncomingCommunication> logger, string externalConnectionId,
        string externalConnectionName, string engineId, string engineName, Exception exception);

    [LoggerMessage(2, LogLevel.Trace,
        "Forwarding {ValueNumber} values from '{EngineId}' ({EngineName}) to '{ExternalConnectionId} '({ExternalConnectionName}).")]
    internal static partial void SendInformation(this ILogger<DirectExternalOutgoingCommunication> logger, int valueNumber, string engineId, string engineName,
        string externalConnectionId, string externalConnectionName);

    [LoggerMessage(3, LogLevel.Error,
        "Cannot send link values from '{EngineId}' ({EngineName}) to '{ExternalConnectionId}' ({ExternalConnectionName}).")]
    internal static partial void CannotSendLinkValues(this ILogger<DirectExternalOutgoingCommunication> logger, string engineId, string engineName,
        string externalConnectionId, string externalConnectionName, Exception exception);
}
