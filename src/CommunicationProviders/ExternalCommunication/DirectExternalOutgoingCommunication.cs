using System.Collections.Generic;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.Runtime;

namespace ViciOne.ManagedEngine.ExternalCommunication;

public sealed class DirectExternalOutgoingCommunication : IExternalOutgoingCommunication<DirectCommunication>
{
    private readonly ExternalOutgoingCommunicationOptions _options;
    private readonly ILogger<DirectExternalOutgoingCommunication> _logger;
    private readonly INameResolver _nameResolver;
    private readonly DirectExchange _exchange;
    private readonly int _contextHash;
    private readonly JsonSerializerOptions _serializerOptions;

    public DirectExternalOutgoingCommunication(ExternalOutgoingCommunicationOptions options, ILogger<DirectExternalOutgoingCommunication> logger,
        INameResolver nameResolver, AssemblyLoadContext assemblyLoadContext)
        : this(options, DirectExchange.Instance, logger, nameResolver, assemblyLoadContext)
    { }

    internal DirectExternalOutgoingCommunication(ExternalOutgoingCommunicationOptions options, DirectExchange exchange,
        ILogger<DirectExternalOutgoingCommunication> logger, INameResolver nameResolver, AssemblyLoadContext assemblyLoadContext)
    {
        _options = options;
        _exchange = exchange;
        _logger = logger;
        _nameResolver = nameResolver;
        _contextHash = assemblyLoadContext.GetHashCode();
        _serializerOptions = JsonSetup.CreatePreserveTypeOptions(assemblyLoadContext);
    }

    public Task ConnectAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task DisconnectAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task SendAsync(ulong engineCycle, IReadOnlyCollection<ExternalValue> values, CancellationToken cancellationToken)
    {
        _logger.SendInformation(
            values.Count,
            _options.EngineUniqueIdentifier,
            _nameResolver.ResolveName(_options.EngineUniqueIdentifier),
            _options.ConnectionUniqueIdentifier,
            _nameResolver.ResolveName(_options.ConnectionUniqueIdentifier));

        await _exchange.ForwardValuesAsync(values, _contextHash, _serializerOptions,
            ex => _logger.CannotSendLinkValues(
                _options.EngineUniqueIdentifier,
                _nameResolver.ResolveName(_options.EngineUniqueIdentifier),
                _options.ConnectionUniqueIdentifier,
                _nameResolver.ResolveName(_options.ConnectionUniqueIdentifier),
                ex));
    }
}
