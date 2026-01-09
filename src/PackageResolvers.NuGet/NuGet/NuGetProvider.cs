using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Packaging.Signing;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using MicrosoftLogger = Microsoft.Extensions.Logging.ILogger;

namespace ViciOne.ManagedEngine.PackageResolver.NuGet;

internal static class NuGetProvider
{
    internal static async Task<LockFile> TryRestoreAsync(IEnumerable<(string Identity, string Version)> packages, NuGetFramework tfm, IEnumerable<NuGetPackageSource>? packageSources, string? outputDirectory, bool warningAsError, bool noCache, MicrosoftLogger? logger = null)
    {
        var settings = LoadSystemWideSettings();
        var project = CreateProject(packages, tfm, settings, outputDirectory, packageSources);
        var nugetLogger = CreateLogger(logger);
        return await RestoreAsync(project, nugetLogger, settings, warningAsError, noCache).ConfigureAwait(false);
    }

    private static ISettings LoadSystemWideSettings() => Settings.LoadDefaultSettings(Environment.CurrentDirectory);

    private static async Task<LockFile> RestoreAsync(PackageSpec project, ILogger logger, ISettings settings, bool warningAsError, bool noCache)
    {
        var clientPolicyContext = ClientPolicyContext.GetClientPolicy(settings, logger);
        using SourceCacheContext cacheContext = new()
        {
            NoCache = noCache,
        };
        RestoreCommandProvidersCache restoreCache = new();
        var commandProviders = restoreCache.GetOrCreate(project.RestoreMetadata.OutputPath, [.. project.RestoreMetadata.FallbackFolders],
            [.. CreateRepositories(project.RestoreMetadata.Sources)], cacheContext, logger);
        RestoreRequest restoreRequest = new(project, commandProviders, cacheContext, clientPolicyContext, PackageSourceMapping.GetPackageSourceMapping(settings), logger, new());
        RestoreCommand restoreCommand = new(restoreRequest);

        var result = await restoreCommand.ExecuteAsync().ConfigureAwait(false);
        return !result.Success || (warningAsError && result.LogMessages.Any(m => m.Level == LogLevel.Warning))
            ? throw new AggregateException(result.LogMessages.Select(m => new InvalidOperationException(m.Message)))
            : result.LockFile;
    }

    private static IEnumerable<SourceRepository> CreateRepositories(IEnumerable<PackageSource> packageSources)
    {
        foreach (var source in packageSources)
            yield return Repository.Factory.GetCoreV3(source);
    }

    private static PackageSpec CreateProject(IEnumerable<(string Identity, string Version)> packages, NuGetFramework tfm, ISettings settings, string? outputDirectory, IEnumerable<NuGetPackageSource>? packageSources)
    {
        ProjectRestoreMetadata restoreMetadata = new()
        {
            OutputPath = outputDirectory ?? SettingsUtility.GetGlobalPackagesFolder(settings),
            FallbackFolders = [.. SettingsUtility.GetFallbackPackageFolders(settings)],
            Sources = [.. GetPackageSources(packageSources, settings)],
        };

        return new([new() { FrameworkName = tfm, Dependencies = [.. GetDependencies(packages)], },])
        {
            Name = "project",
            FilePath = "project.json",
            RestoreMetadata = restoreMetadata,
        };
    }

    private static ILogger CreateLogger(MicrosoftLogger? logger)
        => logger is not null ? new NuGetLogger(logger) : NullLogger.Instance;

    private static IEnumerable<PackageSource> GetPackageSources(IEnumerable<NuGetPackageSource>? packageSources, ISettings settings)
    {
        if (packageSources is not null)
        {
            foreach (var source in packageSources)
            {
                PackageSource packageSource = new(source.Source);
                packageSource.Credentials = source.Credentials switch
                {
                    NuGetBasicAuthentication a => new PackageSourceCredential(packageSource.Name, a.Username, a.Password, a.PasswordIsClearText, string.Empty),
                    _ => null
                };
                yield return packageSource;
            }
        }

        foreach (var source in SettingsUtility.GetEnabledSources(settings))
            yield return source;
    }

    private static IEnumerable<LibraryDependency> GetDependencies(IEnumerable<(string Identity, string Version)> packages)
        => packages.Select(static package =>
        {
            var version = NuGetVersion.Parse(package.Version);
            return new LibraryDependency()
            {
                LibraryRange = new(package.Identity, new(minVersion: version, includeMinVersion: true, maxVersion: version, includeMaxVersion: true), LibraryDependencyTarget.Package),
                IncludeType = LibraryIncludeFlags.All,
            };
        });
}
