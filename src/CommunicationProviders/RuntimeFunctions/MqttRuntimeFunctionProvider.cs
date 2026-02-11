using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet.Extensions;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.Pipelines;
using ViciOne.ManagedEngine.Pipelines.Commands;

namespace ViciOne.ManagedEngine.RuntimeFunctions;

/// <summary>
/// Provides MQTT-based runtime function support for engine commands.
/// </summary>
public sealed class MqttRuntimeFunctionProvider : IRuntimeFunctionProvider<MqttCommunication>
{
    private readonly IVirtualMqttClient _client;
    private readonly IRuntimeFunctionHandler _handler;
    private readonly MqttRpcServer _server;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttRuntimeFunctionProvider"/> class.
    /// </summary>
    /// <param name="communication">The MQTT communication configuration.</param>
    /// <param name="engine">The engine identifier.</param>
    /// <param name="handler">The runtime function handler.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public MqttRuntimeFunctionProvider(MqttCommunication communication, string engine, IRuntimeFunctionHandler handler,
        ILogger<MqttRuntimeFunctionProvider> logger, ILoggerFactory loggerFactory) : this(engine, handler, logger, MqttOptimizer.Instance.Register(communication.ToCommunicationInfo(), loggerFactory))
    { }

    internal MqttRuntimeFunctionProvider(string engine, IRuntimeFunctionHandler handler, ILogger<MqttRuntimeFunctionProvider> logger,
        IVirtualMqttClient client)
    {
        _handler = handler;
        _client = client;
        MqttRpcRegistrations registrations = new();
        registrations.RegisterHandler<ChangeLogLevelCommand>("changeloglevel", CallHandlerAsync);
        registrations.RegisterHandler<LoadPersistenceVariablesCommand>("loadvariables", CallHandlerAsync);
        registrations.RegisterHandler<LoadPersistenceSettingsCommand>("loadsettings", CallHandlerAsync);
        _server = MqttSetup.CreateRpcServer(client, $"{engine}/request", $"{engine}/response", logger, registrations);
    }

    private async Task CallHandlerAsync<TRequest>(TRequest request) where TRequest : IRequest<CommandStatus>
    {
        var status = await _handler.Handle<TRequest, CommandStatus>(request, CancellationToken.None).ConfigureAwait(false);
        if (status is Failure failure)
            throw new InvalidOperationException(failure.Message);
    }

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken cancellationToken) => _server.Start();

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken) => _server.Stop();

    /// <inheritdoc />
    public void Dispose()
    {
        _server.Dispose();
        _client.Dispose();
    }
}
