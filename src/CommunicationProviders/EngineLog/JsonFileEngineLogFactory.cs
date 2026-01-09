using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.Communication;

namespace ViciOne.ManagedEngine.EngineLog;

public class JsonFileEngineLogFactory(
    FileSystemCommunication logCommunication,
    string engine) : IEngineLogFactory<FileSystemCommunication>
{
    private readonly JsonFileSettings _settings = MapSettings(logCommunication, engine);
    private LoggingConfigurationProvider? _loggingProvider;

    private static JsonFileSettings MapSettings(FileSystemCommunication logCommunication, string engineId)
        => new()
        {
            Directory = logCommunication.Directory,
            EngineUniqueIdentifier = engineId,
        };

    public void AddLogging(IServiceCollection services, LogLevel logLevel)
    {
        _loggingProvider = new LoggingConfigurationProvider(logLevel);
        var configuration = new ConfigurationBuilder().Add(new LoggingConfigurationSource(_loggingProvider)).Build();

        services.AddLogging(c => c
            .AddConfiguration(configuration.GetSection("Logging"))
            .AddProvider(new JsonFileLoggerProvider(_settings)));
    }

    public void ChangeLogLevel(LogLevel logLevel)
    {
        if (_loggingProvider is null)
            throw new InvalidOperationException("Logger factory is not initialized.");

        _loggingProvider.SetLevel(logLevel);
    }
}
