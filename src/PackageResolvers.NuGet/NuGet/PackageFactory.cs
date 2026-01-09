using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.ProjectModel;

namespace ViciOne.ManagedEngine.PackageResolver.NuGet;

internal static class PackageFactory
{
    private const char Separator = '/';

    internal static IEnumerable<Package> Create(LockFile lockFile, NuGetFramework tfm)
    {
        var resolver = CreateResolver(lockFile);
        return Create(lockFile, tfm, resolver.GetLibraryPath);
    }

    internal static IEnumerable<Package> Create(LockFile lockFile, NuGetFramework tfm, Func<LockFileTargetLibrary, string> getLibraryPath)
    {
        /*
         * legend:
         * 
         * text=fixed path part
         * {name}=changeable path part
         * ?=additional not required level
         * *=all files and directories
         * 
         * references:
         * - https://github.com/NuGet/NuGet.Client/blob/2136302f54479707befc9fac65af19e1772c7827/src/NuGet.Core/NuGet.Packaging/ContentModel/ManagedCodeConventions.cs#L411
         * - https://github.com/dotnet/sdk/blob/a30e465a2e2ea4e2550f319a2dc088daaafe5649/src/Tasks/Microsoft.NET.Build.Tasks/AssetsFileResolver.cs#L34
         */

        var target = lockFile.GetTarget(tfm, null);

        foreach (var library in target.Libraries)
        {
            if (!library.IsPackage())
                continue;

            var assets = new List<Asset>();
            var libraryPath = getLibraryPath(library);

            /*
             * patterns of runtime and native assemblies (without runtime target):
             * - lib/{tfm}/{assembly}
             * - lib/{assembly}
             * 
             * result relative to base directory: {assembly}
             */
            foreach (var item in library.RuntimeAssemblies.Union(library.NativeLibraries).FilterPlaceholderFiles())
            {
                var assetFilename = libraryPath.Combine(item.Path);
                assets.Add(new(assetFilename));
            }

            /*
             * patterns of targeted files:
             * - runtimes/{rid}/{any}/{assembly}
             * - runtimes/{rid}/{any}/{tfm}/{assembly}
             * 
             * result relative to base directory: ./*
             */
            foreach (var item in library.RuntimeTargets.FilterPlaceholderFiles())
            {
                if (item.IsRuntime() || item.IsNative())
                    assets.Add(new(libraryPath.Combine(item.Path)));
            }

            /*
             * pattern of resource files (without runtime target): lib/{tfm}/{locale?}/{assembly}
             * 
             * result relative to base directory: lib/{tfm}/*
             */
            foreach (var item in library.ResourceAssemblies.FilterPlaceholderFiles())
            {
                var assetFilename = libraryPath.Combine(item.Path);
                assets.Add(new(assetFilename));
            }

            /*
             * pattern of content files: contentFiles/{codeLanguage}/{tfm}/*
             * 
             * result relative to base directory: contentFiles/{codeLanguage}/{tfm}/*
             */
            foreach (var item in library.ContentFiles.FilterPlaceholderFiles())
            {
                if (!item.CopyToOutput)
                    continue;

                var assetFilename = libraryPath.Combine(item.Path);

                assets.Add(new Asset(assetFilename));
            }

            yield return new(string.Empty, assets);
        }
    }

    private static string Combine(this string a, string b)
        => Path.Combine(a, b.Replace(Separator, Path.DirectorySeparatorChar));

    private static bool IsPackage(this LockFileTargetLibrary library)
        => string.Equals(library.Type, "package", StringComparison.OrdinalIgnoreCase);

    private static bool IsRuntime(this LockFileRuntimeTarget target)
        => string.Equals(target.AssetType, "runtime", StringComparison.OrdinalIgnoreCase);

    private static bool IsNative(this LockFileRuntimeTarget target)
        => string.Equals(target.AssetType, "native", StringComparison.OrdinalIgnoreCase);

    private static FallbackPackagePathResolver CreateResolver(LockFile lockFile)
        => new(
            lockFile.PackageFolders.First().Path,
            lockFile.PackageFolders.Skip(1).Select(f => f.Path));

    private static string GetLibraryPath(this FallbackPackagePathResolver resolver, LockFileTargetLibrary library)
        => resolver.GetPackageDirectory(library.Name, library.Version) ?? throw new InvalidOperationException($"Cannot find package '{library.Name}' in version '{library.Version}'.");

    private static IEnumerable<T> FilterPlaceholderFiles<T>(this IEnumerable<T> files) where T : LockFileItem
        => files.Where(f => !f.Path.Contains("_._", StringComparison.InvariantCulture));
}
