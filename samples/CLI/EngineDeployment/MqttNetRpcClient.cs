using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;

namespace ViciOne.ManagedEngine.EngineDeployment;

internal sealed class MqttNetRpcClient : IMqttRpcClient, IDisposable
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<MqttRpcResponse>> _waitingCalls;
    private readonly IMqttClient _mqttClient;

    internal MqttNetRpcClient(IMqttClient mqttClient)
    {
        _waitingCalls = new();
        _mqttClient = mqttClient;
        mqttClient.ApplicationMessageReceivedAsync += HandleApplicationMessageReceivedAsync;
    }

    public async Task<MqttRpcResponse> Request(string requestTopic, MqttRpcRequest request, string responseTopic, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestTopic))
            throw new ArgumentException("RPC request topic is invalid.", nameof(requestTopic));
        if (string.IsNullOrWhiteSpace(responseTopic))
            throw new ArgumentException("RPC response topic is invalid.", nameof(responseTopic));
        if (string.IsNullOrWhiteSpace(request.Method))
            throw new ArgumentException("RPC method is invalid.", nameof(request));

        var correlationData = Guid.NewGuid();
        var requestMessageBuilder = new MqttApplicationMessageBuilder()
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
            .WithCorrelationData(correlationData.ToByteArray())
            .WithTopic(requestTopic)
            .WithUserProperty("Method", request.Method);
        if (request.Payload.HasValue)
        {
            requestMessageBuilder = requestMessageBuilder.WithPayload(request.Payload.Value)
                .WithContentType(request.ContentType);
        }
        var requestMessage = requestMessageBuilder.Build();


        try
        {
            TaskCompletionSource<MqttRpcResponse> awaitable = new(TaskCreationOptions.RunContinuationsAsynchronously);

            _waitingCalls.TryAdd(correlationData, awaitable);

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(responseTopic, MqttQualityOfServiceLevel.ExactlyOnce).Build();
            await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken).ConfigureAwait(false);
            await _mqttClient.PublishAsync(requestMessage, cancellationToken).ConfigureAwait(false);

            using (cancellationToken.Register(() => awaitable.TrySetCanceled()))
            {
                return await awaitable.Task.ConfigureAwait(false);
            }
        }
        finally
        {
            _waitingCalls.TryRemove(correlationData, out _);

            await _mqttClient.UnsubscribeAsync(responseTopic, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        var correlationData = Guid.Empty;
        try
        {
            correlationData = new Guid(eventArgs.ApplicationMessage.CorrelationData);
        }
        catch { }
        if (_waitingCalls.TryRemove(correlationData, out var awaitable))
        {
            var status = eventArgs.ApplicationMessage.UserProperties.Find(_ => StringComparer.InvariantCulture.Equals(_.Name, "Status"))?.Value;
            if (status is null)
            {
                awaitable.TrySetException(new InvalidOperationException("Response does not contain status."));
            }
            else
            {
                MqttRpcResponse response = new(status, eventArgs.ApplicationMessage.PayloadSegment, eventArgs.ApplicationMessage.ContentType);
                awaitable.TrySetResult(response);
            }
        }

        eventArgs.IsHandled = true;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        foreach (var waitingCall in _waitingCalls)
            waitingCall.Value.TrySetCanceled();

        _waitingCalls.Clear();
        _mqttClient.ApplicationMessageReceivedAsync -= HandleApplicationMessageReceivedAsync;
    }
}
