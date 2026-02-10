using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyModel;

namespace ViciOne.ManagedEngine.PackageResolver;

/// <summary>
/// Resolves packages from a local directory.
/// </summary>
/// <param name="directory">The directory containing the packages.</param>
/// <param name="fileSystem">The file system abstraction.</param>
public sealed class DirectoryPackageResolver(string directory, IFileSystem fileSystem) : IPackageResolver
{
    private readonly string _directory = directory;
    private readonly IFileSystem _fileSystem = fileSystem;

    /// <inheritdoc />
    public Task<ResolveResult> ResolveAsync(IReadOnlyCollection<PackageReference> packageReferences, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var packages = GetPackages(packageReferences);
        var depsFiles = FindDependencyFiles(packages);
        var depsFile = MergeDependencyFiles([.. depsFiles.SelectMany(p => p.Value)], _fileSystem);

        return Task.FromResult<ResolveResult>(new(packages, new(depsFile)));
    }

    private List<Package> GetPackages(IReadOnlyCollection<PackageReference> packageReferences)
    {
        List<Package> packages = [];
        List<Exception> exceptions = [];

        foreach (var reference in packageReferences)
        {
            try
            {
                packages.Add(GetPackageFromDirectory(reference.Name, reference.Version));
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }
        }

        return exceptions.Count > 0
            ? throw new InvalidOperationException("Could not obtain some packages.", new AggregateException(exceptions))
            : packages;
    }

    private Package GetPackageFromDirectory(string name, string version)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));
        if (string.IsNullOrEmpty(version))
            throw new ArgumentNullException(nameof(version));
        var directory = _fileSystem.Path.Combine(_directory, name, version);
        if (_fileSystem.Directory.Exists(directory))
        {
            var assets = _fileSystem.Directory
                .GetFiles(directory, "*", SearchOption.AllDirectories)
                .Select(CreateAsset)
                .ToList();
            return new(directory, assets);
        }
        else
        {
            throw new InvalidOperationException($"Package '{name}' with version '{version}' could not be found.");
        }

        static Asset CreateAsset(string filename)
        {
            if (filename.EndsWith(".deps.json", StringComparison.InvariantCultureIgnoreCase))
                return new DependencyFile(filename);
            else
                return new Asset(filename);
        }
    }

    private static string MergeDependencyFiles(IEnumerable<DependencyFile> files, IFileSystem fileSystem)
    {
        var contexts = new Stack<DependencyContext>();
        using DependencyContextJsonReader reader = new();

        foreach (var file in files)
        {
            using var fileStream = fileSystem.FileStream.New(file.Filename, FileMode.Open);
            contexts.Push(reader.Read(fileStream));
        }

        while (contexts.Count >= 2)
            contexts.Push(contexts.Pop().Merge(contexts.Pop()));

        if (contexts.TryPop(out var mainContext))
        {
            using MemoryStream streamOut = new();
            DependencyContextWriter writer = new();
            writer.Write(mainContext, streamOut);
            streamOut.Flush();
            streamOut.Position = 0;

            using StreamReader streamReader = new(streamOut);
            return streamReader.ReadToEnd();
        }
        else
        {
            return string.Empty;
        }
    }

    private static Dictionary<Package, DependencyFile[]> FindDependencyFiles(IEnumerable<Package> packages)
        => packages.ToDictionary(p => p, p => p.Assets.OfType<DependencyFile>().ToArray());
}
