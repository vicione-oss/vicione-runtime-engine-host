using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet.Extensions;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.Pipelines;
using ViciOne.ManagedEngine.Pipelines.Commands;

namespace ViciOne.ManagedEngine.RuntimeFunctions;

public sealed class MqttRuntimeFunctionProvider : IRuntimeFunctionProvider<MqttCommunication>
{
    private readonly IVirtualMqttClient _client;
    private readonly IRuntimeFunctionHandler _handler;
    private readonly MqttRpcServer _server;

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

    public Task ConnectAsync(CancellationToken cancellationToken) => _server.Start();

    public Task DisconnectAsync(CancellationToken cancellationToken) => _server.Stop();

    public void Dispose()
    {
        _server.Dispose();
        _client.Dispose();
    }
}
