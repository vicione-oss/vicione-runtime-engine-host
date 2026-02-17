using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.EngineLog;

/// <summary>
/// Provides functionality for reading and writing JSON-formatted log entries to a file.
/// </summary>
public class JsonFileLog
{
    private readonly string _fileName;
    private readonly IFileSystem _fileSystem;

    internal JsonFileLog(string directory, string engineUniqueIdentifier, IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        _fileName = _fileSystem.Path.Combine(directory, $"{engineUniqueIdentifier}.json");
    }

    internal async Task AppendLogEntryAsync(LogEntry logEntry, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(logEntry, LogEntrySourceGenerationContext.Default.LogEntry);
        await _fileSystem.File.AppendAllLinesAsync(_fileName, [json,], cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads all log entries from the JSON log file.
    /// </summary>
    /// <returns>A collection of log entries read from the file.</returns>
    public async Task<IReadOnlyCollection<LogEntry>> ReadLogEntriesAsync()
    {
        List<LogEntry> logEntries = [];

        if (_fileSystem.File.Exists(_fileName))
        {
            await using var stream = _fileSystem.FileStream.New(_fileName, new FileStreamOptions()
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.ReadWrite,
                Options = FileOptions.Asynchronous,
            });
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) is not null)
            {
                var logEntry = JsonSerializer.Deserialize(line, LogEntrySourceGenerationContext.Default.LogEntry);
                if (logEntry is not null)
                    logEntries.Add(logEntry);
            }
        }

        return logEntries;
    }

    internal void CreateEmptyLog() => _fileSystem.File.Create(_fileName).Dispose();
}
