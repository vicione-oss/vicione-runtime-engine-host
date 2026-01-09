using System;

namespace Microsoft.Extensions.Logging;

public record TestLogEntry(LogLevel LogLevel, string Message, Exception? Exception = default, EventId EventId = default);
