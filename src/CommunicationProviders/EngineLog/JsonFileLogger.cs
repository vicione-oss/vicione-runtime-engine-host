using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.EngineLog;

internal sealed class JsonFileLogger(
    string categoryName,
    Action<LogEntry> logMessage,
    IExternalScopeProvider? scopeProvider) : ILogger
{
    private readonly string _categoryName = categoryName;
    private readonly Action<LogEntry> _logMessage = logMessage;
    private readonly IExternalScopeProvider? _scopeProvider = scopeProvider;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _scopeProvider?.Push(state);

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel) &&
            LogEntryCreator.TryCreate(logLevel, eventId, state, exception, formatter, _categoryName, _scopeProvider, out var logEntry))
        {
            _logMessage(logEntry);
        }
    }
}
