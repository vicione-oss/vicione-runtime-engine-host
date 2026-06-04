using System.Collections.Concurrent;
using System.Text.Json;

namespace ViciOne.ManagedEngine.Runtime;

internal class JsonSerializerOptionsCache
{
    private readonly ConcurrentDictionary<string, JsonSerializerOptions> _optionsPool = new();

    internal JsonSerializerOptions GetOrAdd(string id, JsonSerializerOptions? options = null)
        => _optionsPool.GetOrAdd(id, options ?? new());

    internal void Remove(string id)
        => _optionsPool.TryRemove(id, out _);
}
