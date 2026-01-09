using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.Persistence;

internal static partial class MqttPersistenceLog
{
    [LoggerMessage(1, LogLevel.Trace, "Send persistence entry '{PersistenceEntryId}' ({PersistenceEntryName}) at '{Topic}'")]
    internal static partial void PersistenceEntrySend(this ILogger logger, string persistenceEntryId, string persistenceEntryName, string topic);

    [LoggerMessage(2, LogLevel.Error, "Could not send persistence entry '{PersistenceEntryId}' ({PersistenceEntryName}) at '{Topic}'")]
    internal static partial void PersistenceEntryNotSend(this ILogger logger, Exception ex, string persistenceEntryId, string persistenceEntryName, string topic);
}
