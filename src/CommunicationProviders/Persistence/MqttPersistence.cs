using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Extensions;
using MQTTnet.Protocol;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.Runtime;

namespace ViciOne.ManagedEngine.Persistence;

/// <summary>
/// Provides MQTT-based persistence for settings and variables.
/// </summary>
public sealed class MqttPersistence : ISettingsPersistence<MqttCommunication>, IVariablesPersistence<MqttCommunication>, IDisposable
{
    private readonly MqttQualityOfServiceLevel _qos;
    private readonly MqttCommunication _communication;
    private readonly IVirtualMqttClient _client;
    private readonly ILogger<MqttPersistence> _logger;
    private readonly INameResolver _nameResolver;
    private readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttPersistence"/> class for variables persistence.
    /// </summary>
    /// <param name="communication">The MQTT communication configuration.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="nameResolver">The name resolver for resolving identifiers.</param>
    /// <param name="_">The variables persistence options.</param>
    /// <param name="assemblyLoadContext">The assembly load context.</param>
    public MqttPersistence(MqttCommunication communication, ILogger<MqttPersistence> logger, ILoggerFactory loggerFactory, INameResolver nameResolver,
        VariablesPersistenceOptions _, AssemblyLoadContext assemblyLoadContext)
        : this(communication, MqttOptimizer.Instance.Register(communication.ToCommunicationInfo(), loggerFactory), logger, nameResolver, assemblyLoadContext)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttPersistence"/> class for settings persistence.
    /// </summary>
    /// <param name="communication">The MQTT communication configuration.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="nameResolver">The name resolver for resolving identifiers.</param>
    /// <param name="_">The settings persistence options.</param>
    /// <param name="assemblyLoadContext">The assembly load context.</param>
    public MqttPersistence(MqttCommunication communication, ILogger<MqttPersistence> logger, ILoggerFactory loggerFactory, INameResolver nameResolver,
        SettingsPersistenceOptions _, AssemblyLoadContext assemblyLoadContext)
        : this(communication, MqttOptimizer.Instance.Register(communication.ToCommunicationInfo(), loggerFactory), logger, nameResolver, assemblyLoadContext)
    { }

    internal MqttPersistence(MqttCommunication communication, IVirtualMqttClient client, ILogger<MqttPersistence> logger, INameResolver nameResolver, AssemblyLoadContext assemblyLoadContext)
    {
        _qos = (MqttQualityOfServiceLevel)communication.QualityOfService;
        _communication = communication;
        _client = client;
        _logger = logger;
        _nameResolver = nameResolver;
        _serializerOptions = JsonSetup.CreatePreserveTypeOptions(assemblyLoadContext);
    }

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken cancellationToken) => _client.Connect();

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken) => _client.Disconnect();

    /// <inheritdoc />
    public Task<IReadOnlyCollection<PersistenceEntry>> LoadAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<PersistenceEntry>>([]);

    /// <inheritdoc />
    public async Task SaveAsync(IEnumerable<PersistenceEntry> persistenceEntries, CancellationToken cancellationToken)
    {
        foreach (var persistenceEntry in persistenceEntries)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            await SaveAsync(persistenceEntry).ConfigureAwait(false);
        }
    }

    private async Task SaveAsync(PersistenceEntry persistenceEntry)
    {
        var topic = persistenceEntry.UniqueIdentifier;
        try
        {
            var type = persistenceEntry.Value?.GetType() ?? typeof(object);
            var message = CreateDefaultBuilder()
                .WithTopic(topic)
                .WithJsonPayload(JsonSerializer.SerializeToUtf8Bytes(persistenceEntry.Value, type, _serializerOptions))
                .WithUserProperty(MqttUserProperties.Timestamp, persistenceEntry.Time)
                .WithUserProperty(MqttUserProperties.Type, type.AssemblyQualifiedName)
                .Build();
            await _client.Publish(message).ConfigureAwait(false);
            _logger.PersistenceEntrySend(persistenceEntry.UniqueIdentifier, _nameResolver.ResolveName(persistenceEntry.UniqueIdentifier), topic);
        }
        catch (Exception ex)
        {
            _logger.PersistenceEntryNotSend(ex, persistenceEntry.UniqueIdentifier, _nameResolver.ResolveName(persistenceEntry.UniqueIdentifier), topic);
        }
    }

    private MqttApplicationMessageBuilder CreateDefaultBuilder()
        => new MqttApplicationMessageBuilder()
            .WithQualityOfServiceLevel(_qos)
            .WithRetainFlag(_communication.Retain)
            .WithMessageExpiryInterval(_communication.MessageExpiryInterval);

    /// <inheritdoc />
    public void Dispose() => _client.Dispose();
}
