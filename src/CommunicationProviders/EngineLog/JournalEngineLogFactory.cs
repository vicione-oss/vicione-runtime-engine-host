using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Journal;
using ViciOne.ManagedEngine.Communication;

namespace ViciOne.ManagedEngine.EngineLog;

/// <summary>
/// Factory for creating systemd journal-based engine logging.
/// </summary>
/// <param name="logCommunication">The journal communication configuration.</param>
public sealed class JournalEngineLogFactory(JournalCommunication logCommunication) : IEngineLogFactory<JournalCommunication>
{
    private LoggingLevelSwitch? _logLevelSwitch;
    private readonly JournalCommunication _communication = logCommunication;

    /// <inheritdoc />
    public void AddLogging(IServiceCollection services, LogLevel logLevel)
    {
        _logLevelSwitch = new LoggingLevelSwitch(TranslateLogLevel(logLevel));

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(_logLevelSwitch)
            .WriteTo.Journal(null, _communication.OutputTemplate ?? "{Message:lj}", _communication.MetaDataFields);

        var logger = loggerConfiguration.CreateLogger();
        services.AddLogging(c => c.AddSerilog(logger, true));
    }

    /// <inheritdoc />
    public void ChangeLogLevel(LogLevel logLevel)
    {
        if (_logLevelSwitch is null)
            throw new InvalidOperationException("Logger factory is not initialized.");

        _logLevelSwitch.MinimumLevel = TranslateLogLevel(logLevel);
    }

    private static LogEventLevel TranslateLogLevel(LogLevel logLevel)
        => logLevel switch
        {
            LogLevel.None or LogLevel.Critical => LogEventLevel.Fatal,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Verbose,
        };
}
