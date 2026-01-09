using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace CycleTimeAnalyzer;

internal sealed class App
{
    private readonly ConcurrentBag<ArraySegment<byte>> _messages = [];

    internal static ManagedMqttClientOptions CreateClientOptions(string mqttHost, int mqttPort, string? mqttUsername, string? mqttPassword)
        => new ManagedMqttClientOptionsBuilder()
            .WithPendingMessagesOverflowStrategy(MQTTnet.Server.MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
            .WithMaxPendingMessages(100_000)
            .WithClientOptions(new MqttClientOptionsBuilder()
                .WithTcpServer(mqttHost, mqttPort)
                .WithCredentials(mqttUsername, mqttPassword)
                .Build())
            .Build();

    internal async Task RunAsync(string enginehost, ManagedMqttClientOptions options, int? timeLimit)
    {
        using var managedClient = new MqttFactory().CreateManagedMqttClient();
        managedClient.ApplicationMessageReceivedAsync += ManagedClient_ApplicationMessageReceivedAsync;
        await managedClient.SubscribeAsync($"{enginehost}/cycle/info").ConfigureAwait(false);
        await managedClient.StartAsync(options).ConfigureAwait(false);

        if (timeLimit.HasValue)
            await WaitAsync(timeLimit.Value).ConfigureAwait(false);
        else
            await WaitAsync().ConfigureAwait(false);

        await managedClient.StopAsync().ConfigureAwait(false);

        var messages = await ParseMessagesAsync().ConfigureAwait(false);
        var deploymentDurations = messages
            .SelectMany(m => m.Deployments)
            .GroupBy(e => e.Deployment)
            .ToDictionary(g => g.Key, g => g.Select(d => d.Duration));

        WriteStatsToConsole(deploymentDurations);
    }

    private static void WriteStatsToConsole(Dictionary<string, IEnumerable<double>> deploymentDurations)
    {
        foreach (var (deployment, durations) in deploymentDurations)
            WriteEngineStats(deployment, durations.Min(), durations.Max(), durations.Average());

        static void WriteEngineStats(string deployment, double min, double max, double avg)
        {
            Console.WriteLine();
            Console.WriteLine(deployment);
            Console.WriteLine(new string('-', deployment.Length));
            Console.Write("Min: ");
            Console.WriteLine($"{min:0.000}ms");
            Console.Write("Max: ");
            Console.WriteLine($"{max:0.000}ms");
            Console.Write("Avg: ");
            Console.WriteLine($"{avg:0.000}ms");
        }
    }

    private async Task<List<Message>> ParseMessagesAsync()
    {
        List<Message> messages = [];
        foreach (var data in _messages)
        {
            try
            {
                if (data.Count > 0 && data.Array is not null)
                {
                    using var stream = new MemoryStream(data.Array, data.Offset, data.Count, false);
                    messages.Add(await JsonSerializer.DeserializeAsync<Message>(stream).ConfigureAwait(false)
                        ?? throw new InvalidOperationException("Message cannot be null."));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        return messages;
    }

    private static async Task WaitAsync()
    {
        Console.SetCursorPosition(0, Console.CursorTop + 1);
        Console.Write($"Press Enter to stop ...");
        Console.SetCursorPosition(0, Console.CursorTop - 1);

        var stopwatch = Stopwatch.StartNew();

        while (!Console.KeyAvailable)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write($"{stopwatch.Elapsed.TotalSeconds:0}s");
            await Task.Delay(1_000).ConfigureAwait(false);
        }

        stopwatch.Stop();
    }

    private static async Task WaitAsync(int limit)
    {
        var stopwatch = Stopwatch.StartNew();

        do
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write($"{stopwatch.Elapsed.TotalSeconds:0}s");
            await Task.Delay(1_000).ConfigureAwait(false);
        }
        while (stopwatch.Elapsed.TotalSeconds < limit);

        stopwatch.Stop();
    }

    private Task ManagedClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        _messages.Add(arg.ApplicationMessage.PayloadSegment);
        return Task.CompletedTask;
    }
}
