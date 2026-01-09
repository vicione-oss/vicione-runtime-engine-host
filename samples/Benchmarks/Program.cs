using System.CommandLine;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Benchmarks.Results;

namespace Benchmarks;

internal static class Program
{
    internal static async Task Main(string[] args)
    {
        RootCommand rootCommand = new("Execute a benchmark.");
        rootCommand.SetAction(parseResult =>
        {
            IConfig? config = null;
#if DEBUG
            config = new DebugInProcessConfig();
#endif
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        });

        Command enrichCommand = new("enrich", "Enrich benchmark results with additional information.");
        Argument<string> csvReportFilePathArgument = new("csvReportFilePath")
        {
            Description = "The path to the CSV report file to enrich.",
        };
        enrichCommand.Add(csvReportFilePathArgument);
        enrichCommand.SetAction((parseResult, cancellationToken) => BenchmarkReport.EnrichAsync(parseResult.GetRequiredValue(csvReportFilePathArgument)));
        rootCommand.Subcommands.Add(enrichCommand);

        var parseResult = rootCommand.Parse(args);
        await parseResult.InvokeAsync();
    }
}
