using System.Collections.Generic;
using System.CommandLine;
using System.Threading.Tasks;
using Testably.Abstractions;
using ViciOne.ManagedEngine.PackageResolver;

namespace ViciOne.EngineHost.EngineAssemblyLoadingTest;

internal static class Program
{
    internal static async Task Main(string[] args)
    {
        Argument<string> packagesDirectoryArg = new("packagesDirectory")
        {
            Description = "The directory containing packages.",
        };
        Option<List<PackageReference>?> packagesArg = new("--package", "-p")
        {
            Description = "The packages to load e.g. MyFB.Add@1.0.0",
        };
        packagesArg.CustomParser = result =>
        {
            List<PackageReference> list = [];
            foreach (var token in result.Tokens)
            {
                if (PackageReferenceFactory.TryParse(token.Value, out var packageReference))
                    list.Add(packageReference);
                else
                    result.AddError($"{packagesArg.Name} requires a '@' in '{token.Value}'");
            }
            return list;
        };
        Option<string?> designFilterArg = new("--designFilter", "-f")
        {
            Description = "A part of a FunctionBlock design name to apply as filter.",
        };

        RootCommand root = new("Engine Assembly Loading Test")
        {
            packagesDirectoryArg,
            packagesArg,
            designFilterArg,
        };

        root.SetAction(async (parseResult, cancellationToken) =>
        {
            var dir = parseResult.GetRequiredValue(packagesDirectoryArg);
            var pkgs = parseResult.GetValue(packagesArg);
            var filter = parseResult.GetValue(designFilterArg);

            if (pkgs is null || pkgs.Count == 0)
                pkgs = PackageSelector.Show(dir);

            await AssemblyLoadingTest.RunAsync(dir, pkgs, filter, new RealFileSystem());
        });

        var parseResult = root.Parse(args);
        await parseResult.InvokeAsync();
    }
}
