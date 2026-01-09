using System;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using ViciOne.ManagedEngine.Communication;

namespace ViciOne.ManagedEngine.EngineLog;

public class SerilogFileEngineLogFactory(SeriLogFileCommunication logCommunication) : IEngineLogFactory<SeriLogFileCommunication>
{
    private readonly SeriLogFileCommunication _logCommunication = logCommunication;
    private LoggingLevelSwitch? _logLevelSwitch;

    public void AddLogging(IServiceCollection services, LogLevel logLevel)
    {
        _logLevelSwitch = new LoggingLevelSwitch(TranslateLogLevel(logLevel));

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(_logLevelSwitch);
        if (_logCommunication.OutputTemplate is null)
        {
            loggerConfiguration = loggerConfiguration.WriteTo.File(_logCommunication.Path,
                formatProvider: CultureInfo.InvariantCulture, shared: true);
        }
        else
        {
            loggerConfiguration = loggerConfiguration.WriteTo.File(_logCommunication.Path,
                outputTemplate: _logCommunication.OutputTemplate,
                formatProvider: CultureInfo.InvariantCulture, shared: true);
        }

        var logger = loggerConfiguration.CreateLogger();
        services.AddLogging(c => c.AddSerilog(logger, true));
    }

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
