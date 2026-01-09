using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using ViciOne.ManagedEngine.PackageResolver;

namespace ViciOne.ManagedEngine.EngineDeployment;

internal static class PackageInstaller
{
    internal static List<(string Filename, Package Package)> Resolve(IEnumerable<Package> packages, IFileSystem fileSystem, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return [.. packages
            .SelectMany(p => p.Assets.Select(a => (Asset: a, Package: p)))
            .GroupBy(e => fileSystem.Path.GetFileNameWithoutExtension(e.Asset.Filename), e => new AssetFileCandidate(e.Asset.Filename, ReadAssemblyVersion(e.Asset.Filename, fileSystem), e.Package))
            .Select(e => (e.Key, e.Aggregate((file1 , file2) => ByHighestVersionOrLastFile(file1, file2, fileSystem)).Package))];
    }

    internal sealed record AssetFileCandidate(string Filename, Version? Version, Package Package);

    internal static AssetFileCandidate ByHighestVersionOrLastFile(AssetFileCandidate file1, AssetFileCandidate file2, IFileSystem fileSystem)
    {
        if (file1.Version is not null && file2.Version is not null)
            return file1.Version >= file2.Version ? file1 : file2;
        if (string.Compare(fileSystem.Path.GetFileName(file1.Filename), fileSystem.Path.GetFileName(file2.Filename), StringComparison.Ordinal) > 0)
            return file1;
        return file2;
    }

    private static Version? ReadAssemblyVersion(string filename, IFileSystem fileSystem)
    {
        if (!filename.EndsWith("dll", StringComparison.OrdinalIgnoreCase) || !fileSystem.File.Exists(filename))
            return default;

        Version version = new();
        using var stream = fileSystem.File.OpenRead(filename);
        using PEReader peReader = new(stream);
        if (peReader.HasMetadata)
        {
            var metaDataReader = peReader.GetMetadataReader();
            if (metaDataReader.IsAssembly)
            {
                var assemblyDefinition = metaDataReader.GetAssemblyDefinition();
                version = assemblyDefinition.Version;
            }
        }
        return version;
    }
}
