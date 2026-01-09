using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine;

internal sealed class LogContext : IDisposable
{
    private readonly ILogger _logger;
    private readonly Action<ILogger> _after;

    internal LogContext(ILogger logger, Action<ILogger> after)
    {
        _logger = logger;
        _after = after;
    }

    public void Dispose() => _after(_logger);
}

internal sealed class LogContext<T> : IDisposable
{
    private readonly ILogger<T> _logger;
    private readonly Action<ILogger<T>> _after;

    internal LogContext(ILogger<T> logger, Action<ILogger<T>> after)
    {
        _logger = logger;
        _after = after;
    }

    public void Dispose() => _after(_logger);
}

internal static class ILoggerExtensions
{
    internal static LogContext CreateContext(this ILogger logger, Action<ILogger> before, Action<ILogger> after)
    {
        before(logger);
        return new(logger, after);
    }

    internal static LogContext<T> CreateContext<T>(this ILogger<T> logger, Action<ILogger<T>> before, Action<ILogger<T>> after)
    {
        before(logger);
        return new(logger, after);
    }
}
