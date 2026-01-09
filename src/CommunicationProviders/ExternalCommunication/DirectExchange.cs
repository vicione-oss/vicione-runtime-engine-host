using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.ExternalCommunication;

internal class DirectExchange : IDisposable
{
    public static DirectExchange Instance = new();

    internal readonly Dictionary<string, HashSet<SubscriberInfo>> _subscriptions = [];
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    internal async Task SubscribeAsync(IReadOnlyCollection<string> channels, SubscriberInfo subscriber)
    {
        using (await _semaphore.LockAsync().ConfigureAwait(false))
        {
            foreach (var channel in channels)
            {
                if (_subscriptions.TryGetValue(channel, out var subscribers))
                    subscribers.Add(subscriber);
                else
                    _subscriptions.Add(channel, [subscriber]);
            }
        }
    }

    internal async Task UnsubscribeAsync(SubscriberInfo subscriber)
    {
        using (await _semaphore.LockAsync().ConfigureAwait(false))
        {
            foreach (var (channel, subscribers) in _subscriptions)
            {
                if (subscribers.Contains(subscriber) && subscribers.Count == 1)
                    _subscriptions.Remove(channel);
                else
                    subscribers.Remove(subscriber);
            }
        }
    }

    internal async Task ForwardValuesAsync(IReadOnlyCollection<ExternalValue> values, int contextHash, JsonSerializerOptions serializerOptions, Action<Exception>? onFailure = null)
    {
        using (await _semaphore.LockAsync().ConfigureAwait(false))
        {
            foreach (var value in values)
            {
                if (_subscriptions.TryGetValue(value.Channel, out var subscribers))
                {
                    foreach (var subscriber in subscribers)
                        ForwardToSubscriber(contextHash, value, subscriber);
                }
            }
        }

        void ForwardToSubscriber(int contextHash, ExternalValue value, SubscriberInfo subscriber)
        {
            try
            {
                if (subscriber.ContextHash == contextHash)
                    subscriber.HandleValueDirectly(value);
                else
                    subscriber.HandleValueAbstractly(JsonSerializer.Serialize(value, serializerOptions));
            }
            catch (Exception ex)
            {
                onFailure?.Invoke(ex);
            }
        }
    }

    public void Dispose()
        => _semaphore.Dispose();
}
