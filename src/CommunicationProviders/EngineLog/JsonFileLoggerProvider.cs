using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Testably.Abstractions;

namespace ViciOne.ManagedEngine.EngineLog;

[ProviderAlias("JsonFile")]
internal sealed class JsonFileLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ConcurrentDictionary<string, JsonFileLogger> _loggers;
    private readonly JsonFileLog _jsonFileLog;
    private IExternalScopeProvider? _scopeProvider;
    private readonly Channel<LogEntry> _messageQueue = Channel.CreateUnbounded<LogEntry>();
    private readonly Task _outputTask;


    public JsonFileLoggerProvider(JsonFileSettings settings) : this(settings, new RealFileSystem())
    { }

    internal JsonFileLoggerProvider(JsonFileSettings settings, IFileSystem fileSystem)
    {
        _loggers = new ConcurrentDictionary<string, JsonFileLogger>();
        _jsonFileLog = new JsonFileLog(settings.Directory, settings.EngineUniqueIdentifier, fileSystem);
        _outputTask = Task.Factory.StartNew(ProcessLogQueueAsync, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
    }

    public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, InitializeLogger);

    private JsonFileLogger InitializeLogger(string categoryName)
        => new(categoryName, Log, _scopeProvider);

    public void Dispose()
    {
        _messageQueue.Writer.Complete();
        _outputTask.Wait();
        _outputTask.Dispose();
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

    private void Log(LogEntry logEntry)
        => _messageQueue.Writer.TryWrite(logEntry);

    private async Task ProcessLogQueueAsync()
    {
        _jsonFileLog.CreateEmptyLog();
        await foreach (var logEntry in _messageQueue.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            try
            {
                await _jsonFileLog.AppendLogEntryAsync(logEntry, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            { }
        }
    }
}
