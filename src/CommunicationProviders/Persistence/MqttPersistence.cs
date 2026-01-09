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

public sealed class MqttPersistence : ISettingsPersistence<MqttCommunication>, IVariablesPersistence<MqttCommunication>, IDisposable
{
    private readonly MqttQualityOfServiceLevel _qos;
    private readonly MqttCommunication _communication;
    private readonly IVirtualMqttClient _client;
    private readonly ILogger<MqttPersistence> _logger;
    private readonly INameResolver _nameResolver;
    private readonly JsonSerializerOptions _serializerOptions;

    public MqttPersistence(MqttCommunication communication, ILogger<MqttPersistence> logger, ILoggerFactory loggerFactory, INameResolver nameResolver,
        VariablesPersistenceOptions _, AssemblyLoadContext assemblyLoadContext)
        : this(communication, MqttOptimizer.Instance.Register(communication.ToCommunicationInfo(), loggerFactory), logger, nameResolver, assemblyLoadContext)
    { }

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

    public Task ConnectAsync(CancellationToken cancellationToken) => _client.Connect();

    public Task DisconnectAsync(CancellationToken cancellationToken) => _client.Disconnect();

    public Task<IReadOnlyCollection<PersistenceEntry>> LoadAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<PersistenceEntry>>([]);

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

    public void Dispose() => _client.Dispose();
}
