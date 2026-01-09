using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.PackageResolver;

public interface IPackageResolver
{
    /// <exception cref="System.InvalidOperationException">
    /// A package name or version is <c>null</c> or empty.
    /// - or -
    /// A specified package could not be obtained.
    /// - or -
    /// The versions of packages dependencies are in conflict.
    /// </exception>
    Task<ResolveResult> ResolveAsync(IReadOnlyCollection<PackageReference> packageReferences, CancellationToken cancellationToken);
}
