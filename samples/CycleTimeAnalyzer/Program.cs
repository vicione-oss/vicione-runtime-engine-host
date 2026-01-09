using System.CommandLine;

namespace CycleTimeAnalyzer;

internal static class Program
{
    internal static async Task Main(string[] args)
    {
        Argument<string> engineHost = new("enginehost")
        {
            Description = "Engine Host Id / Topic Root",
        };
        Option<string> host = new("--host", "-h")
        {
            Description = "MQTT broker host",
            DefaultValueFactory = a => "localhost",
        };
        Option<int> port = new("--port", "-p")
        {
            Description = "MQTT broker port",
            DefaultValueFactory = a => 1883,
        };
        port.Validators.Add(o =>
        {
            if (o.GetRequiredValue(port) <= 0)
                o.AddError("Must be greater than 0");
        });
        Option<string?> user = new("--user", "-u")
        {
            Description = "MQTT username",
        };
        Option<string?> password = new("--password", "-s")
        {
            Description = "MQTT password",
        };
        Option<int?> timeLimit = new("--timelimit", "-t")
        {
            Description = "Time limit in seconds",
        };
        timeLimit.Validators.Add(o =>
        {
            if (o.GetValue(timeLimit) <= 0)
                o.AddError("Must be greater than 0");
        });

        RootCommand root = new("Cycle Time Analyzer")
        {
            engineHost,
            host,
            port,
            user,
            password,
            timeLimit,
        };

        root.SetAction(async (parseResult, cancellationToken) =>
        {
            var options = App.CreateClientOptions(
                parseResult.GetRequiredValue(host),
                parseResult.GetRequiredValue(port),
                parseResult.GetValue(user),
                parseResult.GetValue(password));
            await new App().RunAsync(
                parseResult.GetRequiredValue(engineHost),
                options,
                parseResult.GetValue(timeLimit));
        });

        var parseResult = root.Parse(args);
        await parseResult.InvokeAsync();
    }
}
