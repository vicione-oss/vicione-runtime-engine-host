using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using ViciOne.ManagedEngine.Communication;

namespace ViciOne.ManagedEngine.EngineLog;

/// <summary>
/// Factory for creating Serilog-based syslog engine logging.
/// </summary>
/// <param name="logCommunication">The syslog communication configuration.</param>
public class SerilogSyslogEngineLogFactory(SerilogSyslogCommunication logCommunication) : IEngineLogFactory<SerilogSyslogCommunication>
{
    private readonly SerilogSyslogCommunication _logCommunication = logCommunication;
    private LoggingLevelSwitch? _logLevelSwitch;

    /// <inheritdoc />
    public void AddLogging(IServiceCollection services, LogLevel logLevel)
    {
        _logLevelSwitch = new LoggingLevelSwitch(TranslateLogLevel(logLevel));

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(_logLevelSwitch)
            .WriteTo.LocalSyslog(_logCommunication.AppName, outputTemplate: _logCommunication.OutputTemplate);

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
