using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace CycleTimeAnalyzer;

internal sealed class App
{
    private readonly ConcurrentBag<byte[]> _messages = [];

    internal static ManagedMqttClientOptions CreateClientOptions(string mqttHost, int mqttPort, string? mqttUsername, string? mqttPassword)
        => new ManagedMqttClientOptionsBuilder()
            .WithPendingMessagesOverflowStrategy(MQTTnet.Server.MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
            .WithMaxPendingMessages(100_000)
            .WithClientOptions(new MqttClientOptionsBuilder()
                .WithTcpServer(mqttHost, mqttPort)
                .WithCredentials(mqttUsername, mqttPassword)
                .Build())
            .Build();

    internal async Task RunAsync(string enginehost, ManagedMqttClientOptions options, int? timeLimit, CancellationToken cancellationToken = default)
    {
        using var managedClient = new MqttFactory().CreateManagedMqttClient();
        managedClient.ApplicationMessageReceivedAsync += ManagedClient_ApplicationMessageReceivedAsync;
        await managedClient.SubscribeAsync($"{enginehost}/cycle/info").ConfigureAwait(false);
        await managedClient.StartAsync(options).ConfigureAwait(false);

        if (timeLimit.HasValue)
            await WaitAsync(timeLimit.Value, cancellationToken).ConfigureAwait(false);
        else
            await WaitAsync(cancellationToken).ConfigureAwait(false);

        await managedClient.StopAsync().ConfigureAwait(false);

        var messages = await ParseMessagesAsync().ConfigureAwait(false);
        var cycleDurations = messages.Select(m => m.CycleDuration).ToList();
        var deploymentDurations = messages
            .SelectMany(m => m.Deployments)
            .GroupBy(e => e.Deployment)
            .ToDictionary(g => g.Key, g => g.Select(d => d.Duration).ToList());

        WriteStatsToConsole(cycleDurations, deploymentDurations);
    }

    private static void WriteStatsToConsole(List<double> cycleDurations, Dictionary<string, List<double>> deploymentDurations)
    {
        if (cycleDurations.Count > 0)
            WriteEngineStats("Total Cycle", cycleDurations.Min(), cycleDurations.Max(), cycleDurations.Average());

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
                if (data.Length > 0)
                {
                    using var stream = new MemoryStream(data, 0, data.Length, false);
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

    private static async Task WaitAsync(CancellationToken cancellationToken)
    {
        Console.SetCursorPosition(0, Console.CursorTop + 1);
        Console.Write($"Press Enter to stop ...");
        Console.SetCursorPosition(0, Console.CursorTop - 1);

        var stopwatch = Stopwatch.StartNew();

        while (!Console.KeyAvailable && !cancellationToken.IsCancellationRequested)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write($"{stopwatch.Elapsed.TotalSeconds:0}s");
            await Task.Delay(1_000, cancellationToken).ConfigureAwait(false);
        }

        stopwatch.Stop();
    }

    private static async Task WaitAsync(int limit, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        do
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write($"{stopwatch.Elapsed.TotalSeconds:0}s");
            await Task.Delay(1_000, cancellationToken).ConfigureAwait(false);
        }
        while (stopwatch.Elapsed.TotalSeconds < limit && !cancellationToken.IsCancellationRequested);

        stopwatch.Stop();
    }

    private Task ManagedClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        _messages.Add([.. arg.ApplicationMessage.PayloadSegment]);
        return Task.CompletedTask;
    }
}
