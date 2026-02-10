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

/// <summary>
/// Handles direct in-process incoming external communication.
/// </summary>
public sealed class DirectExternalIncomingCommunication : IExternalIncomingCommunication<DirectCommunication>
{
    private readonly ILogger<DirectExternalIncomingCommunication> _logger;
    private readonly ExternalIncomingCommunicationOptions _options;
    private readonly INameResolver _nameResolver;
    private readonly DirectExchange _exchange;
    private readonly SubscriberInfo _subscriber;
    private readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectExternalIncomingCommunication"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The communication options.</param>
    /// <param name="nameResolver">The name resolver for resolving identifiers.</param>
    /// <param name="assemblyLoadContext">The assembly load context.</param>
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

    /// <inheritdoc />
    public event Action<IReadOnlyCollection<ExternalValue>>? Received;

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken cancellationToken)
        => _exchange.SubscribeAsync(_options.Channels, _subscriber);

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken)
        => _exchange.UnsubscribeAsync(_subscriber);

    /// <summary>
    /// Handles an incoming value as a JSON string and deserializes it.
    /// </summary>
    /// <param name="value">The JSON string representing the external value.</param>
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

    /// <summary>
    /// Handles an incoming value directly without deserialization.
    /// </summary>
    /// <param name="value">The external value to handle.</param>
    public void HandleValueDirectly(ExternalValue value)
        => Received?.Invoke([value,]);
}
