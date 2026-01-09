using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.PackageResolver;

namespace ViciOne.ManagedEngine.TypeResolution;

internal static class PackagesAssemblyLoadContextFactory
{
    internal static PackagesAssemblyLoadContext Create(string name, IReadOnlyCollection<string> sharedAssemblies,
        ILogger<PackagesAssemblyLoadContext> logger, List<(string Filename, Package)> files, IFileSystem fileSystem)
        => new(name, sharedAssemblies, logger, [.. files.ToMainComponents()], fileSystem);

    internal static IEnumerable<(string Filename, string PackageMainComponent)> ToMainComponents(this List<(string Filename, Package)> files)
    {
        Dictionary<Package, string> components = [];

        foreach (var (filename, package) in files)
        {
            if (!components.TryGetValue(package, out var componentFilename))
            {
                componentFilename = package.Assets.OfType<DependencyFile>().First().Filename
                    .Replace(".deps.json", ".dll", System.StringComparison.OrdinalIgnoreCase);
                components.Add(package, componentFilename);
            }

            yield return (filename, componentFilename);
        }
    }
}
