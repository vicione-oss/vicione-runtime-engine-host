using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.Persistence;

internal static partial class FileSystemPersistenceLog
{
    [LoggerMessage(1, LogLevel.Warning, "Read Persistence-Entry '{PersistenceEntryId}' ({PersistenceEntryName}) with some trouble.")]
    internal static partial void ReadEntriesWithSomeTrouble(this ILogger<FileSystemPersistence> logger, string persistenceEntryId, string persistenceEntryName,
        Exception exception);

    [LoggerMessage(2, LogLevel.Trace, "Persistence-Entry '{PersistenceEntryId}' ({PersistenceEntryName}) successfully loaded.")]
    internal static partial void PersistenceEntryLoaded(this ILogger<FileSystemPersistence> logger, string persistenceEntryId, string persistenceEntryName);

    [LoggerMessage(3, LogLevel.Trace, "Persistence-Entry '{PersistenceEntryId}' ({PersistenceEntryName}) successfully saved.")]
    internal static partial void PersistenceEntrySaved(this ILogger<FileSystemPersistence> logger, string persistenceEntryId, string persistenceEntryName);

    [LoggerMessage(4, LogLevel.Error, "Cannot save persistence entry in file '{PersistenceEntryFilename}'.")]
    internal static partial void PersistenceEntryNotSaved(this ILogger<FileSystemPersistence> logger, string persistenceEntryFilename, Exception exception);

}
