using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.Communication;

namespace ViciOne.ManagedEngine.EngineLog;

/// <summary>
/// Factory for creating JSON file-based engine logging.
/// </summary>
/// <param name="logCommunication">The file system communication configuration.</param>
/// <param name="engine">The engine identifier.</param>
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

    /// <inheritdoc />
    public void AddLogging(IServiceCollection services, LogLevel logLevel)
    {
        _loggingProvider = new LoggingConfigurationProvider(logLevel);
        var configuration = new ConfigurationBuilder().Add(new LoggingConfigurationSource(_loggingProvider)).Build();

        services.AddLogging(c => c
            .AddConfiguration(configuration.GetSection("Logging"))
            .AddProvider(new JsonFileLoggerProvider(_settings)));
    }

    /// <inheritdoc />
    public void ChangeLogLevel(LogLevel logLevel)
    {
        if (_loggingProvider is null)
            throw new InvalidOperationException("Logger factory is not initialized.");

        _loggingProvider.SetLevel(logLevel);
    }
}
