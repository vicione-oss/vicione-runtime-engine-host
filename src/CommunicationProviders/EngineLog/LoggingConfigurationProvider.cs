using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.EngineLog;

internal class LoggingConfigurationProvider : ConfigurationProvider
{
    internal LoggingConfigurationProvider()
    { }

    internal LoggingConfigurationProvider(LogLevel logLevel)
        => SetLevel(logLevel);

    internal void SetLevel(LogLevel logLevel)
    {
        Data["Logging:LogLevel:Default"] = Enum.GetName(logLevel);
        OnReload();
    }
}

