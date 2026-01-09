using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ViciOne.Engine.DefaultPoolings;
using ViciOne.ManagedEngine.PackageResolver;
using ViciOne.ManagedEngine.TypeResolution;

namespace ViciOne.ManagedEngine.Runtime;

internal class ContextPool : IContextPool, IDisposable
{
    internal static readonly string[] s_preloadAssemblies =
    [
        typeof(MaxPooling<int>).Assembly.GetName().Name!,
    ];

    internal static readonly string[] s_sharedAssemblies =
    [
        typeof(ManagedEngine).Assembly.GetName().Name!,                         // ViciOne.ManagedEngine
        typeof(Communication.ICommunication).Assembly.GetName().Name!,          // ViciOne.ManagedEngine.Contracts
        typeof(Communication.FileSystemCommunication).Assembly.GetName().Name!, // ViciOne.ManagedEngine.CommunicationProviders
        typeof(Core.Contracts.NameGenerator).Assembly.GetName().Name!,          // ViciOne.Core.Contracts
        typeof(Core.Dataflow.NameValidator).Assembly.GetName().Name!,           // ViciOne.Core.Dataflow
        typeof(Core.Runtime.RuntimeFunctionBlock<>).Assembly.GetName().Name!,   // ViciOne.Core.Runtime
        typeof(ViciOne.Engine.RuntimeEngine).Assembly.GetName().Name!,          // ViciOne.Engine
        typeof(ILoggerFactory).Assembly.GetName().Name!,                        // Microsoft.Extensions.Logging.Abstractions
        "System.Runtime",                                                       // System.Runtime (System.Private.CoreLib, netstandard)
    ];

    internal readonly Dictionary<string, string> _deploymentContextMap = [];
    internal readonly Dictionary<string, ContextInfo> _contexts = [];
    private readonly HostConfig _config;
    private readonly DirectoryPackageContextResolver _contextResolver;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ContextPool> _logger;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly IFileSystem _fileSystem;

    public ContextPool(IOptions<HostConfig> config, ILoggerFactory loggerFactory, IFileSystem fileSystem, ILogger<ContextPool> logger)
    {
        _config = config.Value;
        _fileSystem = fileSystem;
        _contextResolver = new DirectoryPackageContextResolver(_config.PackagesDirectory, _config.PreloadTypeConverterAssemblyInContext, _fileSystem);
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public async Task<AssemblyLoadContext> RegisterDeploymentAsync(string deploymentId, IReadOnlyCollection<PackageReference> packageReferences, CancellationToken cancellationToken)
    {
        using (await _mutex.LockAsync())
        {
            var (contextId, contextInfo) = _contexts.FirstOrDefault(c => AreEqualOrSubsetOfPackageReferences(c.Value.PackageReferences, packageReferences));

            if (contextInfo is null)
            {
                contextId = GetUniqueHash(packageReferences);

                var context = await _contextResolver.ResolveAsync(packageReferences, contextId,
                    s_sharedAssemblies, _loggerFactory, cancellationToken);

                contextInfo = new() { Context = context, PackageReferences = packageReferences, };
                _contexts.Add(contextId, contextInfo);
                _logger.CreatedContext(contextId, deploymentId);
            }
            else
            {
                _logger.UsedExistingContext(contextId, deploymentId);
            }

            _deploymentContextMap.Add(deploymentId, contextId);

            contextInfo.ConsumerCount++;
            return contextInfo.Context;
        }
    }

    public async Task UnregisterDeploymentAsync(string deploymentId)
    {
        using (await _mutex.LockAsync())
        {
            if (_deploymentContextMap.TryGetValue(deploymentId, out var contextId)
                && _contexts.TryGetValue(contextId, out var contextInfo))
            {
                _logger.RemoveConsumer(contextId, deploymentId);

                if (contextInfo.ConsumerCount == 1)
                {
                    UnloadContext(deploymentId, contextId, contextInfo.Context);
                    _contexts.Remove(contextId);
                    _ = TryEnsureUnloadAsync();
                    _logger.ContextUnloadInitiated(contextId, deploymentId);
                }
                else
                {
                    contextInfo.ConsumerCount--;
                }

                _deploymentContextMap.Remove(deploymentId);
            }
        }

        static async Task TryEnsureUnloadAsync()
        {
            for (var i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(50, default).ConfigureAwait(false);
            }

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive);
        }
    }

    private void UnloadContext(string deploymentId, string contextId, AssemblyLoadContext context)
    {
        context.Unload();
        AssemblyLoadContextObserver.Observe(context,
            () => _logger.ContextIsStillAlive(LogLevel.Information, contextId, deploymentId),
            () => _logger.ContextIsStillAlive(LogLevel.Warning, contextId, deploymentId),
            () => _logger.ContextIsUnloaded(contextId, deploymentId));
    }

    internal static string GetUniqueHash(IReadOnlyCollection<PackageReference> packageReferences)
    {
        return HashToString(CreateHash(packageReferences));

#pragma warning disable CA5350 // Keine schwachen kryptografischen Algorithmen verwenden
        static Span<byte> CreateHash(IEnumerable<PackageReference> references)
            => SHA1.HashData(
                Encoding.UTF8.GetBytes(
                    string.Join(string.Empty, references
                        .Select(r => r.Name + r.Version)
                        .Order()).ToUpperInvariant()));
#pragma warning restore CA5350 // Keine schwachen kryptografischen Algorithmen verwenden

        static string HashToString(Span<byte> hash)
        {
            var result = new StringBuilder();

            foreach (var part in hash)
                result.Append(part.ToString("X2", CultureInfo.InvariantCulture));

            return result.ToString();
        }
    }

    internal static bool AreEqualOrSubsetOfPackageReferences(IReadOnlyCollection<PackageReference> mainReferences, IReadOnlyCollection<PackageReference> subReferences)
    {
        if (subReferences.Count == 0 && mainReferences.Count > 0)
            return false;

        return subReferences.Intersect(mainReferences, PackageReferenceIgnoreCasingEqualityComparer.Default).Count() == subReferences.Count;
    }

    public void Dispose()
    {
        _mutex.Dispose();
        _contexts.Clear();
    }

    internal class ContextInfo
    {
        public required AssemblyLoadContext Context { get; set; }
        public IReadOnlyCollection<PackageReference> PackageReferences { get; set; } = [];
        public int ConsumerCount { get; set; }
    }
}
