using Microsoft.Extensions.Configuration;

namespace ViciOne.ManagedEngine.EngineLog;

internal class LoggingConfigurationSource : IConfigurationSource
{
    private readonly LoggingConfigurationProvider _loggingConfigurationProvider;

    internal LoggingConfigurationSource(LoggingConfigurationProvider loggingConfigurationProvider)
        => _loggingConfigurationProvider = loggingConfigurationProvider;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => _loggingConfigurationProvider;
}

