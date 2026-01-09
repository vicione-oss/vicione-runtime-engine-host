using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Extensions;
using MQTTnet.Extensions.ManagedClient;

namespace ViciOne.ManagedEngine.Communication;

internal sealed class PipelineSubscriptionWatcher : IDisposable
{
    private readonly IVirtualMqttClient _mqttClient;

    private readonly TaskCompletionSource _subscribedRequestTopic = new();

    private readonly string _topic;
    private int _completed;

    internal PipelineSubscriptionWatcher(string topic, IVirtualMqttClient mqttClient)
    {
        _topic = topic;
        _mqttClient = mqttClient;
        _mqttClient.InnerClient.SubscriptionsChangedAsync += OnSubscriptionsChanged;
    }

    public Task Completion => _subscribedRequestTopic.Task;

    public void Dispose() => _mqttClient.InnerClient.SubscriptionsChangedAsync -= OnSubscriptionsChanged;

    private Task OnSubscriptionsChanged(SubscriptionsChangedEventArgs args)
    {
        if (string.IsNullOrEmpty(_topic))
            return Task.CompletedTask;

        foreach (var subscribeResult in args.SubscribeResult)
        {
            foreach (var item in subscribeResult.Items)
            {
                if (!string.Equals(item.TopicFilter.Topic, _topic, StringComparison.Ordinal))
                    continue;
                if (Interlocked.Exchange(ref _completed, 1) == 0)
                {
                    if (!IsGranted(item.ResultCode))
                    {
                        _subscribedRequestTopic.TrySetException(new InvalidOperationException($"Failed to subscribe to topic '{_topic}': {subscribeResult.ReasonString ?? "none"} ({(int)item.ResultCode})"));
                    }
                    else
                    {
                        _subscribedRequestTopic.TrySetResult();
                        _mqttClient.InnerClient.SubscriptionsChangedAsync -= OnSubscriptionsChanged;
                    }
                }
                return Task.CompletedTask;
            }
        }
        return Task.CompletedTask;
    }

    private static bool IsGranted(MqttClientSubscribeResultCode code)
        => code switch
        {
            MqttClientSubscribeResultCode.GrantedQoS0 or
            MqttClientSubscribeResultCode.GrantedQoS1 or
            MqttClientSubscribeResultCode.GrantedQoS2 => true,
            _ => false,
        };
}
