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

namespace ViciOne.ManagedEngine.ExternalCommunication;

public sealed class MqttExternalOutgoingCommunication : IExternalOutgoingCommunication<MqttCommunication>, IDisposable
{
    private readonly MqttCommunication _communication;
    private readonly MqttQualityOfServiceLevel _qos;
    private readonly ExternalOutgoingCommunicationOptions _options;
    private readonly IVirtualMqttClient _client;
    private readonly ILogger<MqttExternalOutgoingCommunication> _logger;
    private readonly INameResolver _nameResolver;
    private readonly JsonSerializerOptions _serializerOptions;

    public MqttExternalOutgoingCommunication(MqttCommunication communication, ILogger<MqttExternalOutgoingCommunication> logger, ILoggerFactory loggerFactory,
        ExternalOutgoingCommunicationOptions options, INameResolver nameResolver, AssemblyLoadContext assemblyLoadContext)
        : this(communication, options, MqttOptimizer.Instance.Register(communication.ToCommunicationInfo(), loggerFactory), logger, nameResolver, assemblyLoadContext)
    { }

    internal MqttExternalOutgoingCommunication(MqttCommunication communication, ExternalOutgoingCommunicationOptions options, IVirtualMqttClient client,
        ILogger<MqttExternalOutgoingCommunication> logger, INameResolver nameResolver, AssemblyLoadContext assemblyLoadContext)
    {
        _communication = communication;
        _qos = (MqttQualityOfServiceLevel)_communication.QualityOfService;
        _options = options;
        _client = client;
        _logger = logger;
        _nameResolver = nameResolver;
        _serializerOptions = JsonSetup.CreatePreserveTypeOptions(assemblyLoadContext);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        using (_logger.ConnectionConnect(_options.ConnectionUniqueIdentifier, _nameResolver.ResolveName(_options.ConnectionUniqueIdentifier),
            _communication.DescribeEndpoint()))
        {
            await _client.Connect().ConfigureAwait(false);
        }
    }

    public async Task SendAsync(ulong engineCycle, IReadOnlyCollection<ExternalValue> values, CancellationToken cancellationToken)
    {
        foreach (var value in values)
        {
            try
            {
                var type = value.Value?.GetType() ?? typeof(object);
                var message = new MqttApplicationMessageBuilder()
                    .WithQualityOfServiceLevel(_qos)
                    .WithMessageExpiryInterval(_communication.MessageExpiryInterval)
                    .WithRetainFlag(_communication.Retain)
                    .WithTopic(value.Channel)
                    .WithJsonPayload(JsonSerializer.SerializeToUtf8Bytes(value.Value, type, _serializerOptions))
                    .WithUserProperty(MqttUserProperties.Timestamp, value.Timestamp)
                    .WithUserProperty(MqttUserProperties.Validity, value.Validity)
                    .WithUserProperty(MqttUserProperties.EngineCycle, engineCycle)
                    .WithUserProperty(MqttUserProperties.Type, type.AssemblyQualifiedName)
                    .Build();
                await _client.Publish(message).ConfigureAwait(false);
                _logger.MessageSend(_options.ConnectionUniqueIdentifier, _nameResolver.ResolveName(_options.ConnectionUniqueIdentifier), value.Channel);
            }
            catch (Exception ex)
            {
                _logger.MessageNotSend(ex, _options.ConnectionUniqueIdentifier, _nameResolver.ResolveName(_options.ConnectionUniqueIdentifier),
                    value.Channel);
            }
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        using (_logger.ConnectionDisconnect(_options.ConnectionUniqueIdentifier, _nameResolver.ResolveName(_options.ConnectionUniqueIdentifier),
            _communication.DescribeEndpoint()))
        {
            await _client.Disconnect().ConfigureAwait(false);
        }
    }

    public void Dispose() => _client.Dispose();
}
