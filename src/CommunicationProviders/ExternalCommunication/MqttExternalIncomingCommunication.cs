using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using MQTTnet.Extensions;
using MQTTnet.Protocol;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.Runtime;
using ViciOne.ManagedEngine.TypeResolution;

namespace ViciOne.ManagedEngine.ExternalCommunication;

/// <summary>
/// Handles MQTT-based incoming external communication.
/// </summary>
public sealed class MqttExternalIncomingCommunication : IExternalIncomingCommunication<MqttCommunication>, IDisposable
{
    private readonly MqttCommunication _communication;
    private readonly MqttQualityOfServiceLevel _qos;
    private readonly ILogger<MqttExternalIncomingCommunication> _logger;
    private readonly ExternalIncomingCommunicationOptions _options;
    private readonly IVirtualMqttClient _client;
    private readonly INameResolver _nameResolver;
    private readonly AssemblyLoadContext _assemblyLoadContext;
    private readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttExternalIncomingCommunication"/> class.
    /// </summary>
    /// <param name="communication">The MQTT communication configuration.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="options">The incoming communication options.</param>
    /// <param name="nameResolver">The name resolver for resolving identifiers.</param>
    /// <param name="assemblyLoadContext">The assembly load context.</param>
    public MqttExternalIncomingCommunication(MqttCommunication communication, ILogger<MqttExternalIncomingCommunication> logger,
        ILoggerFactory loggerFactory, ExternalIncomingCommunicationOptions options, INameResolver nameResolver, AssemblyLoadContext assemblyLoadContext)
        : this(communication, logger, options, MqttOptimizer.Instance.Register(communication.ToCommunicationInfo(), loggerFactory), nameResolver, assemblyLoadContext)
    { }

    internal MqttExternalIncomingCommunication(MqttCommunication communication, ILogger<MqttExternalIncomingCommunication> logger,
        ExternalIncomingCommunicationOptions options, IVirtualMqttClient client, INameResolver nameResolver, AssemblyLoadContext assemblyLoadContext)
    {
        _communication = communication;
        _qos = (MqttQualityOfServiceLevel)communication.QualityOfService;
        _logger = logger;
        _options = options;
        _client = client;
        _client.MessageReceived += MessageReceivedAsync;
        _nameResolver = nameResolver;
        _assemblyLoadContext = assemblyLoadContext;
        _serializerOptions = JsonSetup.CreatePreserveTypeOptions(assemblyLoadContext);
    }

    /// <inheritdoc />
    public event Action<IReadOnlyCollection<ExternalValue>>? Received;

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        using (_logger.ConnectionConnect(_options.ConnectionUniqueIdentifier, _nameResolver.ResolveName(_options.ConnectionUniqueIdentifier),
            _communication.DescribeEndpoint()))
        {
            await _client.Connect().ConfigureAwait(false);
        }

        using (_logger.TopicsSubscribe(string.Join(", ", _options.Channels.Select(c => $"'{c}'"))))
        {
            foreach (var channel in _options.Channels)
                await _client.Subscribe(channel, _qos, true).ConfigureAwait(false);
        }
    }

    private Task MessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        _logger.MessageReceived(_options.ConnectionUniqueIdentifier, _nameResolver.ResolveName(_options.ConnectionUniqueIdentifier), e.ClientId,
            e.ApplicationMessage.Topic);

        ExternalValue? value;
        try
        {
            ArgumentNullException.ThrowIfNull(e.ApplicationMessage.UserProperties, nameof(e.ApplicationMessage.UserProperties));
            value = new()
            {
                Timestamp = e.ApplicationMessage.UserProperties.FindRequired(MqttUserProperties.Timestamp).GetDateTime(),
                Validity = e.ApplicationMessage.UserProperties.FindRequired(MqttUserProperties.Validity).Get<int>(),
                Channel = e.ApplicationMessage.Topic,
            };
            if (e.ApplicationMessage.PayloadSegment.Count > 0)
            {
                value.Value = JsonSerializer.Deserialize(
                    e.ApplicationMessage.PayloadSegment,
                    TypeResolver.GetType(e.ApplicationMessage.UserProperties.FindRequired(MqttUserProperties.Type).Value, _assemblyLoadContext),
                    _serializerOptions);
            }
        }
        catch (Exception exception)
        {
            value = null;
            _logger.MessageNotProcessable(exception, _options.ConnectionUniqueIdentifier, _nameResolver.ResolveName(_options.ConnectionUniqueIdentifier),
                e.ClientId, e.ApplicationMessage.Topic);
        }
        if (value != null)
            Received?.Invoke([value,]);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        using (_logger.TopicsUnsubscribe())
        {
            foreach (var channel in _options.Channels)
                await _client.Unsubscribe(channel).ConfigureAwait(false);
        }

        using (_logger.ConnectionDisconnect(_options.ConnectionUniqueIdentifier, _nameResolver.ResolveName(_options.ConnectionUniqueIdentifier),
            _communication.DescribeEndpoint()))
        {
            await _client.Disconnect().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _client.MessageReceived -= MessageReceivedAsync;
        _client.Dispose();
    }
}
