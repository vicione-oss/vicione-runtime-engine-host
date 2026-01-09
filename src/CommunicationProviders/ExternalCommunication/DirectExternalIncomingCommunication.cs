using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.Runtime;

namespace ViciOne.ManagedEngine.ExternalCommunication;

public sealed class DirectExternalIncomingCommunication : IExternalIncomingCommunication<DirectCommunication>
{
    private readonly ILogger<DirectExternalIncomingCommunication> _logger;
    private readonly ExternalIncomingCommunicationOptions _options;
    private readonly INameResolver _nameResolver;
    private readonly DirectExchange _exchange;
    private readonly SubscriberInfo _subscriber;
    private readonly JsonSerializerOptions _serializerOptions;

    public DirectExternalIncomingCommunication(ILogger<DirectExternalIncomingCommunication> logger, ExternalIncomingCommunicationOptions options,
        INameResolver nameResolver, AssemblyLoadContext assemblyLoadContext)
        : this(logger, DirectExchange.Instance, options, nameResolver, assemblyLoadContext)
    {
    }

    internal DirectExternalIncomingCommunication(ILogger<DirectExternalIncomingCommunication> logger, DirectExchange exchange,
        ExternalIncomingCommunicationOptions options, INameResolver nameResolver, AssemblyLoadContext assemblyLoadContext)
    {
        _logger = logger;
        _exchange = exchange;
        _options = options;
        _nameResolver = nameResolver;
        _subscriber = new(assemblyLoadContext.GetHashCode(), HandleValueAbstractly, HandleValueDirectly);
        _serializerOptions = JsonSetup.CreatePreserveTypeOptions(assemblyLoadContext);
    }

    public event Action<IReadOnlyCollection<ExternalValue>>? Received;

    public Task ConnectAsync(CancellationToken cancellationToken)
        => _exchange.SubscribeAsync(_options.Channels, _subscriber);

    public Task DisconnectAsync(CancellationToken cancellationToken)
        => _exchange.UnsubscribeAsync(_subscriber);

    public void HandleValueAbstractly(string value)
    {
        try
        {
            var externalValue = JsonSerializer.Deserialize<ExternalValue>(value, _serializerOptions);
            if (externalValue != null)
                Received?.Invoke([externalValue,]);
        }
        catch (JsonException exception)
        {
            _logger.CannotReadLinkValue(_options.ConnectionUniqueIdentifier, _nameResolver.ResolveName(_options.ConnectionUniqueIdentifier),
                _options.EngineUniqueIdentifier, _nameResolver.ResolveName(_options.EngineUniqueIdentifier), exception);
        }
    }

    public void HandleValueDirectly(ExternalValue value)
        => Received?.Invoke([value,]);
}
