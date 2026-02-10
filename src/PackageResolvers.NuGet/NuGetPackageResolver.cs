using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Frameworks;
using ViciOne.ManagedEngine.PackageResolver.NuGet;

namespace ViciOne.ManagedEngine.PackageResolver;

/// <summary>
/// Resolves NuGet packages using the NuGet client library.
/// </summary>
/// <param name="packageSources">The NuGet package sources to use.</param>
/// <param name="outputDirectory">The output directory for resolved packages.</param>
/// <param name="warningAsError">Whether to treat warnings as errors.</param>
/// <param name="noCache">Whether to disable caching.</param>
/// <param name="logger">The logger instance.</param>
public sealed class NuGetPackageResolver(
    IEnumerable<NuGetPackageSource>? packageSources = null,
    string? outputDirectory = null,
    bool warningAsError = true,
    bool noCache = false,
    ILogger? logger = null) : IPackageResolver
{
    private readonly IEnumerable<NuGetPackageSource>? _packageSources = packageSources;
    private readonly NuGetFramework _tfm = FrameworkConstants.CommonFrameworks.Net10_0;

    /// <summary>
    /// Initializes a new instance of the <see cref="NuGetPackageResolver"/> class with a single package source.
    /// </summary>
    /// <param name="packageSource">The NuGet package source to use.</param>
    /// <param name="outputDirectory">The output directory for resolved packages.</param>
    /// <param name="warningAsError">Whether to treat warnings as errors.</param>
    /// <param name="noCache">Whether to disable caching.</param>
    /// <param name="logger">The logger instance.</param>
    public NuGetPackageResolver(NuGetPackageSource packageSource, string? outputDirectory = null, bool warningAsError = true, bool noCache = false, ILogger? logger = null) : this([packageSource,], outputDirectory, warningAsError, noCache, logger)
    { }

    /// <inheritdoc />
    public async Task<ResolveResult> ResolveAsync(IReadOnlyCollection<PackageReference> packageReferences, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var lockFile = await NuGetProvider.TryRestoreAsync(packageReferences.Select(p => (p.Name, p.Version)), _tfm, _packageSources, outputDirectory, warningAsError, noCache, logger).ConfigureAwait(false);
            var dependencyContext = lockFile.ToDependencyContext();
            return new([.. PackageFactory.Create(lockFile, _tfm)], new(dependencyContext.ToJson()));
        }
        catch (Exception ex)
        {
            var packageSources = _packageSources?.Select(s => $"'{s}'") ?? [];
            var packageSourcesText = string.Join(", ", packageSources);
            throw new InvalidOperationException($"Could not resolve packages from {packageSourcesText}.", ex);
        }
    }
}
