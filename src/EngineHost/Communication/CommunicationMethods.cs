using System;
using MQTTnet.Extensions;
using ViciOne.ManagedEngine.Pipelines;

namespace ViciOne.ManagedEngine.Communication;

internal class CommunicationMethods(IMediator mediator)
{
    private readonly IMediator _mediator = mediator;

    internal MqttRpcRegistrations MqttRpcRegistrations { get; } = new();

    internal void Register<TRequest, TResponse>(string name) where TRequest : IRequest<TResponse>
        => MqttRpcRegistrations.RegisterHandler<TRequest, TResponse>(name, _ => _mediator.Send<TRequest, TResponse>(_));

    internal void Register<TPayload, TRequest, TResponse>(string name, Func<TPayload, TRequest> parser) where TRequest : IRequest<TResponse>
        => MqttRpcRegistrations.RegisterHandler(name, parser, _ => _mediator.Send<TRequest, TResponse>(_));
}
