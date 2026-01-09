using System.Collections.Generic;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.PackageResolver;

namespace ViciOne.ManagedEngine.Runtime;

internal interface IContextResolver
{
    Task<AssemblyLoadContext> ResolveAsync(IReadOnlyCollection<PackageReference> references, string contextId,
        IReadOnlyCollection<string> sharedAssemblies, ILoggerFactory loggerFactory, CancellationToken cancellationToken);
}
