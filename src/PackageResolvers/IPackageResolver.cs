using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.PackageResolver;

/// <summary>
/// Defines a contract for resolving NuGet packages.
/// </summary>
public interface IPackageResolver
{
    /// <summary>
    /// Resolves the specified package references asynchronously.
    /// </summary>
    /// <param name="packageReferences">The package references to resolve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The result of the package resolution.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// A package name or version is <c>null</c> or empty.
    /// - or -
    /// A specified package could not be obtained.
    /// - or -
    /// The versions of packages dependencies are in conflict.
    /// </exception>
    Task<ResolveResult> ResolveAsync(IReadOnlyCollection<PackageReference> packageReferences, CancellationToken cancellationToken);
}
