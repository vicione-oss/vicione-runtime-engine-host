using System;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Extensions;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;

namespace Benchmarks.Communication;

internal sealed class VirtualMqttClient : IVirtualMqttClient
{
    private readonly IVirtualMqttClient _client;

    IManagedMqttClient IVirtualMqttClient.InnerClient => _client.InnerClient;

    internal VirtualMqttClient(IVirtualMqttClient client) => _client = client;

    public event Func<MqttApplicationMessageReceivedEventArgs, Task>? MessageReceived
    {
        add => _client.MessageReceived += value;
        remove => _client.MessageReceived -= value;
    }

    public async Task Connect()
    {
        await _client.Connect();
        while (!_client.InnerClient.IsConnected)
            await Task.Delay(1);
    }

    public async Task Disconnect()
    {
        await _client.Disconnect();
        while (_client.InnerClient.IsConnected)
            await Task.Delay(1);
    }

    public Task Publish(MqttApplicationMessage message)
    {
        /* Pakete nicht auf dem Broker speichern, weil sie sonst am Ende eines Benchmarks wieder entfernt werden müssten.
         * Dies würde Zeit kosten, sich auf die Leistungsmessung auswirken und könnte zu Fehlern führen.
         */
        message.Retain = false;
        return _client.Publish(message);
    }

    public async Task Subscribe(string topicFilter, MqttQualityOfServiceLevel qos, bool retain)
    {
        /* Direkt am Broker subscriben, damit die Subscription vorhanden ist, bevor der ersten Werte gesendet werden.
         * Ansonsten verwirft der Broker einzelne Werte, weil kein Subscriber registriert ist.
         */
        await _client.InnerClient.InternalClient.SubscribeAsync(topicFilter, qos);
        await _client.Subscribe(topicFilter, qos, false);
    }

    public async Task Unsubscribe(string topicFilter)
    {
        await _client.InnerClient.InternalClient.UnsubscribeAsync(topicFilter);
        await _client.Unsubscribe(topicFilter);
    }

    public Task ReceiveMessage(MqttApplicationMessageReceivedEventArgs eventArgs) => _client.ReceiveMessage(eventArgs);
    public void Dispose() => _client.Dispose();
}
