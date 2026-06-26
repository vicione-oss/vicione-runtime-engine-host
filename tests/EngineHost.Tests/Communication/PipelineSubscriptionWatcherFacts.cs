using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Extensions;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using NSubstitute;
using Xunit;

namespace ViciOne.ManagedEngine.Communication;

public class PipelineSubscriptionWatcherFacts
{
    [Fact]
    public void Completes_on_request_topic_subscribe()
    {
        var mqttClient = Substitute.For<IVirtualMqttClient>();
        TestLogger<PipelineSubscriptionWatcher> logger = new();
        using PipelineSubscriptionWatcher watcher = new("test/topic", mqttClient);

        mqttClient.InnerClient.SubscriptionsChangedAsync += Raise.Event<Func<SubscriptionsChangedEventArgs, Task>>(
            new SubscriptionsChangedEventArgs(
                [
                    new MqttClientSubscribeResult(
                        0,
                        [
                            new MqttClientSubscribeResultItem(new MqttTopicFilter { Topic = "test/topic" }, MqttClientSubscribeResultCode.GrantedQoS0),
                        ],
                        "OK",
                        [])
                ],
                []));

        var act = FluentActions.Awaiting(() => watcher.Completion);

        act.Should().CompleteWithinAsync(TimeSpan.FromSeconds(5));
    }
}
