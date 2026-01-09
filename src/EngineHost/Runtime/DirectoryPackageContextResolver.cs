using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.EngineDeployment;
using ViciOne.ManagedEngine.PackageResolver;
using ViciOne.ManagedEngine.TypeResolution;

namespace ViciOne.ManagedEngine.Runtime;

internal class DirectoryPackageContextResolver(string packagesDirectory, bool preloadTypeConverterAssemblyInContext, IFileSystem fileSystem) : IContextResolver
{
    private readonly string _packagesDirectory = packagesDirectory;

    private readonly Dictionary<string, IReadOnlyCollection<string>> _packagesAssemblyFullNames = [];

    public async Task<AssemblyLoadContext> ResolveAsync(IReadOnlyCollection<PackageReference> references, string contextId,
        IReadOnlyCollection<string> sharedAssemblies, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var (packages, _) = await new DirectoryPackageResolver(_packagesDirectory, fileSystem)
            .ResolveAsync(references, cancellationToken).ConfigureAwait(false);
        var files = PackageInstaller.Resolve(packages, fileSystem, cancellationToken);
        var context = PackagesAssemblyLoadContextFactory.Create(contextId,
            sharedAssemblies, loggerFactory.CreateLogger<PackagesAssemblyLoadContext>(), files, fileSystem);

        if (preloadTypeConverterAssemblyInContext)
        {
            var typeDescriptorAssemblyPath = typeof(TypeDescriptor).Assembly.Location;
            var netstandardAssemblyPath = fileSystem.Path.Combine(fileSystem.Path.GetDirectoryName(typeDescriptorAssemblyPath) ?? throw new InvalidOperationException($"{nameof(TypeDescriptor)} has no directory."), "netstandard.dll");
            context.LoadFromAssemblyPath(typeDescriptorAssemblyPath);
            context.LoadFromAssemblyPath(netstandardAssemblyPath);
        }

        foreach (var package in packages)
        {
            if (_packagesAssemblyFullNames.TryGetValue(package.Directory, out var assemblyNames))
            {
                foreach (var fullName in assemblyNames)
                    context.LoadFromAssemblyName(new(fullName));
            }
            else
            {
                var loadedAssemblyNames = AssemblyLoader.Create(package.Directory)
                    .RegisterInterfacesToLoad(ConstructorProviderFactory.SupportedProviderInterfaces)
                    .RegisterInterfacesToLoad(ContractAssemblies.ViciOneCoreContractTypes)
                    .Load(context);
                _packagesAssemblyFullNames.Add(package.Directory, [.. loadedAssemblyNames.Select(a => a.FullName)]);
            }
        }

        return context;
    }
}
