using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.EngineLog;

internal static class LogEntryCreator
{
    internal static bool TryCreate<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter, string categoryName, IExternalScopeProvider? scopeProvider,
        [NotNullWhen(true)] out LogEntry? logEntry)
    {
        var message = default(string);
        message = formatter(state, exception);
        if (!string.IsNullOrEmpty(message) || (exception != null))
        {
            var lines = new List<string>
                {
                    $"{categoryName}[{eventId}]",
                };
            if (scopeProvider != null)
            {
                var scopeInfo = new List<string?>();
                scopeProvider.ForEachScope((scope, si) => si.Add(scope?.ToString()), scopeInfo);
                if (scopeInfo.Count > 0)
                    lines.Add("=> " + string.Join(" => ", scopeInfo.Where(si => !string.IsNullOrEmpty(si))));
            }
            if (!string.IsNullOrEmpty(message))
                lines.Add(message);
            if (exception != null)
                lines.Add(exception.ToString());
            var entry = new LogEntry
            {
                Text = string.Join(Environment.NewLine, lines),
                Timestamp = DateTime.UtcNow,
                Type = logLevel,
            };

            logEntry = entry;
            return true;
        }

        logEntry = null;
        return false;
    }
}
